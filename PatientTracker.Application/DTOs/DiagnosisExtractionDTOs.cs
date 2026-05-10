using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using PatientTracker.Domain.Entities;

namespace PatientTracker.Application.DTOs;

public class DiagnosisDocumentDto
{
    public int Id { get; set; }
    public int? DocumentId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string? ThumbnailPath { get; set; }
    public DiagnosisExtractionStatus ExtractionStatus { get; set; }
    public string ExtractionStatusName { get; set; } = string.Empty;
    public DateTime? ExtractedAt { get; set; }
    public string? ExtractionError { get; set; }
    public int RetryCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<DiagnosisDto> ExtractedDiagnoses { get; set; } = new();
}

public class UploadDiagnosisDocumentRequest
{
    [Required]
    public IFormFile File { get; set; } = null!;
    
    public DateTime? DiagnosisDate { get; set; }
}

public class ExtractedDiagnosisDto
{
    public string DiagnosisName { get; set; } = string.Empty;
    public string? ICDCode { get; set; }
    public string? Description { get; set; }
    public string? Severity { get; set; }
    public string? Status { get; set; }
    public string? Notes { get; set; }
    public decimal? Confidence { get; set; }
}

public class DiagnosisExtractionResponse
{
    public DiagnosisDocumentDto Document { get; set; } = null!;
    public List<ExtractedDiagnosisDto> ExtractedDiagnoses { get; set; } = new();
    public bool NeedsManualReview { get; set; }
    public string? Message { get; set; }
}

public class UpdateExtractedDiagnosisRequest
{
    [Required]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string DiagnosisName { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string? ICDCode { get; set; }
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    [MaxLength(50)]
    public string? Severity { get; set; }
    
    [MaxLength(50)]
    public string? Status { get; set; } = "active";
    
    [MaxLength(1000)]
    public string? Notes { get; set; }
}

public class DiagnosisDocumentWithDiagnosesDto
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
    public DiagnosisExtractionStatus ExtractionStatus { get; set; }
    public string ExtractionStatusName { get; set; } = string.Empty;
    public DateTime? ExtractedAt { get; set; }
    public string? ExtractionError { get; set; }
    public int RetryCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<DiagnosisDto> Diagnoses { get; set; } = new();
}

public class DiagnosisDocumentsQueryParameters
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
