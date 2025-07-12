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
builder.Services.AddSingleton<ICrawlTriggerService, CrawlTriggerService>();
builder.Services.AddScoped<IDelayProvider, DelayProvider>();

// Add Git-related services
builder.Services.AddScoped<IGitCommandService, GitCommandService>();
builder.Services.AddScoped<IGitLogParser, GitLogParser>();
builder.Services.AddScoped<IRepositoryScanner, RepositoryScanner>();
builder.Services.AddScoped<IGitIntegrationService, GitIntegrationService>();
builder.Services.AddScoped<IRepositoryDataService, RepositoryDataService>();

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


app.Run();
