using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatientTracker.Application.DTOs;
using PatientTracker.Application.Services;
using System.Security.Claims;

namespace PatientTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LabTestsController : ControllerBase
{
    private readonly ILabTestService _labTestService;

    public LabTestsController(ILabTestService labTestService)
    {
        _labTestService = labTestService;
    }

    /// <summary>
    /// Get all lab tests for the authenticated user
    /// </summary>
    /// <returns>List of lab tests</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<LabTestDto>>> GetLabTests()
    {
        try
        {
            var userId = GetUserId();
            var labTests = await _labTestService.GetLabTestsAsync(userId);
            return Ok(labTests);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while fetching lab tests" });
        }
    }

    /// <summary>
    /// Get a specific lab test by ID
    /// </summary>
    /// <param name="id">Lab test ID</param>
    /// <returns>Lab test details</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<LabTestDto>> GetLabTest(int id)
    {
        try
        {
            var userId = GetUserId();
            var labTest = await _labTestService.GetLabTestAsync(id, userId);
            
            if (labTest == null)
            {
                return NotFound(new { error = "Lab test not found" });
            }

            return Ok(labTest);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while fetching lab test" });
        }
    }

    /// <summary>
    /// Create a new lab test
    /// </summary>
    /// <param name="request">Lab test creation request</param>
    /// <returns>Created lab test</returns>
    [HttpPost]
    public async Task<ActionResult<LabTestDto>> CreateLabTest([FromBody] CreateLabTestRequest request)
    {
        try
        {
            var userId = GetUserId();
            var labTest = await _labTestService.CreateLabTestAsync(userId, request);
            return CreatedAtAction(nameof(GetLabTest), new { id = labTest.Id }, labTest);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while creating lab test" });
        }
    }

    /// <summary>
    /// Update an existing lab test
    /// </summary>
    /// <param name="id">Lab test ID</param>
    /// <param name="request">Lab test update request</param>
    /// <returns>Updated lab test</returns>
    [HttpPut("{id}")]
    public async Task<ActionResult<LabTestDto>> UpdateLabTest(int id, [FromBody] UpdateLabTestRequest request)
    {
        try
        {
            var userId = GetUserId();
            var labTest = await _labTestService.UpdateLabTestAsync(id, userId, request);
            return Ok(labTest);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while updating lab test" });
        }
    }

    /// <summary>
    /// Delete a lab test
    /// </summary>
    /// <param name="id">Lab test ID</param>
    /// <returns>Delete result</returns>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteLabTest(int id)
    {
        try
        {
            var userId = GetUserId();
            var result = await _labTestService.DeleteLabTestAsync(id, userId);
            
            if (!result)
            {
                return NotFound(new { error = "Lab test not found" });
            }

            return Ok(new { message = "Lab test deleted successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while deleting lab test" });
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
