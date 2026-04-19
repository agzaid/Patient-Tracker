using PatientTracker.Application.DTOs;

namespace PatientTracker.Application.Services;

public interface ILabTestExtractionService
{
    Task<LabTestExtractionResponse> UploadAndExtractAsync(int userId, UploadLabTestDocumentRequest request);
    Task<LabTestExtractionResponse> GetExtractionStatusAsync(int userId, int documentId);
    Task<LabTestExtractionResponse> RetryExtractionAsync(int userId, int documentId);
    Task<List<LabTestDto>> UpdateExtractedTestsAsync(int userId, int documentId, List<UpdateExtractedLabTestRequest> updates);
    Task<bool> DeleteLabTestDocumentAsync(int userId, int documentId);
    Task<LabTestDocumentWithTestsDto?> GetLabTestDocumentWithTestsAsync(int userId, int documentId);
    Task<PaginatedResponse<LabTestDocumentDto>> GetLabTestDocumentsAsync(int userId, int page = 1, int pageSize = 10, string? search = null);
}
