using System.ComponentModel.DataAnnotations;

namespace PatientTracker.Application.DTOs;

public class PasswordResetRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    public string Email { get; set; }
}

public class ResetPasswordRequest
{
    [Required(ErrorMessage = "Token is required")]
    public string Token { get; set; }

    [Required(ErrorMessage = "New password is required")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters long")]
    public string NewPassword { get; set; }

    [Required(ErrorMessage = "Password confirmation is required")]
    [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; set; }
}
