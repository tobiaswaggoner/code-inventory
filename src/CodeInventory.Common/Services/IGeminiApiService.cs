namespace CodeInventory.Common.Services;

public interface IGeminiApiService
{
    Task<GeminiTextResult> GenerateProjectDescriptionAsync(string repomixContent, CancellationToken cancellationToken = default);
    Task<GeminiTextResult> GenerateOneLineDescriptionAsync(string markdownDescription, CancellationToken cancellationToken = default);
    Task<GeminiTextResult> GenerateImagePromptAsync(string markdownDescription, CancellationToken cancellationToken = default);
    Task<GeminiImageResult> GenerateImageAsync(string imagePrompt, CancellationToken cancellationToken = default);
}

public class GeminiTextResult
{
    public bool IsSuccess { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
    public int TokensUsed { get; set; }
}

public class GeminiImageResult
{
    public bool IsSuccess { get; set; }
    public byte[] ImageData { get; set; } = Array.Empty<byte>();
    public string Error { get; set; } = string.Empty;
    public string MimeType { get; set; } = "image/png";
}