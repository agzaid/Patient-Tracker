using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatientTracker.Application.DTOs;
using PatientTracker.Application.Services;
using System.Security.Claims;

namespace PatientTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DiagnosesController : ControllerBase
{
    private readonly IDiagnosisService _diagnosisService;

    public DiagnosesController(IDiagnosisService diagnosisService)
    {
        _diagnosisService = diagnosisService;
    }

    /// <summary>
    /// Get all diagnoses for the authenticated user
    /// </summary>
    /// <returns>List of diagnoses</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<DiagnosisDto>>> GetDiagnoses()
    {
        try
        {
            var userId = GetUserId();
            var diagnoses = await _diagnosisService.GetDiagnosesAsync(userId);
            return Ok(diagnoses);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while fetching diagnoses" });
        }
    }

    /// <summary>
    /// Get a specific diagnosis by ID
    /// </summary>
    /// <param name="id">Diagnosis ID</param>
    /// <returns>Diagnosis details</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<DiagnosisDto>> GetDiagnosis(int id)
    {
        try
        {
            var userId = GetUserId();
            var diagnosis = await _diagnosisService.GetDiagnosisAsync(id, userId);
            
            if (diagnosis == null)
            {
                return NotFound(new { error = "Diagnosis not found" });
            }

            return Ok(diagnosis);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while fetching diagnosis" });
        }
    }

    /// <summary>
    /// Create a new diagnosis
    /// </summary>
    /// <param name="request">Diagnosis creation request</param>
    /// <returns>Created diagnosis</returns>
    [HttpPost]
    public async Task<ActionResult<DiagnosisDto>> CreateDiagnosis([FromBody] CreateDiagnosisRequest request)
    {
        try
        {
            var userId = GetUserId();
            var diagnosis = await _diagnosisService.CreateDiagnosisAsync(userId, request);
            return CreatedAtAction(nameof(GetDiagnosis), new { id = diagnosis.Id }, diagnosis);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while creating diagnosis" });
        }
    }

    /// <summary>
    /// Update an existing diagnosis
    /// </summary>
    /// <param name="id">Diagnosis ID</param>
    /// <param name="request">Diagnosis update request</param>
    /// <returns>Updated diagnosis</returns>
    [HttpPut("{id}")]
    public async Task<ActionResult<DiagnosisDto>> UpdateDiagnosis(int id, [FromBody] UpdateDiagnosisRequest request)
    {
        try
        {
            var userId = GetUserId();
            var diagnosis = await _diagnosisService.UpdateDiagnosisAsync(id, userId, request);
            return Ok(diagnosis);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while updating diagnosis" });
        }
    }

    /// <summary>
    /// Delete a diagnosis
    /// </summary>
    /// <param name="id">Diagnosis ID</param>
    /// <returns>Delete result</returns>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDiagnosis(int id)
    {
        try
        {
            var userId = GetUserId();
            var result = await _diagnosisService.DeleteDiagnosisAsync(id, userId);
            
            if (!result)
            {
                return NotFound(new { error = "Diagnosis not found" });
            }

            return Ok(new { message = "Diagnosis deleted successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while deleting diagnosis" });
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
