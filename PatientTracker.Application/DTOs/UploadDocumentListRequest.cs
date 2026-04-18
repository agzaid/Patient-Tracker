using Microsoft.AspNetCore.Http;
using PatientTracker.Domain.Enums;

namespace PatientTracker.Application.DTOs;

public class UploadDocumentListRequest
{
    public DocumentType DocumentType { get; set; }
    public ParentEntityType? ParentEntityType { get; set; }
    public int? ParentEntityId { get; set; }
    public int? MaxWidth { get; set; }
    public int? MaxHeight { get; set; }
    public List<IFormFile> Files { get; set; } = new();
}
