using System.Diagnostics;
using System.Text;
using CodeInventory.Common.Services;

namespace CodeInventory.Backend.Services;

public class RepomixService : IRepomixService
{
    private readonly ILogger<RepomixService> _logger;
    private const int DefaultTimeoutMs = 60000; // 60 seconds for repomix

    public RepomixService(ILogger<RepomixService> logger)
    {
        _logger = logger;
    }

    public async Task<RepomixResult> GenerateRepomixOutputAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(repositoryPath))
        {
            throw new ArgumentException("Repository path cannot be null or empty", nameof(repositoryPath));
        }

        if (!Directory.Exists(repositoryPath))
        {
            return new RepomixResult
            {
                IsSuccess = false,
                Error = $"Repository directory does not exist: {repositoryPath}"
            };
        }

        try
        {
            _logger.LogInformation("Generating repomix output for repository: {Path}", repositoryPath);

            // Check if repomix is available
            if (!await IsRepomixAvailableAsync())
            {
                return new RepomixResult
                {
                    IsSuccess = false,
                    Error = "Repomix is not available. Please install it with: npm install -g repomix"
                };
            }

            // Create .repomixignore file if needed
            await CreateRepomixIgnoreFileAsync(repositoryPath, cancellationToken);

            // Run repomix with --compress flag
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "npx",
                Arguments = "repomix --compress",
                WorkingDirectory = repositoryPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            using var process = new Process();
            process.StartInfo = processStartInfo;
            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    outputBuilder.AppendLine(e.Data);
                }
            };
            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    errorBuilder.AppendLine(e.Data);
                }
            };

            _logger.LogDebug("Executing: npx repomix --compress in {WorkingDirectory}", repositoryPath);

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            var timeoutTask = Task.Delay(DefaultTimeoutMs, cancellationToken);
            var processTask = process.WaitForExitAsync(cancellationToken);

            var completedTask = await Task.WhenAny(processTask, timeoutTask);

            if (completedTask == timeoutTask)
            {
                _logger.LogWarning("Repomix command timed out after {TimeoutMs}ms", DefaultTimeoutMs);
                
                try
                {
                    process.Kill();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to kill timed out repomix process");
                }

                return new RepomixResult
                {
                    IsSuccess = false,
                    Error = $"Repomix command timed out after {DefaultTimeoutMs}ms"
                };
            }

            var output = outputBuilder.ToString().Trim();
            var error = errorBuilder.ToString().Trim();
            var exitCode = process.ExitCode;

            _logger.LogDebug("Repomix command completed with exit code {ExitCode}", exitCode);

            if (exitCode == 0)
            {
                // Try to find the generated repomix file
                var repomixFilePath = await FindRepomixOutputFileAsync(repositoryPath);
                var repomixContent = string.Empty;
                var tokenCount = 0;

                if (!string.IsNullOrEmpty(repomixFilePath) && File.Exists(repomixFilePath))
                {
                    repomixContent = await File.ReadAllTextAsync(repomixFilePath, cancellationToken);
                    tokenCount = EstimateTokenCount(repomixContent);
                }

                return new RepomixResult
                {
                    IsSuccess = true,
                    Output = repomixContent,
                    RepomixFilePath = repomixFilePath,
                    TokenCount = tokenCount
                };
            }

            return new RepomixResult
            {
                IsSuccess = false,
                Error = string.IsNullOrEmpty(error) ? "Repomix command failed with unknown error" : error
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing repomix command for repository: {Path}", repositoryPath);
            return new RepomixResult
            {
                IsSuccess = false,
                Error = $"Exception occurred: {ex.Message}"
            };
        }
    }

    public async Task CreateRepomixIgnoreFileAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(repositoryPath) || !Directory.Exists(repositoryPath))
        {
            return;
        }

        try
        {
            var repomixIgnorePath = Path.Combine(repositoryPath, ".repomixignore");
            
            _logger.LogDebug("Creating .repomixignore file at: {Path}", repomixIgnorePath);

            var ignorePatterns = new List<string>();

            // Add standard exclusions for CSS and map files
            ignorePatterns.Add("**/*.css");
            ignorePatterns.Add("**/*.css.map");
            ignorePatterns.Add("**/*.min.js");
            ignorePatterns.Add("**/*.min.css");

            // Find and combine all .gitignore files in the repository
            await AddGitIgnoreContentsAsync(repositoryPath, ignorePatterns, cancellationToken);

            // Add common additional exclusions
            ignorePatterns.AddRange(new[]
            {
                "**/node_modules/**",
                "**/bin/**",
                "**/obj/**",
                "**/.vs/**",
                "**/.vscode/**",
                "**/dist/**",
                "**/build/**",
                "**/*.log",
                "**/.git/**",
                "**/target/**",
                "**/.gradle/**",
                "**/out/**"
            });

            // Write the consolidated .repomixignore file
            var content = string.Join("\n", ignorePatterns.Distinct());
            await File.WriteAllTextAsync(repomixIgnorePath, content, cancellationToken);

            _logger.LogDebug("Created .repomixignore with {PatternCount} patterns", ignorePatterns.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create .repomixignore file for repository: {Path}", repositoryPath);
        }
    }

    public async Task<bool> IsRepomixAvailableAsync()
    {
        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "npx",
                Arguments = "repomix --version",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process();
            process.StartInfo = processStartInfo;
            process.Start();
            
            var timeout = Task.Delay(5000);
            var processTask = process.WaitForExitAsync();
            
            var completedTask = await Task.WhenAny(processTask, timeout);
            
            if (completedTask == timeout)
            {
                try
                {
                    process.Kill();
                }
                catch { }
                return false;
            }

            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error checking repomix availability");
            return false;
        }
    }

    private async Task AddGitIgnoreContentsAsync(string repositoryPath, List<string> ignorePatterns, CancellationToken cancellationToken)
    {
        try
        {
            var gitIgnoreFiles = Directory.GetFiles(repositoryPath, ".gitignore", SearchOption.AllDirectories);
            
            foreach (var gitIgnoreFile in gitIgnoreFiles)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                try
                {
                    var lines = await File.ReadAllLinesAsync(gitIgnoreFile, cancellationToken);
                    var validLines = lines
                        .Where(line => !string.IsNullOrWhiteSpace(line) && !line.TrimStart().StartsWith("#"))
                        .Select(line => line.Trim());
                    
                    ignorePatterns.AddRange(validLines);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to read .gitignore file: {FilePath}", gitIgnoreFile);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error searching for .gitignore files in: {Path}", repositoryPath);
        }
    }

    private Task<string> FindRepomixOutputFileAsync(string repositoryPath)
    {
        try
        {
            // Common repomix output file names
            var possibleFiles = new[]
            {
                "repomix-output.txt",
                "repomix-output.xml",
                "repomix-output.md",
                "output.txt"
            };

            foreach (var fileName in possibleFiles)
            {
                var filePath = Path.Combine(repositoryPath, fileName);
                if (File.Exists(filePath))
                {
                    return Task.FromResult(filePath);
                }
            }

            // Search for any file that might be the repomix output
            var files = Directory.GetFiles(repositoryPath, "*repomix*", SearchOption.TopDirectoryOnly);
            return Task.FromResult(files.FirstOrDefault() ?? string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error finding repomix output file in: {Path}", repositoryPath);
            return Task.FromResult(string.Empty);
        }
    }

    private static int EstimateTokenCount(string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return 0;
        }

        // Rough estimation: 1 token ≈ 4 characters (OpenAI standard)
        // This is an approximation as actual tokenization depends on the specific tokenizer
        return content.Length / 4;
    }
}