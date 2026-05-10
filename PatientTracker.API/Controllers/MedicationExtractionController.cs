using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using PatientTracker.Application.DTOs;
using PatientTracker.Application.Resources;
using PatientTracker.Application.Services;
using System.Security.Claims;

namespace PatientTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MedicationExtractionController : ControllerBase
{
    private readonly IMedicationExtractionService _extractionService;
    private readonly IStringLocalizer<ErrorMessages> _localizer;

    public MedicationExtractionController(
        IMedicationExtractionService extractionService,
        IStringLocalizer<ErrorMessages> localizer)
    {
        _extractionService = extractionService;
        _localizer = localizer;
    }

    /// <summary>
    /// Upload and extract medication information from PDF/image
    /// </summary>
    /// <param name="request">Upload request with file</param>
    /// <returns>Extraction status and results</returns>
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<MedicationExtractionResponse>> UploadAndExtract([FromForm] UploadMedicationDocumentRequest request)
    {
        try
        {
            var userId = GetUserId();
            var result = await _extractionService.UploadAndExtractAsync(userId, request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = _localizer["ErrorUploadingDocument"] });
        }
    }

    /// <summary>
    /// Upload and extract medication information using Tesseract OCR
    /// </summary>
    /// <param name="request">Upload request with file</param>
    /// <returns>Extraction status and results</returns>
    [HttpPost("tesseract")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<MedicationExtractionResponse>> UploadAndExtractTesseract([FromForm] UploadMedicationDocumentRequest request)
    {
        try
        {
            var userId = GetUserId();
            var result = await _extractionService.UploadAndExtractTesseractAsync(userId, request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = _localizer["ErrorUploadingDocument"] });
        }
    }

    /// <summary>
    /// Get extraction status and results for a document
    /// </summary>
    /// <param name="documentId">Document ID</param>
    /// <returns>Extraction status and results</returns>
    [HttpGet("{documentId}")]
    public async Task<ActionResult<MedicationExtractionResponse>> GetExtractionStatus(int documentId)
    {
        try
        {
            var userId = GetUserId();
            var result = await _extractionService.GetExtractionStatusAsync(userId, documentId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while fetching extraction status" });
        }
    }

    /// <summary>
    /// Retry extraction for a failed document
    /// </summary>
    /// <param name="documentId">Document ID</param>
    /// <returns>Extraction status</returns>
    [HttpPost("{documentId}/retry")]
    public async Task<ActionResult<MedicationExtractionResponse>> RetryExtraction(int documentId)
    {
        try
        {
            var userId = GetUserId();
            var result = await _extractionService.RetryExtractionAsync(userId, documentId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while retrying extraction" });
        }
    }

    /// <summary>
    /// Update manually edited medication information
    /// </summary>
    /// <param name="documentId">Document ID</param>
    /// <param name="updates">List of medication updates</param>
    /// <returns>Updated medications</returns>
    [HttpPut("{documentId}/medications")]
    public async Task<ActionResult<List<MedicationDto>>> UpdateExtractedMedications(int documentId, [FromBody] List<UpdateExtractedMedicationRequest> updates)
    {
        try
        {
            var userId = GetUserId();
            var result = await _extractionService.UpdateExtractedMedicationsAsync(userId, documentId, updates);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while updating medications" });
        }
    }

    /// <summary>
    /// Delete a medication document and all its extracted medications
    /// </summary>
    /// <param name="documentId">Document ID</param>
    /// <returns>Deletion result</returns>
    [HttpDelete("{documentId}")]
    public async Task<IActionResult> DeleteMedicationDocument(int documentId)
    {
        try
        {
            var userId = GetUserId();
            var result = await _extractionService.DeleteMedicationDocumentAsync(userId, documentId);
            
            if (!result)
            {
                return NotFound(new { error = "Document not found" });
            }

            return Ok(new { message = "Document deleted successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while deleting document" });
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
