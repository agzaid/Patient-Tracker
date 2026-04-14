using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatientTracker.Application.DTOs;
using PatientTracker.Application.Services;
using System.Security.Claims;

namespace PatientTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IProfileService _profileService;

    public ProfileController(IProfileService profileService)
    {
        _profileService = profileService;
    }

    /// <summary>
    /// Get user profile
    /// </summary>
    /// <returns>User profile information</returns>
    [HttpGet]
    public async Task<ActionResult<ProfileDto>> GetProfile()
    {
        var userId = GetUserId();
        var profile = await _profileService.GetProfileAsync(userId);
        
        if (profile == null)
        {
            return NotFound(new { error = "Profile not found" });
        }

        return Ok(profile);
    }

    /// <summary>
    /// Create user profile
    /// </summary>
    /// <param name="request">Profile creation request</param>
    /// <returns>Created profile</returns>
    [HttpPost]
    public async Task<ActionResult<ProfileDto>> CreateProfile([FromBody] CreateProfileRequest request)
    {
        try
        {
            var userId = GetUserId();
            var profile = await _profileService.CreateProfileAsync(userId, request);
            return CreatedAtAction(nameof(GetProfile), new { }, profile);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while creating profile" });
        }
    }

    /// <summary>
    /// Update user profile
    /// </summary>
    /// <param name="request">Profile update request</param>
    /// <returns>Updated profile</returns>
    [HttpPut]
    public async Task<ActionResult<ProfileDto>> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        try
        {
            var userId = GetUserId();
            var profile = await _profileService.UpdateProfileAsync(userId, request);
            return Ok(profile);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while updating profile" });
        }
    }

    /// <summary>
    /// Delete user profile
    /// </summary>
    /// <returns>Delete result</returns>
    [HttpDelete]
    public async Task<IActionResult> DeleteProfile()
    {
        try
        {
            var userId = GetUserId();
            var result = await _profileService.DeleteProfileAsync(userId);
            
            if (!result)
            {
                return NotFound(new { error = "Profile not found" });
            }

            return Ok(new { message = "Profile deleted successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while deleting profile" });
        }
    }

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user identifier");
        }
        return userId;
    }
}
