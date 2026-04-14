using System.ComponentModel.DataAnnotations;

namespace PatientTracker.Domain.Entities;

public class LabTest
{
    public int Id { get; set; }
    public int UserId { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string TestName { get; set; } = string.Empty;
    
    public DateTime TestDate { get; set; }
    
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
    
    [MaxLength(500)]
    public string? ReportUrl { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation property
    public User User { get; set; } = null!;
}
