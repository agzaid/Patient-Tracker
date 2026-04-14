using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatientTracker.Application.DTOs;
using PatientTracker.Application.Services;
using System.Security.Claims;

namespace PatientTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RadiologyController : ControllerBase
{
    private readonly IRadiologyService _radiologyService;

    public RadiologyController(IRadiologyService radiologyService)
    {
        _radiologyService = radiologyService;
    }

    /// <summary>
    /// Get all radiology scans for the authenticated user
    /// </summary>
    /// <returns>List of radiology scans</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<RadiologyScanDto>>> GetRadiologyScans()
    {
        try
        {
            var userId = GetUserId();
            var scans = await _radiologyService.GetRadiologyScansAsync(userId);
            return Ok(scans);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while fetching radiology scans" });
        }
    }

    /// <summary>
    /// Get a specific radiology scan by ID
    /// </summary>
    /// <param name="id">Radiology scan ID</param>
    /// <returns>Radiology scan details</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<RadiologyScanDto>> GetRadiologyScan(int id)
    {
        try
        {
            var userId = GetUserId();
            var scan = await _radiologyService.GetRadiologyScanAsync(id, userId);
            
            if (scan == null)
            {
                return NotFound(new { error = "Radiology scan not found" });
            }

            return Ok(scan);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while fetching radiology scan" });
        }
    }

    /// <summary>
    /// Create a new radiology scan
    /// </summary>
    /// <param name="request">Radiology scan creation request</param>
    /// <returns>Created radiology scan</returns>
    [HttpPost]
    public async Task<ActionResult<RadiologyScanDto>> CreateRadiologyScan([FromBody] CreateRadiologyScanRequest request)
    {
        try
        {
            var userId = GetUserId();
            var scan = await _radiologyService.CreateRadiologyScanAsync(userId, request);
            return CreatedAtAction(nameof(GetRadiologyScan), new { id = scan.Id }, scan);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while creating radiology scan" });
        }
    }

    /// <summary>
    /// Update an existing radiology scan
    /// </summary>
    /// <param name="id">Radiology scan ID</param>
    /// <param name="request">Radiology scan update request</param>
    /// <returns>Updated radiology scan</returns>
    [HttpPut("{id}")]
    public async Task<ActionResult<RadiologyScanDto>> UpdateRadiologyScan(int id, [FromBody] UpdateRadiologyScanRequest request)
    {
        try
        {
            var userId = GetUserId();
            var scan = await _radiologyService.UpdateRadiologyScanAsync(id, userId, request);
            return Ok(scan);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while updating radiology scan" });
        }
    }

    /// <summary>
    /// Delete a radiology scan
    /// </summary>
    /// <param name="id">Radiology scan ID</param>
    /// <returns>Delete result</returns>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRadiologyScan(int id)
    {
        try
        {
            var userId = GetUserId();
            var result = await _radiologyService.DeleteRadiologyScanAsync(id, userId);
            
            if (!result)
            {
                return NotFound(new { error = "Radiology scan not found" });
            }

            return Ok(new { message = "Radiology scan deleted successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while deleting radiology scan" });
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
