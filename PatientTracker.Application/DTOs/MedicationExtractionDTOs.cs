using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using PatientTracker.Domain.Entities;

namespace PatientTracker.Application.DTOs;

public class MedicationDocumentDto
{
    public int Id { get; set; }
    public int? DocumentId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string? ThumbnailPath { get; set; }
    public MedicationExtractionStatus ExtractionStatus { get; set; }
    public string ExtractionStatusName { get; set; } = string.Empty;
    public DateTime? ExtractedAt { get; set; }
    public string? ExtractionError { get; set; }
    public int RetryCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<MedicationDto> ExtractedMedications { get; set; } = new();
}

public class UploadMedicationDocumentRequest
{
    [Required]
    public IFormFile File { get; set; } = null!;
    
    public DateTime? PrescriptionDate { get; set; }
}

public class ExtractedMedicationDto
{
    public string MedicationName { get; set; } = string.Empty;
    public string? Dosage { get; set; }
    public string? Frequency { get; set; }
    public string? Route { get; set; }
    public string? Duration { get; set; }
    public string? Instructions { get; set; }
    public decimal? Confidence { get; set; }
}

public class MedicationExtractionResponse
{
    public MedicationDocumentDto Document { get; set; } = null!;
    public List<ExtractedMedicationDto> ExtractedMedications { get; set; } = new();
    public bool NeedsManualReview { get; set; }
    public string? Message { get; set; }
}

public class UpdateExtractedMedicationRequest
{
    [Required]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string MedicationName { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string? Dosage { get; set; }
    
    [MaxLength(100)]
    public string? Frequency { get; set; }
    
    [MaxLength(50)]
    public string? Route { get; set; }
    
    [MaxLength(100)]
    public string? Duration { get; set; }
    
    [MaxLength(1000)]
    public string? Instructions { get; set; }
}

public class MedicationDocumentWithMedicationsDto
{
    public int Id { get; set; }
    public int? DocumentId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string? ThumbnailPath { get; set; }
    public string DocumentUrl { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public MedicationExtractionStatus ExtractionStatus { get; set; }
    public string ExtractionStatusName { get; set; } = string.Empty;
    public DateTime? ExtractedAt { get; set; }
    public string? ExtractionError { get; set; }
    public int RetryCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<MedicationDto> Medications { get; set; } = new();
}

public class MedicationDocumentsQueryParameters
{
    private int _page = 1;
    private int _pageSize = 10;

    public int Page 
    { 
        get => _page; 
        set => _page = Math.Max(1, value); 
    }

    public int PageSize 
    { 
        get => _pageSize; 
        set => _pageSize = Math.Max(1, Math.Min(100, value)); 
    }

    public string? Search { get; set; }
}
