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
public class LabTestExtractionController : ControllerBase
{
    private readonly ILabTestExtractionService _extractionService;
    private readonly IStringLocalizer<ErrorMessages> _localizer;

    public LabTestExtractionController(
        ILabTestExtractionService extractionService,
        IStringLocalizer<ErrorMessages> localizer)
    {
        _extractionService = extractionService;
        _localizer = localizer;
    }

    /// <summary>
    /// Upload and extract lab test results from PDF/image
    /// </summary>
    /// <param name="request">Upload request with file</param>
    /// <returns>Extraction status and results</returns>
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<LabTestExtractionResponse>> UploadAndExtract([FromForm] UploadLabTestDocumentRequest request)
    {
        var userId = GetUserId();
        var result = await _extractionService.UploadAndExtractAsync(userId, request);
        return Ok(result);
    }

    /// <summary>
    /// Upload and extract lab test results using Tesseract OCR
    /// </summary>
    /// <param name="request">Upload request with file</param>
    /// <returns>Extraction status and results</returns>
    [HttpPost("tesseract")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<LabTestExtractionResponse>> UploadAndExtractTesseract([FromForm] UploadLabTestDocumentRequest request)
    {
        var userId = GetUserId();
        var result = await _extractionService.UploadAndExtractTesseractAsync(userId, request);
        return Ok(result);
    }

    /// <summary>
    /// Get extraction status and results for a document
    /// </summary>
    /// <param name="documentId">Document ID</param>
    /// <returns>Extraction status and results</returns>
    [HttpGet("{documentId}")]
    public async Task<ActionResult<LabTestExtractionResponse>> GetExtractionStatus(int documentId)
    {
        var userId = GetUserId();
        var result = await _extractionService.GetExtractionStatusAsync(userId, documentId);
        return Ok(result);
    }

    /// <summary>
    /// Retry extraction for a failed document
    /// </summary>
    /// <param name="documentId">Document ID</param>
    /// <returns>Extraction status</returns>
    [HttpPost("{documentId}/retry")]
    public async Task<ActionResult<LabTestExtractionResponse>> RetryExtraction(int documentId)
    {
        var userId = GetUserId();
        var result = await _extractionService.RetryExtractionAsync(userId, documentId);
        return Ok(result);
    }

    /// <summary>
    /// Update manually edited lab test results
    /// </summary>
    /// <param name="documentId">Document ID</param>
    /// <param name="updates">List of lab test updates</param>
    /// <returns>Updated lab tests</returns>
    [HttpPut("{documentId}/tests")]
    public async Task<ActionResult<List<LabTestDto>>> UpdateExtractedTests(int documentId, [FromBody] List<UpdateExtractedLabTestRequest> updates)
    {
        var userId = GetUserId();
        var result = await _extractionService.UpdateExtractedTestsAsync(userId, documentId, updates);
        return Ok(result);
    }

    /// <summary>
    /// Delete a lab test document and all its extracted tests
    /// </summary>
    /// <param name="documentId">Document ID</param>
    /// <returns>Deletion result</returns>
    [HttpDelete("{documentId}")]
    public async Task<IActionResult> DeleteLabTestDocument(int documentId)
    {
        var userId = GetUserId();
        var result = await _extractionService.DeleteLabTestDocumentAsync(userId, documentId);

        if (!result)
        {
            return NotFound(new { error = _localizer["FileNotFound"] });
        }

        return Ok(new { message = _localizer["DocumentDeletedSuccessfully"] });
    }

    /// <summary>
    /// Update the original file name of a lab test document
    /// </summary>
    /// <param name="documentId">Document ID</param>
    /// <param name="request">Request with new file name</param>
    /// <returns>Update result</returns>
    [HttpPatch("{documentId}/filename")]
    public async Task<IActionResult> UpdateOriginalFileName(int documentId, [FromBody] UpdateFileNameRequest request)
    {
        var userId = GetUserId();
        var result = await _extractionService.UpdateOriginalFileNameAsync(userId, documentId, request.NewFileName);

        if (!result)
        {
            return NotFound(new { error = _localizer["FileNotFound"] });
        }

        return Ok(new { message = _localizer["FileNameUpdatedSuccessfully"] });
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
