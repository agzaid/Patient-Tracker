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
public class MedicationsController : ControllerBase
{
    private readonly IMedicationService _medicationService;
    private readonly IStringLocalizer<ErrorMessages> _localizer;

    public MedicationsController(IMedicationService medicationService, IStringLocalizer<ErrorMessages> localizer)
    {
        _medicationService = medicationService;
        _localizer = localizer;
    }

    /// <summary>
    /// Get all medications for the authenticated user
    /// </summary>
    /// <returns>List of medications</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MedicationDto>>> GetMedications()
    {
        try
        {
            var userId = GetUserId();
            var medications = await _medicationService.GetMedicationsAsync(userId);
            return Ok(medications);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = _localizer["ErrorFetchingMedications"] });
        }
    }

    /// <summary>
    /// Get a specific medication by ID
    /// </summary>
    /// <param name="id">Medication ID</param>
    /// <returns>Medication details</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<MedicationDto>> GetMedication(int id)
    {
        try
        {
            var userId = GetUserId();
            var medication = await _medicationService.GetMedicationAsync(id, userId);
            
            if (medication == null)
            {
                return NotFound(new { error = _localizer["MedicationNotFound"] });
            }

            return Ok(medication);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = _localizer["ErrorFetchingMedication"] });
        }
    }

    /// <summary>
    /// Create a new medication
    /// </summary>
    /// <param name="request">Medication creation request</param>
    /// <returns>Created medication</returns>
    [HttpPost]
    public async Task<ActionResult<MedicationDto>> CreateMedication([FromBody] CreateMedicationRequest request)
    {
        try
        {
            var userId = GetUserId();
            var medication = await _medicationService.CreateMedicationAsync(userId, request);
            return CreatedAtAction(nameof(GetMedication), new { id = medication.Id }, medication);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = _localizer["ErrorCreatingMedication"] });
        }
    }

    /// <summary>
    /// Update an existing medication
    /// </summary>
    /// <param name="id">Medication ID</param>
    /// <param name="request">Medication update request</param>
    /// <returns>Updated medication</returns>
    [HttpPut("{id}")]
    public async Task<ActionResult<MedicationDto>> UpdateMedication(int id, [FromBody] UpdateMedicationRequest request)
    {
        try
        {
            var userId = GetUserId();
            var medication = await _medicationService.UpdateMedicationAsync(id, userId, request);
            return Ok(medication);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = _localizer["ErrorUpdatingMedication"] });
        }
    }

    /// <summary>
    /// Delete a medication
    /// </summary>
    /// <param name="id">Medication ID</param>
    /// <returns>Delete result</returns>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteMedication(int id)
    {
        try
        {
            var userId = GetUserId();
            var result = await _medicationService.DeleteMedicationAsync(id, userId);
            
            if (!result)
            {
                return NotFound(new { error = "Medication not found" });
            }

            return Ok(new { message = _localizer["MedicationDeletedSuccessfully"] });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = _localizer["ErrorDeletingMedication"] });
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
