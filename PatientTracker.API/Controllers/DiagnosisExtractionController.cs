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
public class DiagnosisExtractionController : ControllerBase
{
    private readonly IDiagnosisExtractionService _extractionService;
    private readonly IStringLocalizer<ErrorMessages> _localizer;

    public DiagnosisExtractionController(
        IDiagnosisExtractionService extractionService,
        IStringLocalizer<ErrorMessages> localizer)
    {
        _extractionService = extractionService;
        _localizer = localizer;
    }

    /// <summary>
    /// Upload and extract diagnosis information from PDF/image
    /// </summary>
    /// <param name="request">Upload request with file</param>
    /// <returns>Extraction status and results</returns>
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<DiagnosisExtractionResponse>> UploadAndExtract([FromForm] UploadDiagnosisDocumentRequest request)
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
    /// Upload and extract diagnosis information using Tesseract OCR
    /// </summary>
    /// <param name="request">Upload request with file</param>
    /// <returns>Extraction status and results</returns>
    [HttpPost("tesseract")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<DiagnosisExtractionResponse>> UploadAndExtractTesseract([FromForm] UploadDiagnosisDocumentRequest request)
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
    public async Task<ActionResult<DiagnosisExtractionResponse>> GetExtractionStatus(int documentId)
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
            return StatusCode(500, new { error = _localizer["ErrorFetchingExtractionStatus"] });
        }
    }

    /// <summary>
    /// Retry extraction for a failed document
    /// </summary>
    /// <param name="documentId">Document ID</param>
    /// <returns>Extraction status</returns>
    [HttpPost("{documentId}/retry")]
    public async Task<ActionResult<DiagnosisExtractionResponse>> RetryExtraction(int documentId)
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
            return StatusCode(500, new { error = _localizer["ErrorRetryingExtraction"] });
        }
    }

    /// <summary>
    /// Update manually edited diagnosis information
    /// </summary>
    /// <param name="documentId">Document ID</param>
    /// <param name="updates">List of diagnosis updates</param>
    /// <returns>Updated diagnoses</returns>
    [HttpPut("{documentId}/diagnoses")]
    public async Task<ActionResult<List<DiagnosisDto>>> UpdateExtractedDiagnoses(int documentId, [FromBody] List<UpdateExtractedDiagnosisRequest> updates)
    {
        try
        {
            var userId = GetUserId();
            var result = await _extractionService.UpdateExtractedDiagnosesAsync(userId, documentId, updates);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = _localizer["ErrorUpdatingDiagnoses"] });
        }
    }

    /// <summary>
    /// Delete a diagnosis document and all its extracted diagnoses
    /// </summary>
    /// <param name="documentId">Document ID</param>
    /// <returns>Deletion result</returns>
    [HttpDelete("{documentId}")]
    public async Task<IActionResult> DeleteDiagnosisDocument(int documentId)
    {
        try
        {
            var userId = GetUserId();
            var result = await _extractionService.DeleteDiagnosisDocumentAsync(userId, documentId);

            if (!result)
            {
                return NotFound(new { error = _localizer["FileNotFound"] });
            }

            return Ok(new { message = _localizer["DocumentDeletedSuccessfully"] });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = _localizer["ErrorDeletingDocument"] });
        }
    }

    /// <summary>
    /// Update the original file name of a diagnosis document
    /// </summary>
    /// <param name="documentId">Document ID</param>
    /// <param name="request">Request with new file name</param>
    /// <returns>Update result</returns>
    [HttpPatch("{documentId}/filename")]
    public async Task<IActionResult> UpdateOriginalFileName(int documentId, [FromBody] UpdateFileNameRequest request)
    {
        try
        {
            var userId = GetUserId();
            var result = await _extractionService.UpdateOriginalFileNameAsync(userId, documentId, request.NewFileName);

            if (!result)
            {
                return NotFound(new { error = _localizer["FileNotFound"] });
            }

            return Ok(new { message = _localizer["FileNameUpdatedSuccessfully"] });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = _localizer["ErrorUpdatingFileName"] });
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
