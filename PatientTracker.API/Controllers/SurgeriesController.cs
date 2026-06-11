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
public class SurgeriesController : ControllerBase
{
    private readonly ISurgeryService _surgeryService;
    private readonly IStringLocalizer<ErrorMessages> _localizer;

    public SurgeriesController(ISurgeryService surgeryService, IStringLocalizer<ErrorMessages> localizer)
    {
        _surgeryService = surgeryService;
        _localizer = localizer;
    }

    /// <summary>
    /// Get all surgeries for the authenticated user (paginated)
    /// </summary>
    /// <param name="parameters">Query parameters for pagination and search</param>
    /// <returns>Paginated list of surgeries</returns>
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<SurgeryDto>>> GetSurgeries([FromQuery] QueryParameters parameters)
    {
        var userId = GetUserId();
        var paginatedSurgeries = await _surgeryService.GetSurgeriesPaginatedAsync(userId, parameters.Page, parameters.PageSize, parameters.Search);
        return Ok(paginatedSurgeries);
    }

    /// <summary>
    /// Get a specific surgery by ID
    /// </summary>
    /// <param name="id">Surgery ID</param>
    /// <returns>Surgery details</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<SurgeryDto>> GetSurgery(int id)
    {
        var userId = GetUserId();
        var surgery = await _surgeryService.GetSurgeryAsync(id, userId);
        
        if (surgery == null)
        {
            return NotFound(new { error = _localizer["SurgeryNotFound"] });
        }

        return Ok(surgery);
    }

    /// <summary>
    /// Create a new surgery
    /// </summary>
    /// <param name="request">Surgery creation request</param>
    /// <returns>Created surgery</returns>
    [HttpPost]
    public async Task<ActionResult<SurgeryDto>> CreateSurgery([FromBody] CreateSurgeryRequest request)
    {
        var userId = GetUserId();
        var surgery = await _surgeryService.CreateSurgeryAsync(userId, request);
        return CreatedAtAction(nameof(GetSurgery), new { id = surgery.Id }, surgery);
    }

    /// <summary>
    /// Update an existing surgery
    /// </summary>
    /// <param name="id">Surgery ID</param>
    /// <param name="request">Surgery update request</param>
    /// <returns>Updated surgery</returns>
    [HttpPut("{id}")]
    public async Task<ActionResult<SurgeryDto>> UpdateSurgery(int id, [FromBody] UpdateSurgeryRequest request)
    {
        var userId = GetUserId();
        var surgery = await _surgeryService.UpdateSurgeryAsync(id, userId, request);
        return Ok(surgery);
    }

    /// <summary>
    /// Delete a surgery
    /// </summary>
    /// <param name="id">Surgery ID</param>
    /// <returns>Delete result</returns>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSurgery(int id)
    {
        var userId = GetUserId();
        var result = await _surgeryService.DeleteSurgeryAsync(id, userId);
        
        if (!result)
        {
            return NotFound(new { error = "Surgery not found" });
        }

        return Ok(new { message = _localizer["SurgeryDeletedSuccessfully"] });
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
