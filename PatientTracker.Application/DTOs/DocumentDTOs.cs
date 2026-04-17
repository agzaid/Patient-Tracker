using Microsoft.AspNetCore.Http;
using PatientTracker.Domain.Enums;

namespace PatientTracker.Application.DTOs;

public class DocumentDto
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string? ThumbnailPath { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public DocumentType DocumentType { get; set; }
    public ParentEntityType ParentEntityType { get; set; }
    public int? ParentEntityId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class UploadDocumentRequest
{
    public IFormFile File { get; set; } = null!;
    public DocumentType DocumentType { get; set; }
    public ParentEntityType ParentEntityType { get; set; } = ParentEntityType.None;
    public int? ParentEntityId { get; set; }
    public int? MaxWidth { get; set; }
    public int? MaxHeight { get; set; }
}
