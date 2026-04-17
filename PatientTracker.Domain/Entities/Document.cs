using PatientTracker.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace PatientTracker.Domain.Entities;

public class Document
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(500)]
    public string OriginalFileName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string ContentType { get; set; } = string.Empty;
    
    public long FileSize { get; set; }
    
    [Required]
    [MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? ThumbnailPath { get; set; }
    
    public int? Width { get; set; }
    public int? Height { get; set; }
    
    public DocumentType DocumentType { get; set; } = DocumentType.General;
    
    public ParentEntityType ParentEntityType { get; set; } = ParentEntityType.None;
    public int? ParentEntityId { get; set; }
    
    [Required]
    public int UserId { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public User User { get; set; } = null!;
}
