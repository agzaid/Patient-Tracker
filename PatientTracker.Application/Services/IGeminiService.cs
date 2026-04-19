using PatientTracker.Application.DTOs;

namespace PatientTracker.Application.Services;

public interface IGeminiService
{
    Task<List<ExtractedLabTestDto>> ExtractLabTestsAsync(Stream fileStream, string contentType, string fileName);
    Task<bool> IsHealthyAsync();
}
