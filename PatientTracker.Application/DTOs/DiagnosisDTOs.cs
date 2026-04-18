using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace PatientTracker.Application.DTOs;

public class DiagnosisDto
{
    public int Id { get; set; }
    public string DiagnosisName { get; set; } = string.Empty;
    public DateTime? DateDiagnosed { get; set; }
    public string? DoctorName { get; set; }
    public string? Severity { get; set; }
    public string? Status { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateDiagnosisRequest
{
    [Required(ErrorMessage = "Diagnosis name is required")]
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
    
    public List<int>? DocumentIds { get; set; }
}

public class UpdateDiagnosisRequest
{
    [Required(ErrorMessage = "Diagnosis name is required")]
    [MaxLength(255)]
    public string DiagnosisName { get; set; } = string.Empty;

    public DateTime? DateDiagnosed { get; set; }

    [MaxLength(255)]
    public string? DoctorName { get; set; }

    [MaxLength(50)]
    public string? Severity { get; set; }

    [MaxLength(50)]
    public string? Status { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }
}
