using System.Diagnostics;
using System.Text;
using CodeInventory.Common.Services;

namespace CodeInventory.Backend.Services;

public class GitCommandService : IGitCommandService
{
    private readonly ILogger<GitCommandService> _logger;
    private const int DefaultTimeoutMs = 30000; // 30 seconds

    public GitCommandService(ILogger<GitCommandService> logger)
    {
        _logger = logger;
    }

    public async Task<GitCommandResult> ExecuteGitCommandAsync(string workingDirectory, string arguments, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(workingDirectory))
        {
            throw new ArgumentException("Working directory cannot be null or empty", nameof(workingDirectory));
        }

        if (string.IsNullOrEmpty(arguments))
        {
            throw new ArgumentException("Arguments cannot be null or empty", nameof(arguments));
        }

        if (!Directory.Exists(workingDirectory))
        {
            return new GitCommandResult
            {
                IsSuccess = false,
                Error = $"Working directory does not exist: {workingDirectory}",
                ExitCode = -1
            };
        }

        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
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

            _logger.LogDebug("Executing git command: git {Arguments} in {WorkingDirectory}", arguments, workingDirectory);

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            var timeoutTask = Task.Delay(DefaultTimeoutMs, cancellationToken);
            var processTask = process.WaitForExitAsync(cancellationToken);

            var completedTask = await Task.WhenAny(processTask, timeoutTask);

            if (completedTask == timeoutTask)
            {
                _logger.LogWarning("Git command timed out after {TimeoutMs}ms: git {Arguments}", DefaultTimeoutMs, arguments);
                
                try
                {
                    process.Kill();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to kill timed out git process");
                }

                return new GitCommandResult
                {
                    IsSuccess = false,
                    Error = $"Git command timed out after {DefaultTimeoutMs}ms",
                    ExitCode = -1
                };
            }

            var output = outputBuilder.ToString().Trim();
            var error = errorBuilder.ToString().Trim();
            var exitCode = process.ExitCode;

            _logger.LogDebug("Git command completed with exit code {ExitCode}", exitCode);

            return new GitCommandResult
            {
                IsSuccess = exitCode == 0,
                Output = output,
                Error = error,
                ExitCode = exitCode
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing git command: git {Arguments} in {WorkingDirectory}", arguments, workingDirectory);
            return new GitCommandResult
            {
                IsSuccess = false,
                Error = $"Exception occurred: {ex.Message}",
                ExitCode = -1
            };
        }
    }

    public async Task<bool> IsGitRepositoryAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
        {
            return false;
        }

        var result = await ExecuteGitCommandAsync(path, "rev-parse --is-inside-work-tree", cancellationToken);
        return result.IsSuccess && result.Output.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
    }
}