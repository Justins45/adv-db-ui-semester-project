using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using TheaterApi.Data;
using Google.Cloud.Firestore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseNpgsql(
        builder.Configuration.GetConnectionString("NeonDb")
        )
);

builder.Services.AddSingleton(_ =>
    FirestoreDb.Create(builder.Configuration["Firestore:ProjectId"]!));

builder.Services.AddSingleton<ILogStore, FirestoreLogStore>();

builder.Services.AddControllers();
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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
