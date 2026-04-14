using System.ComponentModel.DataAnnotations;

namespace PatientTracker.Domain.Entities;

public class User
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public Profile? Profile { get; set; }
    public ICollection<Medication> Medications { get; set; } = new List<Medication>();
    public ICollection<LabTest> LabTests { get; set; } = new List<LabTest>();
    public ICollection<RadiologyScan> RadiologyScans { get; set; } = new List<RadiologyScan>();
    public ICollection<Diagnosis> Diagnoses { get; set; } = new List<Diagnosis>();
    public ICollection<Surgery> Surgeries { get; set; } = new List<Surgery>();
    public ICollection<SharedLink> SharedLinks { get; set; } = new List<SharedLink>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
