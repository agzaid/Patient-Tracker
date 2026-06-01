using PatientTracker.Application.DTOs;

namespace PatientTracker.Application.Services;

public interface IMedicationExtractionService
{
    Task<MedicationExtractionResponse> UploadAndExtractAsync(int userId, UploadMedicationDocumentRequest request);
    Task<MedicationExtractionResponse> UploadAndExtractTesseractAsync(int userId, UploadMedicationDocumentRequest request);
    Task<MedicationExtractionResponse> GetExtractionStatusAsync(int userId, int documentId);
    Task<MedicationExtractionResponse> RetryExtractionAsync(int userId, int documentId);
    Task<List<MedicationDto>> UpdateExtractedMedicationsAsync(int userId, int documentId, List<UpdateExtractedMedicationRequest> updates);
    Task<bool> DeleteMedicationDocumentAsync(int userId, int documentId);
    Task<MedicationDocumentWithMedicationsDto?> GetMedicationDocumentWithMedicationsAsync(int userId, int documentId);
    Task<PaginatedResponse<MedicationDocumentDto>> GetMedicationDocumentsAsync(int userId, int page = 1, int pageSize = 10, string? search = null);
    Task<bool> UpdateOriginalFileNameAsync(int userId, int documentId, string newFileName);
}
