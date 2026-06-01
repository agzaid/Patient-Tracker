using PatientTracker.Application.DTOs;

namespace PatientTracker.Application.Services;

public interface IDiagnosisExtractionService
{
    Task<DiagnosisExtractionResponse> UploadAndExtractAsync(int userId, UploadDiagnosisDocumentRequest request);
    Task<DiagnosisExtractionResponse> UploadAndExtractTesseractAsync(int userId, UploadDiagnosisDocumentRequest request);
    Task<DiagnosisExtractionResponse> GetExtractionStatusAsync(int userId, int documentId);
    Task<DiagnosisExtractionResponse> RetryExtractionAsync(int userId, int documentId);
    Task<List<DiagnosisDto>> UpdateExtractedDiagnosesAsync(int userId, int documentId, List<UpdateExtractedDiagnosisRequest> updates);
    Task<bool> DeleteDiagnosisDocumentAsync(int userId, int documentId);
    Task<DiagnosisDocumentWithDiagnosesDto?> GetDiagnosisDocumentWithDiagnosesAsync(int userId, int documentId);
    Task<PaginatedResponse<DiagnosisDocumentDto>> GetDiagnosisDocumentsAsync(int userId, int page = 1, int pageSize = 10, string? search = null);
    Task<bool> UpdateOriginalFileNameAsync(int userId, int documentId, string newFileName);
}
