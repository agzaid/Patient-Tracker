using System.ComponentModel.DataAnnotations;

namespace PatientTracker.Application.DTOs;

public class SharedLinkDto
{
    public int Id { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
    public List<string> Categories { get; set; } = new();
    public bool IsActive { get; set; }
    public int AccessCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateSharedLinkRequest
{
    public List<string> Categories { get; set; } = new();
    public string Expiry { get; set; } = "7d"; // 24h, 7d, 30d, never
}

public class SharedProfileResponse
{
    public ProfileDto Profile { get; set; } = null!;
    public List<MedicationDto> Medications { get; set; } = new();
    public List<LabTestDto> LabTests { get; set; } = new();
    public List<RadiologyScanDto> RadiologyScans { get; set; } = new();
    public List<DiagnosisDto> Diagnoses { get; set; } = new();
    public List<SurgeryDto> Surgeries { get; set; } = new();
}

public class TimelineItemDto
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string? Details { get; set; }
    public string? Status { get; set; }
}
