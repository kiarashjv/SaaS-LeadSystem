using Microsoft.OpenApi.Models;
using Serilog;
using ServerX.Services;
using SharedMessaging;

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

// Add RabbitMQ Message Broker as Singleton
builder.Services.AddSingleton<IMessageBroker>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<MessageBroker>>();
    return new MessageBroker(
        builder.Configuration["RabbitMQ:HostName"] ?? "localhost",
        logger);
});

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ServerX API",
        Version = "v1",
        Description = "Main API Gateway for Lead Management System"
    });
});

// Configure HTTP clients first
builder.Services.AddHttpClient("ServerA", client =>
{
    client.BaseAddress = new Uri("http://localhost:5007");
});

builder.Services.AddHttpClient("ServerY", client =>
{
    client.BaseAddress = new Uri("http://localhost:5008");
});

// Add services with proper dependencies
builder.Services.AddSingleton<ILeadEvaluationService>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<LeadEvaluationService>>();
    var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("ServerA");
    var messageBroker = sp.GetRequiredService<IMessageBroker>();
    return new LeadEvaluationService(httpClient, logger, messageBroker);
});

builder.Services.AddSingleton<ICmsService>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<CmsService>>();
    var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("ServerY");
    var messageBroker = sp.GetRequiredService<IMessageBroker>();
    return new CmsService(httpClient, logger, messageBroker);
});

// Add QueueInitializationService last
builder.Services.AddHostedService<QueueInitializationService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
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

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
