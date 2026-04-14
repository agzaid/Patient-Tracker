using System.ComponentModel.DataAnnotations;

namespace PatientTracker.Domain.Entities;

public class Surgery
{
    public int Id { get; set; }
    public int UserId { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string SurgeryName { get; set; } = string.Empty;
    
    public DateTime SurgeryDate { get; set; }
    
    [MaxLength(255)]
    public string? HospitalName { get; set; }
    
    [MaxLength(255)]
    public string? SurgeonName { get; set; }
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    [MaxLength(1000)]
    public string? Notes { get; set; }
    
    [MaxLength(500)]
    public string? ReportUrl { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation property
    public User User { get; set; } = null!;
}
