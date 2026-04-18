using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using PatientTracker.Application.Common;
using PatientTracker.Application.DTOs;
using PatientTracker.Application.Interfaces;
using PatientTracker.Application.Resources;
using PatientTracker.Domain.Entities;
using PatientTracker.Domain.Enums;

namespace PatientTracker.Application.Services;

public interface IDocumentService
{
    Task<DocumentDto> UploadDocumentAsync(UploadDocumentRequest request, int userId);
    Task<IEnumerable<DocumentDto>> GetUserDocumentsAsync(int userId);
    Task<IEnumerable<DocumentDto>> GetEntityDocumentsAsync(ParentEntityType entityType, int entityId);
    Task<bool> DeleteDocumentAsync(int documentId, int userId);
    Task<DocumentDto?> GetDocumentAsync(int documentId, int userId);
    Task<DocumentDto?> GetDocumentForSharedLinkAsync(int documentId);
}

public class DocumentService : IDocumentService
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IImageProcessingService _imageProcessingService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;
    private readonly IStringLocalizer<ErrorMessages> _localizer;

    private readonly string[] _allowedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
    private readonly string[] _allowedDocumentExtensions = { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt" };
    private readonly string[] _imageMimeTypes = { "image/jpeg", "image/png", "image/gif", "image/bmp", "image/webp" };
    private readonly string[] _documentMimeTypes = { "application/pdf", "application/msword", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "application/vnd.ms-excel", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "application/vnd.ms-powerpoint", "application/vnd.openxmlformats-officedocument.presentationml.presentation", "text/plain" };

    public DocumentService(
        IDocumentRepository documentRepository,
        IImageProcessingService imageProcessingService,
        IUnitOfWork unitOfWork,
        IConfiguration configuration,
        IStringLocalizer<ErrorMessages> localizer)
    {
        _documentRepository = documentRepository;
        _imageProcessingService = imageProcessingService;
        _unitOfWork = unitOfWork;
        _configuration = configuration;
        _localizer = localizer;
    }

    public async Task<DocumentDto> UploadDocumentAsync(UploadDocumentRequest request, int userId)
    {
        // Validate file
        ValidateFile(request.File);

        // Create user folder path
        var userFolder = userId.ToString();
        var documentTypeFolder = GetDocumentTypeFolder(request.DocumentType);
        var fullUserFolder = Path.Combine(userFolder, documentTypeFolder);

        string filePath;
        string? thumbnailPath = null;
        int? width = null;
        int? height = null;

        // Process image files
        if (IsImageFile(request.File))
        {
            var result = await _imageProcessingService.ProcessImageAsync(
                request.File.OpenReadStream(),
                request.File.FileName,
                fullUserFolder,
                request.MaxWidth,
                request.MaxHeight);

            filePath = result.filePath;
            thumbnailPath = result.thumbnailPath;
            width = result.width;
            height = result.height;
        }
        else
        {
            // Save non-image files as-is
            filePath = await SaveDocumentAsync(request.File, fullUserFolder);
        }

        // Create document entity
        var document = new Document
        {
            FileName = Path.GetFileName(filePath),
            OriginalFileName = request.File.FileName,
            ContentType = request.File.ContentType,
            FileSize = request.File.Length,
            FilePath = filePath,
            ThumbnailPath = thumbnailPath,
            Width = width,
            Height = height,
            DocumentType = request.DocumentType,
            ParentEntityType = request.ParentEntityType,
            ParentEntityId = request.ParentEntityId,
            UserId = userId
        };

        await _documentRepository.AddAsync(document);
        await _unitOfWork.CompleteAsync();

        return MapToDto(document);
    }

    public async Task<IEnumerable<DocumentDto>> GetUserDocumentsAsync(int userId)
    {
        var documents = await _documentRepository.GetByUserIdAsync(userId);
        return documents.Select(MapToDto);
    }

    public async Task<IEnumerable<DocumentDto>> GetEntityDocumentsAsync(ParentEntityType entityType, int entityId)
    {
        var documents = await _documentRepository.GetByParentEntityAsync(entityType, entityId);
        return documents.Select(MapToDto);
    }

    public async Task<bool> DeleteDocumentAsync(int documentId, int userId)
    {
        var document = await _documentRepository.GetByIdAsync(documentId);
        if (document == null || document.UserId != userId)
        {
            throw new BusinessException(ErrorCodes.DocumentNotFound, _localizer["DocumentNotFound"]);
        }

        // Delete files
        DeleteFile(document.FilePath);
        if (!string.IsNullOrEmpty(document.ThumbnailPath))
        {
            DeleteFile(document.ThumbnailPath);
        }

        _documentRepository.Delete(document);
        await _unitOfWork.CompleteAsync();

        return true;
    }

    public async Task<DocumentDto?> GetDocumentAsync(int documentId, int userId)
    {
        var document = await _documentRepository.GetByIdAsync(documentId);
        if (document == null || document.UserId != userId)
        {
            return null;
        }

        return MapToDto(document);
    }

    private void ValidateFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            throw new InvalidOperationException(_localizer["NoFileUploaded"]);
        }

        var maxFileSize = _configuration.GetValue<long>("Uploads:MaxFileSize", 50 * 1024 * 1024); // 50MB default
        if (file.Length > maxFileSize)
        {
            throw new InvalidOperationException(_localizer["FileTooLarge"]);
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var isImage = _allowedImageExtensions.Contains(extension);
        var isDocument = _allowedDocumentExtensions.Contains(extension);

        if (!isImage && !isDocument)
        {
            throw new InvalidOperationException(_localizer["UnsupportedFileType"]);
        }

        if (isImage && !_imageMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
        {
            throw new InvalidOperationException(_localizer["InvalidImageFile"]);
        }

        if (isDocument && !_documentMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
        {
            throw new InvalidOperationException(_localizer["InvalidDocumentFile"]);
        }
    }

    private bool IsImageFile(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        return _allowedImageExtensions.Contains(extension);
    }

    private string GetDocumentTypeFolder(DocumentType documentType)
    {
        return documentType switch
        {
            DocumentType.LabReport => "lab-reports",
            DocumentType.RadiologyImage => "radiology",
            DocumentType.Prescription => "prescriptions",
            DocumentType.Insurance => "insurance",
            DocumentType.IDDocument => "id-documents",
            DocumentType.MedicalRecord => "medical-records",
            DocumentType.Invoice => "invoices",
            _ => "general"
        };
    }

    private async Task<string> SaveDocumentAsync(IFormFile file, string folder)
    {
        var uploadsPath = _configuration["Uploads:Path"] ?? "uploads";
        var userDirectory = Path.Combine(uploadsPath, folder);
        Directory.CreateDirectory(userDirectory);

        var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
        var filePath = Path.Combine(userDirectory, uniqueFileName);

        using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        return filePath;
    }

    public async Task<DocumentDto?> GetDocumentForSharedLinkAsync(int documentId)
    {
        var document = await _documentRepository.GetByIdAsync(documentId);
        if (document == null)
        {
            return null;
        }

        return MapToDto(document);
    }

    private void DeleteFile(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch
        {
            // Log error but don't throw
        }
    }

    private static DocumentDto MapToDto(Document document)
    {
        return new DocumentDto
        {
            Id = document.Id,
            UserId = document.UserId,
            FileName = document.FileName,
            OriginalFileName = document.OriginalFileName,
            ContentType = document.ContentType,
            FileSize = document.FileSize,
            FilePath = document.FilePath,
            ThumbnailPath = document.ThumbnailPath,
            Width = document.Width,
            Height = document.Height,
            DocumentType = document.DocumentType,
            ParentEntityType = document.ParentEntityType,
            ParentEntityId = document.ParentEntityId,
            CreatedAt = document.CreatedAt,
            UpdatedAt = document.UpdatedAt
        };
    }
}
