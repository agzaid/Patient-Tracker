using System.ComponentModel.DataAnnotations;

namespace PatientTracker.Domain.Entities;

public class RefreshToken
{
    public int Id { get; set; }
    public int UserId { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string Token { get; set; } = string.Empty;
    
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; } = false;
    public bool IsRevoked { get; set; } = false;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation property
    public User User { get; set; } = null!;
}
