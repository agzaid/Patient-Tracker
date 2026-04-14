using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using PatientTracker.Application.DTOs;
using PatientTracker.Application.Services;
using PatientTracker.Application.Resources;
using System.Security.Claims;

namespace PatientTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SharedLinksController : ControllerBase
{
    private readonly ISharedLinkService _sharedLinkService;
    private readonly IStringLocalizer<ErrorMessages> _localizer;

    public SharedLinksController(ISharedLinkService sharedLinkService, IStringLocalizer<ErrorMessages> localizer)
    {
        _sharedLinkService = sharedLinkService;
        _localizer = localizer;
    }

    /// <summary>
    /// Get all shared links for the authenticated user
    /// </summary>
    /// <returns>List of shared links</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SharedLinkDto>>> GetSharedLinks()
    {
        try
        {
            var userId = GetUserId();
            var links = await _sharedLinkService.GetSharedLinksAsync(userId);
            return Ok(links);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = _localizer["ErrorFetchingSharedLinks"] });
        }
    }

    /// <summary>
    /// Create a new shared link
    /// </summary>
    /// <param name="request">Shared link creation request</param>
    /// <returns>Created shared link</returns>
    [HttpPost]
    public async Task<ActionResult<SharedLinkDto>> CreateSharedLink([FromBody] CreateSharedLinkRequest request)
    {
        try
        {
            var userId = GetUserId();
            var link = await _sharedLinkService.CreateSharedLinkAsync(userId, request);
            return CreatedAtAction(nameof(GetSharedLinks), new { }, link);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = _localizer["ErrorCreatingSharedLink"] });
        }
    }

    /// <summary>
    /// Delete a shared link
    /// </summary>
    /// <param name="id">Shared link ID</param>
    /// <returns>Delete result</returns>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSharedLink(int id)
    {
        try
        {
            var userId = GetUserId();
            var result = await _sharedLinkService.DeleteSharedLinkAsync(id, userId);
            
            if (!result)
            {
                return NotFound(new { error = _localizer["SharedLinkNotFound"] });
            }

            return Ok(new { message = _localizer["SharedLinkDeletedSuccessfully"] });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = _localizer["ErrorDeletingSharedLink"] });
        }
    }

    /// <summary>
    /// Toggle shared link active status
    /// </summary>
    /// <param name="id">Shared link ID</param>
    /// <returns>Toggle result</returns>
    [HttpPut("{id}/toggle")]
    public async Task<IActionResult> ToggleSharedLink(int id)
    {
        try
        {
            var userId = GetUserId();
            var result = await _sharedLinkService.ToggleSharedLinkAsync(id, userId);
            
            if (!result)
            {
                return NotFound(new { error = _localizer["SharedLinkNotFound"] });
            }

            return Ok(new { message = _localizer["SharedLinkStatusUpdatedSuccessfully"] });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = _localizer["ErrorTogglingSharedLink"] });
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

[ApiController]
[Route("api/[controller]")]
public class ShareController : ControllerBase
{
    private readonly ISharedLinkService _sharedLinkService;

    public ShareController(ISharedLinkService sharedLinkService)
    {
        _sharedLinkService = sharedLinkService;
    }

    /// <summary>
    /// Get shared profile by token (public endpoint)
    /// </summary>
    /// <param name="token">Share token</param>
    /// <returns>Shared profile data</returns>
    [HttpGet("{token}")]
    [AllowAnonymous]
    public async Task<ActionResult<SharedProfileResponse>> GetSharedProfile(string token)
    {
        try
        {
            var profile = await _sharedLinkService.GetSharedProfileAsync(token);
            
            if (profile == null)
            {
                return NotFound(new { error = "Shared link not found, expired, or inactive" });
            }

            return Ok(profile);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while fetching shared profile" });
        }
    }
}
