using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using CodeInventory.Common.Services;
using CodeInventory.Common.Configuration;

namespace CodeInventory.Backend.Services;

public class GeminiApiService : IGeminiApiService
{
    private readonly ILogger<GeminiApiService> _logger;
    private readonly HttpClient _httpClient;
    private readonly GeminiApiSettings _settings;

    public GeminiApiService(
        ILogger<GeminiApiService> logger,
        HttpClient httpClient,
        IOptions<GeminiApiSettings> settings)
    {
        _logger = logger;
        _httpClient = httpClient;
        _settings = settings.Value;
    }

    public async Task<GeminiTextResult> GenerateProjectDescriptionAsync(string repomixContent, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(repomixContent))
        {
            return new GeminiTextResult
            {
                IsSuccess = false,
                Error = "Repomix content cannot be null or empty"
            };
        }

        var systemPrompt = @"Based on the codebase in this file, please generate a detailed response in Markdown format.

This file should be structured as:

# <PROJECT TITLE>

## Executive Summary
<A brief summary of the following sections>

## Purpose
<What ist the project intended to do? What ist the project about to solve?>

## Tech Stack
<What technologies where used? Programming Language. Frameworks, Versions, Infrastructure like Docker, Databases, Cloud Resources>

## State
<Given the purpose: How far ist the implementation? Just Begun? Partially implemented? Feature complete? Give a reason for your opinion>

## Quality
<Be opinonated: Does this look like production code or like a throw away prototype? How current are the used frameworks and code constructs? What needs to be improved?

Create only the markdown - nothing else.";

        return await CallGeminiTextApiAsync(systemPrompt, repomixContent, cancellationToken);
    }

    public async Task<GeminiTextResult> GenerateOneLineDescriptionAsync(string markdownDescription, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(markdownDescription))
        {
            return new GeminiTextResult
            {
                IsSuccess = false,
                Error = "Markdown description cannot be null or empty"
            };
        }

        var systemPrompt = @"This is the description of a development project I have been working on.

Please generate a snappy one liner that captures the intent of this project.

Return nothing else. Only the one-liner.";

        return await CallGeminiTextApiAsync(systemPrompt, markdownDescription, cancellationToken);
    }

    public async Task<GeminiTextResult> GenerateImagePromptAsync(string markdownDescription, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(markdownDescription))
        {
            return new GeminiTextResult
            {
                IsSuccess = false,
                Error = "Markdown description cannot be null or empty"
            };
        }

        var systemPrompt = @"Please complete the prompt which created an image for this project by replacing the <description> in the follwowing

---
A minimalist flat design illustration, intended as a professional hero image for a tech blog article.
The overall style is clean and symbolic, using simple geometric shapes and avoiding distracting details, reminiscent of a high-end modern infographic.

<description>

The color palette is corporate and modern, using deep blues (#0B2447) and grays, with a single bright accent color like amber or orange (#FFC700) used sparingly..
The image must be in a wide 16:9 aspect ratio, suitable for a web banner.
---

Create only the prompt and nothing else";

        return await CallGeminiTextApiAsync(systemPrompt, markdownDescription, cancellationToken);
    }

    public async Task<GeminiImageResult> GenerateImageAsync(string imagePrompt, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(imagePrompt))
        {
            return new GeminiImageResult
            {
                IsSuccess = false,
                Error = "Image prompt cannot be null or empty"
            };
        }

        if (string.IsNullOrEmpty(_settings.ApiKey))
        {
            return new GeminiImageResult
            {
                IsSuccess = false,
                Error = "Gemini API key is not configured"
            };
        }

        try
        {
            _logger.LogDebug("Generating image with Gemini API");

            var requestBody = new
            {
                instances = new[]
                {
                    new { prompt = imagePrompt }
                },
                parameters = new
                {
                    sampleCount = 1,
                }
            };

            var jsonContent = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var request = new HttpRequestMessage(HttpMethod.Post, $"{_settings.BaseUrl}/models/{_settings.ImageModel}:predict")
            {
                Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
            };
            request.Headers.Add("x-goog-api-key", _settings.ApiKey);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Gemini image API request failed with status {StatusCode}: {Content}", 
                    response.StatusCode, responseContent);
                
                return new GeminiImageResult
                {
                    IsSuccess = false,
                    Error = $"API request failed with status {response.StatusCode}: {responseContent}"
                };
            }

            var responseJson = JsonSerializer.Deserialize<JsonElement>(responseContent);
            
            // Extract base64 image data from response
            if (responseJson.TryGetProperty("predictions", out var predictions) &&
                predictions.EnumerateArray().FirstOrDefault().TryGetProperty("bytesBase64Encoded", out var content))
            {
                var base64Data = content.GetString();
                if (!string.IsNullOrEmpty(base64Data))
                {
                    var imageBytes = Convert.FromBase64String(base64Data);
                    
                    _logger.LogInformation("Successfully generated image with {ByteCount} bytes", imageBytes.Length);
                    
                    return new GeminiImageResult
                    {
                        IsSuccess = true,
                        ImageData = imageBytes,
                        MimeType = "image/png"
                    };
                }
            }

            return new GeminiImageResult
            {
                IsSuccess = false,
                Error = "No image data found in response"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating image with Gemini API");
            return new GeminiImageResult
            {
                IsSuccess = false,
                Error = $"Exception occurred: {ex.Message}"
            };
        }
    }

    private async Task<GeminiTextResult> CallGeminiTextApiAsync(string systemPrompt, string userContent, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_settings.ApiKey))
        {
            return new GeminiTextResult
            {
                IsSuccess = false,
                Error = "Gemini API key is not configured"
            };
        }

        try
        {
            _logger.LogDebug("Calling Gemini text API");

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = $"{systemPrompt}\n\n{userContent}" }
                        }
                    }
                }
            };

            var jsonContent = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var request = new HttpRequestMessage(HttpMethod.Post, $"{_settings.BaseUrl}/models/{_settings.TextModel}:generateContent")
            {
                Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
            };
            request.Headers.Add("x-goog-api-key", _settings.ApiKey);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Gemini API request failed with status {StatusCode}: {Content}", 
                    response.StatusCode, responseContent);
                
                return new GeminiTextResult
                {
                    IsSuccess = false,
                    Error = $"API request failed with status {response.StatusCode}: {responseContent}"
                };
            }

            var responseJson = JsonSerializer.Deserialize<JsonElement>(responseContent);
            
            // Extract text content from response
            if (responseJson.TryGetProperty("candidates", out var candidates) &&
                candidates.EnumerateArray().FirstOrDefault().TryGetProperty("content", out var content) &&
                content.TryGetProperty("parts", out var parts) &&
                parts.EnumerateArray().FirstOrDefault().TryGetProperty("text", out var textElement))
            {
                var generatedText = textElement.GetString() ?? string.Empty;
                
                // Estimate token usage
                var tokenCount = EstimateTokenCount(userContent + systemPrompt + generatedText);
                
                _logger.LogInformation("Successfully generated text with approximately {TokenCount} tokens", tokenCount);
                
                return new GeminiTextResult
                {
                    IsSuccess = true,
                    Content = generatedText,
                    TokensUsed = tokenCount
                };
            }

            return new GeminiTextResult
            {
                IsSuccess = false,
                Error = "No text content found in response"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Gemini text API");
            return new GeminiTextResult
            {
                IsSuccess = false,
                Error = $"Exception occurred: {ex.Message}"
            };
        }
    }

    private static int EstimateTokenCount(string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return 0;
        }

        // Rough estimation: 1 token ≈ 4 characters (OpenAI standard)
        return content.Length / 4;
    }
}