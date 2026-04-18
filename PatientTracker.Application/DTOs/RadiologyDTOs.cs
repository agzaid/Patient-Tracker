using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace PatientTracker.Application.DTOs;

public class RadiologyScanDto
{
    public int Id { get; set; }
    public string ScanType { get; set; } = string.Empty;
    public string? BodyPart { get; set; }
    public DateTime ScanDate { get; set; }
    public string? Description { get; set; }
    public string? DoctorNotes { get; set; }
    public string? ReportUrl { get; set; }
    public string? HospitalName { get; set; }
    public string? DoctorName { get; set; }
    public List<DocumentDto> Documents { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateRadiologyScanRequest
{
    [Required(ErrorMessage = "Scan type is required")]
    [MaxLength(255)]
    public string ScanType { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? BodyPart { get; set; }

    [Required(ErrorMessage = "Scan date is required")]
    public DateTime ScanDate { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    [MaxLength(1000)]
    public string? DoctorNotes { get; set; }

    [MaxLength(500)]
    public string? ReportUrl { get; set; }

    [MaxLength(255)]
    public string? HospitalName { get; set; }

    [MaxLength(255)]
    public string? DoctorName { get; set; }

    public List<int>? DocumentIds { get; set; }
}

public class UpdateRadiologyScanRequest
{
    [Required(ErrorMessage = "Scan type is required")]
    [MaxLength(255)]
    public string ScanType { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? BodyPart { get; set; }

    [Required(ErrorMessage = "Scan date is required")]
    public DateTime ScanDate { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    [MaxLength(1000)]
    public string? DoctorNotes { get; set; }

    [MaxLength(500)]
    public string? ReportUrl { get; set; }

    [MaxLength(255)]
    public string? HospitalName { get; set; }

    [MaxLength(255)]
    public string? DoctorName { get; set; }
}
