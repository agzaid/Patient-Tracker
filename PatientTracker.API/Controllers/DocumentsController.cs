using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using PatientTracker.Application.DTOs;
using PatientTracker.Application.Resources;
using PatientTracker.Application.Services;
using PatientTracker.Domain.Enums;
using System.Security.Claims;

namespace PatientTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly IStringLocalizer<ErrorMessages> _localizer;
    private readonly IConfiguration _configuration;

    public DocumentsController(IDocumentService documentService, IStringLocalizer<ErrorMessages> localizer, IConfiguration configuration)
    {
        _documentService = documentService;
        _localizer = localizer;
        _configuration = configuration;
    }
    
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<DocumentDto>> UploadDocument([FromForm] UploadDocumentRequest request)
    {
        if (request.File == null)
        {
            return BadRequest(new { error = _localizer["NoFileUploaded"] });
        }

        var userId = GetUserId();
        var document = await _documentService.UploadDocumentAsync(request, userId);
        return Ok(document);
    }

    /// <summary>
    /// Upload multiple documents
    /// </summary>
    /// <param name="request">Upload document list request</param>
    /// <returns>List of uploaded document details</returns>
    [HttpPost("list")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<List<DocumentDto>>> UploadDocumentList([FromForm] UploadDocumentListRequest request)
    {
        if (request.Files == null || request.Files.Count == 0)
        {
            return BadRequest(new { error = _localizer["NoFilesUploaded"] });
        }

        var userId = GetUserId();
        var uploadedDocuments = new List<DocumentDto>();

        foreach (var file in request.Files)
        {
            if (file != null && file.Length > 0)
            {
                var uploadRequest = new UploadDocumentRequest
                {
                    File = file,
                    DocumentType = request.DocumentType,
                    ParentEntityType = request.ParentEntityType ?? default(ParentEntityType),
                    ParentEntityId = request.ParentEntityId,
                    MaxWidth = request.MaxWidth,
                    MaxHeight = request.MaxHeight
                };

                var document = await _documentService.UploadDocumentAsync(uploadRequest, userId);
                uploadedDocuments.Add(document);
            }
        }

        return Ok(uploadedDocuments);
    }

    /// <summary>
    /// Get user's documents
    /// </summary>
    /// <returns>List of user's documents</returns>
    [HttpGet]
    public async Task<IActionResult> GetUserDocuments()
    {
        var userId = GetUserId();
        var documents = await _documentService.GetUserDocumentsAsync(userId);
        return Ok(documents);
    }

    /// <summary>
    /// Get documents for a specific entity
    /// </summary>
    /// <param name="entityType">Type of entity</param>
    /// <param name="entityId">ID of the entity</param>
    /// <returns>List of documents for the entity</returns>
    [HttpGet("entity/{entityType}/{entityId}")]
    public async Task<IActionResult> GetEntityDocuments(ParentEntityType entityType, int entityId)
    {
        var documents = await _documentService.GetEntityDocumentsAsync(entityType, entityId);
        return Ok(documents);
    }

    /// <summary>
    /// Get document by ID
    /// </summary>
    /// <param name="id">Document ID</param>
    /// <returns>Document information</returns>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetDocument(int id)
    {
        var userId = GetUserId();
        var document = await _documentService.GetDocumentAsync(id, userId);
        
        if (document == null)
        {
            return NotFound(new { error = _localizer["DocumentNotFound"] });
        }

        return Ok(document);
    }

    /// <summary>
    /// Download document
    /// </summary>
    /// <param name="id">Document ID</param>
    /// <returns>File stream</returns>
    [HttpGet("{id}/download")]
    public async Task<IActionResult> DownloadDocument(int id)
    {
        var userId = GetUserId();
        var document = await _documentService.GetDocumentAsync(id, userId);
        
        if (document == null)
        {
            return NotFound(new { error = _localizer["DocumentNotFound"] });
        }

        if (!System.IO.File.Exists(document.FilePath))
        {
            return NotFound(new { error = _localizer["FileNotFound"] });
        }

        // Validate file path to prevent directory traversal
        var fullPath = Path.GetFullPath(document.FilePath);
        var uploadsPath = Path.GetFullPath(_configuration["Uploads:Path"] ?? "uploads");
        if (!fullPath.StartsWith(uploadsPath))
        {
            return BadRequest(new { error = _localizer["InvalidFilePath"] });
        }

        var fileStream = new FileStream(document.FilePath, FileMode.Open, FileAccess.Read);
        return File(fileStream, document.ContentType, document.OriginalFileName);
    }

    /// <summary>
    /// Get document thumbnail
    /// </summary>
    /// <param name="id">Document ID</param>
    /// <returns>Thumbnail image</returns>
    [HttpGet("{id}/thumbnail")]
    public async Task<IActionResult> GetThumbnail(int id)
    {
        var userId = GetUserId();
        var document = await _documentService.GetDocumentAsync(id, userId);
        
        if (document == null)
        {
            return NotFound(new { error = _localizer["DocumentNotFound"] });
        }

        if (string.IsNullOrEmpty(document.ThumbnailPath) || !System.IO.File.Exists(document.ThumbnailPath))
        {
            return NotFound(new { error = _localizer["ThumbnailNotFound"] });
        }

        var fileBytes = await System.IO.File.ReadAllBytesAsync(document.ThumbnailPath);
        return File(fileBytes, "image/webp");
    }

    /// <summary>
    /// Delete document
    /// </summary>
    /// <param name="id">Document ID</param>
    /// <returns>Deletion result</returns>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDocument(int id)
    {
        var userId = GetUserId();
        var result = await _documentService.DeleteDocumentAsync(id, userId);
        
        if (!result)
        {
            return NotFound(new { error = _localizer["DocumentNotFound"] });
        }

        return Ok(new { message = _localizer["DocumentDeletedSuccessfully"] });
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
