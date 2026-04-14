using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatientTracker.Application.DTOs;
using PatientTracker.Application.Services;
using System.Security.Claims;

namespace PatientTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SurgeriesController : ControllerBase
{
    private readonly ISurgeryService _surgeryService;

    public SurgeriesController(ISurgeryService surgeryService)
    {
        _surgeryService = surgeryService;
    }

    /// <summary>
    /// Get all surgeries for the authenticated user
    /// </summary>
    /// <returns>List of surgeries</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SurgeryDto>>> GetSurgeries()
    {
        try
        {
            var userId = GetUserId();
            var surgeries = await _surgeryService.GetSurgeriesAsync(userId);
            return Ok(surgeries);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while fetching surgeries" });
        }
    }

    /// <summary>
    /// Get a specific surgery by ID
    /// </summary>
    /// <param name="id">Surgery ID</param>
    /// <returns>Surgery details</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<SurgeryDto>> GetSurgery(int id)
    {
        try
        {
            var userId = GetUserId();
            var surgery = await _surgeryService.GetSurgeryAsync(id, userId);
            
            if (surgery == null)
            {
                return NotFound(new { error = "Surgery not found" });
            }

            return Ok(surgery);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while fetching surgery" });
        }
    }

    /// <summary>
    /// Create a new surgery
    /// </summary>
    /// <param name="request">Surgery creation request</param>
    /// <returns>Created surgery</returns>
    [HttpPost]
    public async Task<ActionResult<SurgeryDto>> CreateSurgery([FromBody] CreateSurgeryRequest request)
    {
        try
        {
            var userId = GetUserId();
            var surgery = await _surgeryService.CreateSurgeryAsync(userId, request);
            return CreatedAtAction(nameof(GetSurgery), new { id = surgery.Id }, surgery);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while creating surgery" });
        }
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
        try
        {
            var userId = GetUserId();
            var surgery = await _surgeryService.UpdateSurgeryAsync(id, userId, request);
            return Ok(surgery);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while updating surgery" });
        }
    }

    /// <summary>
    /// Delete a surgery
    /// </summary>
    /// <param name="id">Surgery ID</param>
    /// <returns>Delete result</returns>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSurgery(int id)
    {
        try
        {
            var userId = GetUserId();
            var result = await _surgeryService.DeleteSurgeryAsync(id, userId);
            
            if (!result)
            {
                return NotFound(new { error = "Surgery not found" });
            }

            return Ok(new { message = "Surgery deleted successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while deleting surgery" });
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
