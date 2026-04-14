using System.ComponentModel.DataAnnotations;

namespace PatientTracker.Application.DTOs;

public class LabTestDto
{
    public int Id { get; set; }
    public string TestName { get; set; } = string.Empty;
    public DateTime TestDate { get; set; }
    public string? ResultValue { get; set; }
    public string? ResultUnit { get; set; }
    public string? NormalRange { get; set; }
    public string? Status { get; set; }
    public string? Notes { get; set; }
    public string? ReportUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateLabTestRequest
{
    [Required(ErrorMessage = "Test name is required")]
    [MaxLength(255)]
    public string TestName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Test date is required")]
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
}

public class UpdateLabTestRequest
{
    [Required(ErrorMessage = "Test name is required")]
    [MaxLength(255)]
    public string TestName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Test date is required")]
    public DateTime TestDate { get; set; }

    [MaxLength(100)]
    public string? ResultValue { get; set; }

    [MaxLength(50)]
    public string? ResultUnit { get; set; }

    [MaxLength(100)]
    public string? NormalRange { get; set; }

    [MaxLength(50)]
    public string? Status { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    [MaxLength(500)]
    public string? ReportUrl { get; set; }
}
