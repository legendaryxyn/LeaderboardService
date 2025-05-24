using LeaderboardService.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to listen on all interfaces
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(80);
});

// Add services to the container.
builder.Services.AddSingleton<LeaderboardManager>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Leaderboard API", 
        Version = "v1",
        Description = "A simple leaderboard service API"
    });
});

var app = builder.Build();

// Always enable Swagger in Docker container
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Leaderboard API V1");
    c.RoutePrefix = "swagger";
});

// Comment out HTTPS redirection in Docker
// app.UseHttpsRedirection();

app.UseAuthorization();
app.MapControllers();

// Add a health check endpoint
app.MapGet("/health", () => "Healthy");

app.Run();
