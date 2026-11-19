using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TheaterApi.Data;
using TheaterApi.Service;
using TheaterApi.Services;
using TheaterApi.Logging;
using TheaterApi.BackgroundJobs;
using Google.Cloud.Firestore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseNpgsql(
        builder.Configuration.GetConnectionString("NeonDb"),
        npgsql => npgsql.EnableRetryOnFailure()
        )
);

builder.Services.AddSingleton(_ =>
    FirestoreDb.Create(builder.Configuration["Firestore:ProjectId"]!));

builder.Services.AddSingleton<ILogStore, FirestoreLogStore>();

builder.Services.AddTransient<FirestoreSeeder>();

builder.Services.AddFirestoreLogs();

builder.Services.AddControllers();
builder.Services.AddSingleton<ILogSink, InMemoryLogSink>();
builder.Services.AddSingleton<IMockEmailSender, MockEmailSender>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKeyResolver = (_, _, _, _) =>
        {
            var key = builder.Configuration["Jwt:SigningKey"];
            return string.IsNullOrWhiteSpace(key) ? [] : [new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))];
        },
        ValidateIssuer = !string.IsNullOrWhiteSpace(builder.Configuration["Jwt:Issuer"]),
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidateAudience = !string.IsNullOrWhiteSpace(builder.Configuration["Jwt:Audience"]),
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(1)
    };
});
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options => options.AddFixedWindowLimiter("auth", limiter => { limiter.PermitLimit = 10; limiter.Window = TimeSpan.FromMinutes(1); limiter.QueueLimit = 0; }));
builder.Services.AddScoped<AuditLogService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<HoldService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddHostedService<HoldCleanupService>();
builder.Services.AddScoped<SeedService>();
builder.Services.AddSingleton<IPriceResolutionService, PriceResolutionService>();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => 
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Our Awesome API",
        Version = "v1",
        Description = "An Api for selling and managing theater tickets.",
        Contact = new OpenApiContact
        {
            Name = "Tradition",
            Email = "tradition@example.com"
        }
    });
});

var app = builder.Build();

if (args.Any(a => string.Equals(a, "seed", StringComparison.OrdinalIgnoreCase)))
{
    using var seedScope = app.Services.CreateScope();
    await seedScope.ServiceProvider.GetRequiredService<SeedService>().RunAsync();
    return;
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Seed only in Development, and only when you flip the flag in appsettings.Development.json
if (app.Environment.IsDevelopment() && app.Configuration.GetValue<bool>("SeedFirestore"))
{
    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<FirestoreSeeder>();
    await seeder.SeedAsync(Path.Combine(app.Environment.ContentRootPath, "firestore_logs_seed.json"));
    var loginSeed = Path.Combine(app.Environment.ContentRootPath, "seed", "login_history.json");
    if (File.Exists(loginSeed)) await seeder.SeedAsync(loginSeed);
}

app.UseHttpsRedirection();

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
