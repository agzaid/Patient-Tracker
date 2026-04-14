using System.ComponentModel.DataAnnotations;

namespace PatientTracker.Application.DTOs;

public class ProfileDto
{
    public int Id { get; set; }
    public string? FullName { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? BloodType { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public List<string> Allergies { get; set; } = new();
    public List<string> ChronicDiseases { get; set; } = new();
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? EmergencyContactRelation { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateProfileRequest
{
    public string? FullName { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? BloodType { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public List<string> Allergies { get; set; } = new();
    public List<string> ChronicDiseases { get; set; } = new();
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? EmergencyContactRelation { get; set; }
}

public class UpdateProfileRequest
{
    public string? FullName { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? BloodType { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public List<string> Allergies { get; set; } = new();
    public List<string> ChronicDiseases { get; set; } = new();
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? EmergencyContactRelation { get; set; }
}
