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
    private readonly IDocumentService _documentService;
    private readonly IConfiguration _configuration;

    public ShareController(ISharedLinkService sharedLinkService, IDocumentService documentService, IConfiguration configuration)
    {
        _sharedLinkService = sharedLinkService;
        _documentService = documentService;
        _configuration = configuration;
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

    /// <summary>
    /// Download document from shared profile (public endpoint)
    /// </summary>
    /// <param name="token">Share token</param>
    /// <param name="documentId">Document ID</param>
    /// <returns>File stream</returns>
    [HttpGet("{token}/documents/{documentId}/download")]
    [AllowAnonymous]
    public async Task<IActionResult> DownloadSharedDocument(string token, int documentId)
    {
        try
        {
            // Validate token and get shared profile
            var profile = await _sharedLinkService.GetSharedProfileAsync(token);
            
            if (profile == null)
            {
                return NotFound(new { error = "Shared link not found, expired, or inactive" });
            }

            // Get document without user validation (since this is a shared link)
            var document = await _documentService.GetDocumentForSharedLinkAsync(documentId);
            
            if (document == null)
            {
                return NotFound(new { error = "Document not found" });
            }

            // Check if document belongs to the shared profile owner
            if (document.UserId != profile.Profile.UserId)
            {
                return Forbid();
            }

            if (!System.IO.File.Exists(document.FilePath))
            {
                return NotFound(new { error = "File not found" });
            }

            // Validate file path to prevent directory traversal
            var fullPath = Path.GetFullPath(document.FilePath);
            var uploadsPath = Path.GetFullPath(_configuration["Uploads:Path"] ?? "uploads");
            if (!fullPath.StartsWith(uploadsPath))
            {
                return BadRequest(new { error = "Invalid file path" });
            }

            var fileStream = new FileStream(document.FilePath, FileMode.Open, FileAccess.Read);
            return File(fileStream, document.ContentType, document.OriginalFileName);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while downloading document" });
        }
    }
}
