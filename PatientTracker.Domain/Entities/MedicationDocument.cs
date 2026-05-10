using System.ComponentModel.DataAnnotations;

namespace PatientTracker.Domain.Entities;

public class MedicationDocument
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int? DocumentId { get; set; }
    
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
    
    public string? ThumbnailPath { get; set; }
    
    public MedicationExtractionStatus ExtractionStatus { get; set; } = MedicationExtractionStatus.Pending;
    
    public DateTime? ExtractedAt { get; set; }
    
    public string? ExtractionError { get; set; }
    
    // Store the raw AI response for debugging/re-processing
    public string? RawExtractionData { get; set; }
    
    public int RetryCount { get; set; } = 0;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public User User { get; set; } = null!;
    public Document? Document { get; set; }
    public ICollection<Medication> Medications { get; set; } = new List<Medication>();
}
