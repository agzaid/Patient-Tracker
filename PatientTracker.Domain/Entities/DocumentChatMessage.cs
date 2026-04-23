using System.ComponentModel.DataAnnotations;

namespace PatientTracker.Domain.Entities;

public class DocumentChatMessage
{
    public int Id { get; set; }
    
    [Required]
    public int DocumentId { get; set; }
    
    [Required]
    [MaxLength(1000)]
    public string UserMessage { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(2000)]
    public string AiResponse { get; set; } = string.Empty;
    
    [Required]
    public int UserId { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public Document Document { get; set; } = null!;
    public User User { get; set; } = null!;
}
