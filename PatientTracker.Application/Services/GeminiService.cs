using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PatientTracker.Application.DTOs;
using PatientTracker.Application.Common;
using PatientTracker.Application.Resources;
using Polly;
using Polly.Retry;

namespace PatientTracker.Application.Services;

public class GeminiService : IGeminiService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GeminiService> _logger;
    private readonly IStringLocalizer<ErrorMessages> _localizer;
    private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;
    private readonly string _apiKey;

    public GeminiService(HttpClient httpClient, IConfiguration configuration, ILogger<GeminiService> logger, IStringLocalizer<ErrorMessages> localizer)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _localizer = localizer;
        _apiKey = _configuration["Gemini:ApiKey"] ?? throw new BusinessException(ErrorCodes.ConfigurationError, _localizer["GeminiApiKeyNotConfigured"].Value);
        
        // Configure retry policy with exponential backoff
        _retryPolicy = Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .OrResult(r => !r.IsSuccessStatusCode)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryAttempt, context) =>
                {
                    _logger.LogWarning("Retry {RetryAttempt} after {Delay}ms due to: {Error}", 
                        retryAttempt, timespan.TotalMilliseconds, outcome.Exception?.Message ?? "Non-success status code");
                });
    }

    public async Task<List<ExtractedLabTestDto>> ExtractLabTestsAsync(Stream fileStream, string contentType, string fileName)
    {
        try
        {
            _logger.LogInformation("Starting lab test extraction for file: {FileName}", fileName);
            
            // Convert file to base64
            using var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream);
            var fileBytes = memoryStream.ToArray();
            var base64Data = Convert.ToBase64String(fileBytes);

            // Prepare the request payload
            var prompt = @"You are a medical lab test extraction expert. 
                Extract all lab test results from this document and return them as a JSON array.
                Each test should have these exact fields:
                - testName: The name of the test
                - resultValue: The numerical result (always as a string, e.g., '95' not 95)
                - resultUnit: The unit of measurement
                - normalRange: The reference range
                - status: 'normal', 'high', 'low', or 'critical'
                - confidence: Your confidence (0.0 to 1.0) as a number (e.g., 0.95, not '0.95')
                
                IMPORTANT: All values except confidence must be strings. Only confidence should be a number.";

            var requestPayload = new
            {
                contents = new[]
    {
        new
        {
            parts = new object[]
            {
                new { text = prompt },
                new
                {
                    inline_data = new
                    {
                        mime_type = contentType,
                        data = base64Data
                    }
                }
            }
        }
    },
                generation_config = new // Use snake_case for the key
                {
                    temperature = 0.1,
                    max_output_tokens = 8192, // Use snake_case
                    response_mime_type = "application/json" // Use snake_case
                }
            };

            var json = JsonSerializer.Serialize(requestPayload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Make the request with retry policy
            // Use the current Gemini 3 Flash preview model
            var response = await _retryPolicy.ExecuteAsync(() =>
                _httpClient.PostAsync($"https://generativelanguage.googleapis.com/v1beta/models/gemini-3-flash-preview:generateContent?key={_apiKey}", content));

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Gemini API error: {StatusCode} - {Error}", response.StatusCode, errorContent);
                throw new BusinessException(ErrorCodes.ServiceUnavailable, string.Format(_localizer["GeminiApiError"].Value, response.StatusCode));
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("Gemini response: {Response}", responseContent);

            // Parse the response
            using var jsonDoc = JsonDocument.Parse(responseContent);
            var candidates = jsonDoc.RootElement.GetProperty("candidates");
            
            if (candidates.GetArrayLength() == 0)
            {
                throw new BusinessException(ErrorCodes.ServiceUnavailable, _localizer["GeminiNoResponse"].Value);
            }

            var text = candidates[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();
            
            if (string.IsNullOrEmpty(text))
            {
                throw new BusinessException(ErrorCodes.ServiceUnavailable, _localizer["GeminiEmptyResponse"].Value);
            }

            // Clean markdown formatting if present
            if (text.Contains("```"))
            {
                text = text.Replace("```json", "").Replace("```", "").Trim();
            }

            // Parse the extracted lab tests
            var extractedTests = JsonSerializer.Deserialize<List<ExtractedLabTestDto>>(text, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<ExtractedLabTestDto>();

            _logger.LogInformation("Successfully extracted {Count} lab tests", extractedTests.Count);
            return extractedTests;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting lab tests from file: {FileName}", fileName);
            throw;
        }
    }

    public async Task<string> GenerateResponseAsync(string prompt, string? base64Content = null, string? contentType = null)
    {
        try
        {
            var requestPayload = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = BuildChatParts(prompt, base64Content, contentType)
                    }
                },
                generation_config = new
                {
                    temperature = 0.7,
                    max_output_tokens = 2048,
                    response_mime_type = "text/plain"
                }
            };

            var json = JsonSerializer.Serialize(requestPayload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(
                $"https://generativelanguage.googleapis.com/v1beta/models/gemini-3-flash-preview:generateContent?key={_apiKey}",
                content);

            response.EnsureSuccessStatusCode();
            var responseJson = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(responseJson);
            var text = doc.RootElement
                .GetProperty("candidates")
                .EnumerateArray()
                .FirstOrDefault()
                .GetProperty("content")
                .GetProperty("parts")
                .EnumerateArray()
                .FirstOrDefault()
                .GetProperty("text")
                .GetString();

            return text ?? "I apologize, but I couldn't generate a response.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating response from Gemini");
            throw;
        }
    }

    private object[] BuildChatParts(string prompt, string? base64Content, string? contentType)
    {
        var parts = new List<object>();

        if (!string.IsNullOrEmpty(base64Content) && !string.IsNullOrEmpty(contentType))
        {
            parts.Add(new
            {
                text = prompt
            });

            parts.Add(new
            {
                inline_data = new
                {
                    mime_type = contentType,
                    data = base64Content
                }
            });
        }
        else
        {
            parts.Add(new
            {
                text = prompt
            });
        }

        return parts.ToArray();
    }

    public async Task<bool> IsHealthyAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"https://generativelanguage.googleapis.com/v1beta/models?key={_apiKey}");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
