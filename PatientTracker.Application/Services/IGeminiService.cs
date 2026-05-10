using PatientTracker.Application.DTOs;

namespace PatientTracker.Application.Services;

public interface IGeminiService
{
    Task<List<ExtractedLabTestDto>> ExtractLabTestsAsync(Stream fileStream, string contentType, string fileName, int userId);
    Task<List<ExtractedMedicationDto>> ExtractMedicationsAsync(Stream fileStream, string contentType, string fileName, int userId);
    Task<List<ExtractedDiagnosisDto>> ExtractDiagnosesAsync(Stream fileStream, string contentType, string fileName, int userId);
    Task<string> GenerateResponseAsync(string prompt, int userId, string? base64Content = null, string? contentType = null);
    Task<bool> IsHealthyAsync();
}
