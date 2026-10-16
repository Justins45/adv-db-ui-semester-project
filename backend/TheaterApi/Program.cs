using Microsoft.EntityFrameworkCore;
using TheaterApi.Data;
using Google.Cloud.Firestore;
using TheaterApi.Data;

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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
