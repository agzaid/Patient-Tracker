using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using PatientTracker.Domain.Entities;

namespace PatientTracker.Application.DTOs;

public class LabTestDocumentDto
{
    public int Id { get; set; }
    public int? DocumentId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string? ThumbnailPath { get; set; }
    public LabTestExtractionStatus ExtractionStatus { get; set; }
    public string ExtractionStatusName { get; set; } = string.Empty;
    public DateTime? ExtractedAt { get; set; }
    public string? ExtractionError { get; set; }
    public int RetryCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<LabTestDto> ExtractedLabTests { get; set; } = new();
}

public class UploadLabTestDocumentRequest
{
    [Required]
    public IFormFile File { get; set; } = null!;
    
    public DateTime? TestDate { get; set; }
}

public class ExtractedLabTestDto
{
    public string TestName { get; set; } = string.Empty;
    public string? ResultValue { get; set; }
    public string? ResultUnit { get; set; }
    public string? NormalRange { get; set; }
    public string? Status { get; set; }
    public decimal? Confidence { get; set; }
}

public class LabTestExtractionResponse
{
    public LabTestDocumentDto Document { get; set; } = null!;
    public List<ExtractedLabTestDto> ExtractedTests { get; set; } = new();
    public bool NeedsManualReview { get; set; }
    public string? Message { get; set; }
}

public class UpdateExtractedLabTestRequest
{
    [Required]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string TestName { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string? ResultValue { get; set; }
    
    [MaxLength(50)]
    public string? ResultUnit { get; set; }
    
    [MaxLength(100)]
    public string? NormalRange { get; set; }
    
    [MaxLength(50)]
    public string? Status { get; set; } = "normal";
    
    [MaxLength(1000)]
    public string? Notes { get; set; }
}

public class LabTestDocumentWithTestsDto
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
    public LabTestExtractionStatus ExtractionStatus { get; set; }
    public string ExtractionStatusName { get; set; } = string.Empty;
    public DateTime? ExtractedAt { get; set; }
    public string? ExtractionError { get; set; }
    public int RetryCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<LabTestDto> LabTests { get; set; } = new();
}

public class RetryExtractionRequest
{
    [Required]
    public int DocumentId { get; set; }
}

public class LabTestDocumentsQueryParameters
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
