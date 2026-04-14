using System.ComponentModel.DataAnnotations;

namespace PatientTracker.Domain.Entities;

public class SharedLink
{
    public int Id { get; set; }
    public int UserId { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string Token { get; set; } = string.Empty;
    
    public DateTime? ExpiresAt { get; set; }
    
    // JSON array stored as string
    public string? Categories { get; set; } = "[]";
    
    public bool IsActive { get; set; } = true;
    
    public int AccessCount { get; set; } = 0;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation property
    public User User { get; set; } = null!;
}
