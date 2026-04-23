namespace PatientTracker.Application.Resources;

public class ErrorMessages
{
    public const string NoFileProvided = "No file provided";
    public const string FileSizeExceedsMaximum = "File size exceeds maximum allowed size of {0}MB";
    public const string DocumentNotFound = "Document not found";
    public const string GeminiApiKeyNotConfigured = "Gemini API key not configured";
    public const string GeminiApiError = "Failed to communicate with Gemini API: {0}";
    public const string GeminiNoResponse = "No response from Gemini API";
    public const string GeminiEmptyResponse = "Empty response from Gemini API";
}
