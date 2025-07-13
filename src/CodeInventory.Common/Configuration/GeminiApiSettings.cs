namespace CodeInventory.Common.Configuration;

public class GeminiApiSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta";
    public string TextModel { get; set; } = "gemini-2.5-flash";
    public string ImageModel { get; set; } = "gemini-2.0-flash-preview-image-generation";
    public int TimeoutSeconds { get; set; } = 60;
    public int MaxRetries { get; set; } = 3;
}