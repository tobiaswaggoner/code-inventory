using Microsoft.EntityFrameworkCore;
using CodeInventory.Backend.Data;
using CodeInventory.Backend.Services;
using CodeInventory.Common.Configuration;
using CodeInventory.Common.Services;

var builder = WebApplication.CreateBuilder(args);

// Parse command line arguments early
var commandLineArgs = Environment.GetCommandLineArgs();

// Read .env
builder.Configuration
    .SetBasePath(AppContext.BaseDirectory)
    .AddEnvironmentVariables()
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddUserSecrets<Program>(optional: true)
    .AddEnvFile(".env")
    .AddCommandLine(commandLineArgs);


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
builder.Services.Configure<GeminiApiSettings>(builder.Configuration.GetSection("GeminiApiSettings"));
builder.Services.Configure<RepositoryAnalysisSettings>(builder.Configuration.GetSection("RepositoryAnalysisSettings"));

// Add services
builder.Services.AddSingleton<ICrawlTriggerService, CrawlTriggerService>();
builder.Services.AddScoped<IDelayProvider, DelayProvider>();

// Add Git-related services
builder.Services.AddScoped<IGitCommandService, GitCommandService>();
builder.Services.AddScoped<IGitLogParser, GitLogParser>();
builder.Services.AddScoped<IRepositoryScanner, RepositoryScanner>();
builder.Services.AddScoped<IGitIntegrationService, GitIntegrationService>();
builder.Services.AddScoped<IRepositoryDataService, RepositoryDataService>();

// Add Repository Analysis services
builder.Services.AddHttpClient<IGeminiApiService, GeminiApiService>();
builder.Services.AddScoped<IRepomixService, RepomixService>();
builder.Services.AddScoped<IRepositoryAnalysisService, RepositoryAnalysisService>();

// Add background services
builder.Services.AddHostedService<DirectoryCrawlerService>();

var app = builder.Build();

// Process command line arguments
var shouldTriggerCrawl = commandLineArgs.Contains("-execute-crawl", StringComparer.OrdinalIgnoreCase);
var shouldAnalyzeRepositories = commandLineArgs.Contains("--analyze-repositories", StringComparer.OrdinalIgnoreCase);
var analyzePathIndex = Array.FindIndex(commandLineArgs, arg => string.Equals(arg, "--analyze-path", StringComparison.OrdinalIgnoreCase));
var analyzePath = analyzePathIndex >= 0 && analyzePathIndex + 1 < commandLineArgs.Length ? commandLineArgs[analyzePathIndex + 1] : null;

// Handle different command line options
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

if (shouldAnalyzeRepositories)
{
    // Analyze all repositories after app starts
    _ = Task.Run(async () =>
    {
        await Task.Delay(1000); // Give the app time to start
        using var scope = app.Services.CreateScope();
        var analysisService = scope.ServiceProvider.GetRequiredService<IRepositoryAnalysisService>();
        var summary = await analysisService.AnalyzeAllRepositoriesAsync();
        
        Console.WriteLine($"Repository Analysis Summary:");
        Console.WriteLine($"Total: {summary.TotalRepositories}, Success: {summary.SuccessfulAnalyses}, Failed: {summary.FailedAnalyses}");
        Console.WriteLine($"Duration: {summary.TotalDuration}, Tokens Used: {summary.TotalTokensUsed}");
        
        if (summary.Errors.Any())
        {
            Console.WriteLine("Errors:");
            foreach (var error in summary.Errors)
            {
                Console.WriteLine($"  - {error}");
            }
        }
    });
}

if (!string.IsNullOrEmpty(analyzePath))
{
    // Analyze specific repository path after app starts
    _ = Task.Run(async () =>
    {
        await Task.Delay(1000); // Give the app time to start
        using var scope = app.Services.CreateScope();
        var analysisService = scope.ServiceProvider.GetRequiredService<IRepositoryAnalysisService>();
        var result = await analysisService.AnalyzeRepositoryAsync(analyzePath);
        
        Console.WriteLine($"Repository Analysis Result for {analyzePath}:");
        Console.WriteLine($"Success: {result.IsSuccess}");
        if (result.IsSuccess)
        {
            Console.WriteLine($"Headline: {result.Headline}");
            Console.WriteLine($"Tokens Used: {result.TokensUsed}");
            Console.WriteLine($"Files Created:");
            if (!string.IsNullOrEmpty(result.DescriptionFilePath)) Console.WriteLine($"  - {result.DescriptionFilePath}");
            if (!string.IsNullOrEmpty(result.HeadlineFilePath)) Console.WriteLine($"  - {result.HeadlineFilePath}");
            if (!string.IsNullOrEmpty(result.ImagePromptFilePath)) Console.WriteLine($"  - {result.ImagePromptFilePath}");
            if (!string.IsNullOrEmpty(result.HeroImageFilePath)) Console.WriteLine($"  - {result.HeroImageFilePath}");
        }
        else
        {
            Console.WriteLine($"Error: {result.Error}");
        }
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
