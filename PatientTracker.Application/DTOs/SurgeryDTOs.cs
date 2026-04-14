using System.ComponentModel.DataAnnotations;

namespace PatientTracker.Application.DTOs;

public class SurgeryDto
{
    public int Id { get; set; }
    public string SurgeryName { get; set; } = string.Empty;
    public DateTime SurgeryDate { get; set; }
    public string? HospitalName { get; set; }
    public string? SurgeonName { get; set; }
    public string? Description { get; set; }
    public string? Notes { get; set; }
    public string? ReportUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateSurgeryRequest
{
    [Required(ErrorMessage = "Surgery name is required")]
    [MaxLength(255)]
    public string SurgeryName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Surgery date is required")]
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
}

public class UpdateSurgeryRequest
{
    [Required(ErrorMessage = "Surgery name is required")]
    [MaxLength(255)]
    public string SurgeryName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Surgery date is required")]
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
}
