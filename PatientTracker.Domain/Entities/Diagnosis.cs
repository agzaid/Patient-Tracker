using System.ComponentModel.DataAnnotations;

namespace PatientTracker.Domain.Entities;

public class Diagnosis
{
    public int Id { get; set; }
    public int UserId { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string DiagnosisName { get; set; } = string.Empty;
    
    public DateTime? DateDiagnosed { get; set; }
    
    [MaxLength(255)]
    public string? DoctorName { get; set; }
    
    [MaxLength(50)]
    public string? Severity { get; set; } = "moderate";
    
    [MaxLength(50)]
    public string? Status { get; set; } = "active";
    
    [MaxLength(1000)]
    public string? Notes { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation property
    public User User { get; set; } = null!;
}
