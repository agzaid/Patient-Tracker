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
public class DocumentChatController : ControllerBase
{
    private readonly IDocumentChatService _chatService;
    private readonly IStringLocalizer<ErrorMessages> _localizer;

    public DocumentChatController(
        IDocumentChatService chatService,
        IStringLocalizer<ErrorMessages> localizer)
    {
        _chatService = chatService;
        _localizer = localizer;
    }

    /// <summary>
    /// Ask a question about a document
    /// </summary>
    /// <param name="request">Chat request with document ID and question</param>
    /// <returns>AI response with chat history</returns>
    [HttpPost]
    public async Task<ActionResult<DocumentChatResponse>> AskAboutDocument([FromBody] DocumentChatRequest request)
    {
        try
        {
            var userId = GetUserId();
            var response = await _chatService.AskAboutDocumentAsync(userId, request);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = _localizer["ErrorProcessingRequest"] });
        }
    }
    [HttpGet("history")]
    public async Task<ActionResult<PaginatedChatResponse>> GetChatHistory(
        [FromQuery] GetChatHistoryParameters parameters)
    {
        try
        {
            var userId = GetUserId();
            var request = new GetChatHistoryRequest
            {
                DocumentId = parameters.DocumentId,
                Page = parameters.Page,
                PageSize = parameters.PageSize
            };
            var response = await _chatService.GetChatHistoryAsync(userId, request);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = _localizer["ErrorFetchingChatHistory"] });
        }
    }

    /// <summary>
    /// Delete chat history for a document
    /// </summary>
    /// <param name="documentId">Document ID</param>
    /// <returns>Delete result</returns>
    [HttpDelete("history/{documentId}")]
    public async Task<IActionResult> DeleteChatHistory(int documentId)
    {
        try
        {
            var userId = GetUserId();
            var result = await _chatService.DeleteChatHistoryAsync(userId, documentId);
            
            if (!result)
            {
                return NotFound(new { error = _localizer["DocumentNotFound"] });
            }

            return Ok(new { message = "Chat history deleted successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = _localizer["ErrorDeletingChatHistory"] });
        }
    }

    private int GetUserId()
    {
        // Debug: Check if User is authenticated
        if (!User.Identity?.IsAuthenticated ?? false)
        {
            throw new UnauthorizedAccessException("User is not authenticated");
        }

        // Debug: Log all claims to see what's available
        var allClaims = User.Claims.Select(c => $"{c.Type}: {c.Value}").ToList();
        
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
        {
            // Try alternative claim names
            userIdClaim = User.FindFirst("sub") ?? User.FindFirst("userId");
        }
        
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException($"Invalid user identifier. Available claims: {string.Join(", ", allClaims)}");
        }
        return userId;
    }
}
