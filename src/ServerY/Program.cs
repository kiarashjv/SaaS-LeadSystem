using Microsoft.OpenApi.Models;
using Serilog;
using ServerY.Services;

var builder = WebApplication.CreateBuilder(args);

// Add Serilog
builder.Host.UseSerilog((context, configuration) =>
    configuration.WriteTo.Console());

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ServerY API", Version = "v1" });
});

// Register services
builder.Services.AddSingleton<ILeadStorageService, LeadStorageService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Use CORS before routing
app.UseCors();

// Comment out HTTPS redirection in development
// app.UseHttpsRedirection();

app.UseAuthorization();
app.MapControllers();

// Configure to run on port 5008
app.Urls.Add("http://localhost:5008");

app.Run();
