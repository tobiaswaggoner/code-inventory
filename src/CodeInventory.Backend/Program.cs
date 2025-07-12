using Microsoft.EntityFrameworkCore;
using CodeInventory.Backend.Data;
using CodeInventory.Backend.Services;
using CodeInventory.Common.Configuration;
using CodeInventory.Common.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Entity Framework
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add configuration
builder.Services.Configure<CrawlSettings>(builder.Configuration.GetSection("CrawlSettings"));

// Add services
builder.Services.AddSingleton<CrawlTriggerService>();
builder.Services.AddScoped<ICrawlTriggerService>(provider => provider.GetRequiredService<CrawlTriggerService>());
builder.Services.AddScoped<IDelayProvider, DelayProvider>();

// Add background services
builder.Services.AddHostedService<DirectoryCrawlerService>();

var app = builder.Build();

// Check for -execute-crawl parameter and trigger crawl if present
var shouldTriggerCrawl = Environment.GetCommandLineArgs().Contains("-execute-crawl", StringComparer.OrdinalIgnoreCase);
if (shouldTriggerCrawl)
{
    // Trigger crawl after app starts
    _ = Task.Run(async () =>
    {
        await Task.Delay(1000); // Give the app time to start
        var triggerService = app.Services.GetRequiredService<CrawlTriggerService>();
        await triggerService.TriggerCrawlAsync();
    });
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
