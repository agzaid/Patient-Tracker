using System.ComponentModel.DataAnnotations;

namespace PatientTracker.Domain.Entities;

public class Medication
{
    public int Id { get; set; }
    public int UserId { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(255)]
    public string? Dosage { get; set; }
    
    [MaxLength(100)]
    public string? Frequency { get; set; }
    
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    
    public bool IsCurrent { get; set; } = true;
    
    [MaxLength(1000)]
    public string? Notes { get; set; }
    
    [MaxLength(500)]
    public string? PrescriptionUrl { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation property
    public User User { get; set; } = null!;
}
