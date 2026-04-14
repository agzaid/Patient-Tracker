using System.ComponentModel.DataAnnotations;

namespace PatientTracker.Domain.Entities;

public class Profile
{
    public int Id { get; set; }
    public int UserId { get; set; }
    
    [MaxLength(255)]
    public string? FullName { get; set; }
    
    public DateTime? DateOfBirth { get; set; }
    
    [MaxLength(50)]
    public string? Gender { get; set; }
    
    [MaxLength(10)]
    public string? BloodType { get; set; }
    
    [MaxLength(20)]
    public string? Phone { get; set; }
    
    [MaxLength(255)]
    public string? Email { get; set; }
    
    [MaxLength(500)]
    public string? Address { get; set; }
    
    // JSON arrays stored as strings
    public string? Allergies { get; set; } = "[]";
    public string? ChronicDiseases { get; set; } = "[]";
    
    [MaxLength(255)]
    public string? EmergencyContactName { get; set; }
    
    [MaxLength(20)]
    public string? EmergencyContactPhone { get; set; }
    
    [MaxLength(100)]
    public string? EmergencyContactRelation { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation property
    public User User { get; set; } = null!;
}
