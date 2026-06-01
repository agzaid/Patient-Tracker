using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PatientTracker.Application.DTOs;
using PatientTracker.Application.Interfaces;
using PatientTracker.Application.Common;
using PatientTracker.Application.Resources;
using PatientTracker.Domain.Entities;
using Polly;
using Tesseract;

namespace PatientTracker.Application.Services;

public class DiagnosisExtractionService : IDiagnosisExtractionService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IGeminiService _geminiService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DiagnosisExtractionService> _logger;
    private readonly IStringLocalizer<ErrorMessages> _localizer;
    private readonly IDocumentService _documentService;

    public DiagnosisExtractionService(
        IServiceScopeFactory scopeFactory,
        IGeminiService geminiService,
        IConfiguration configuration,
        ILogger<DiagnosisExtractionService> logger,
        IStringLocalizer<ErrorMessages> localizer,
        IDocumentService documentService)
    {
        _scopeFactory = scopeFactory;
        _geminiService = geminiService;
        _configuration = configuration;
        _logger = logger;
        _localizer = localizer;
        _documentService = documentService;
    }

    public async Task<DiagnosisExtractionResponse> UploadAndExtractAsync(int userId, UploadDiagnosisDocumentRequest request)
    {
        using var scope = _scopeFactory.CreateScope();
        var diagnosisDocumentRepository = scope.ServiceProvider.GetRequiredService<IDiagnosisDocumentRepository>();
        var documentRepository = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        try
        {
            _logger.LogInformation("Starting diagnosis document upload for user {UserId}", userId);

            if (request.File == null || request.File.Length == 0)
            {
                throw new InvalidOperationException(_localizer["FileRequired"]);
            }

            var maxFileSize = _configuration.GetValue<long>("Uploads:MaxFileSize", 10485760);
            if (request.File.Length > maxFileSize)
            {
                throw new InvalidOperationException(_localizer["FileSizeExceeded"]);
            }

            // Check file type and handle accordingly
            var extension = Path.GetExtension(request.File.FileName).ToLowerInvariant();
            var isImage = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" }.Contains(extension);
            var isDocument = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt" }.Contains(extension);

            if (!isImage && !isDocument)
            {
                throw new InvalidOperationException(_localizer["UnsupportedFileType"]);
            }

            string filePath;
            string contentType;

            if (isImage)
            {
                // Save and optimize image
                filePath = await _documentService.SaveOptimizedImageAsync(request.File, $"diagnoses/{userId}");
                contentType = "image/webp";
            }
            else
            {
                // Save document as-is
                filePath = await _documentService.SaveDocumentAsync(request.File, $"diagnoses/{userId}");
                contentType = request.File.ContentType;
            }

            var document = new Document
            {
                UserId = userId,
                FileName = Path.GetFileName(filePath),
                OriginalFileName = request.File.FileName,
                ContentType = contentType,
                FileSize = request.File.Length,
                FilePath = filePath
            };

            document = await documentRepository.AddAsync(document);
            await unitOfWork.CompleteAsync();

            var diagnosisDocument = new DiagnosisDocument
            {
                UserId = userId,
                DocumentId = document.Id,
                FileName = Path.GetFileName(filePath),
                OriginalFileName = request.File.FileName,
                ContentType = contentType,
                FileSize = request.File.Length,
                FilePath = filePath,
                ExtractionStatus = DiagnosisExtractionStatus.Pending
            };

            diagnosisDocumentRepository.Add(diagnosisDocument);
            await unitOfWork.CompleteAsync();

            _ = Task.Run(() => ProcessExtractionAsync(diagnosisDocument.Id));

            _logger.LogInformation("Diagnosis document uploaded successfully with ID {DocumentId}", diagnosisDocument.Id);

            return new DiagnosisExtractionResponse
            {
                Document = new DiagnosisDocumentDto
                {
                    Id = diagnosisDocument.Id,
                    DocumentId = diagnosisDocument.DocumentId,
                    FileName = diagnosisDocument.FileName,
                    OriginalFileName = diagnosisDocument.OriginalFileName,
                    ContentType = diagnosisDocument.ContentType,
                    FileSize = diagnosisDocument.FileSize,
                    FilePath = diagnosisDocument.FilePath,
                    ExtractionStatus = diagnosisDocument.ExtractionStatus,
                    ExtractionStatusName = diagnosisDocument.ExtractionStatus.ToString(),
                    RetryCount = diagnosisDocument.RetryCount,
                    CreatedAt = diagnosisDocument.CreatedAt,
                    UpdatedAt = diagnosisDocument.UpdatedAt
                },
                ExtractedDiagnoses = new List<ExtractedDiagnosisDto>(),
                NeedsManualReview = false,
                Message = "Document uploaded successfully. Extraction in progress."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading diagnosis document for user {UserId}", userId);
            throw;
        }
    }

    public async Task<DiagnosisExtractionResponse> UploadAndExtractTesseractAsync(int userId, UploadDiagnosisDocumentRequest request)
    {
        using var scope = _scopeFactory.CreateScope();
        var diagnosisDocumentRepository = scope.ServiceProvider.GetRequiredService<IDiagnosisDocumentRepository>();
        var documentRepository = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        try
        {
            _logger.LogInformation("Starting diagnosis document upload with Tesseract for user {UserId}", userId);

            if (request.File == null || request.File.Length == 0)
            {
                throw new InvalidOperationException(_localizer["FileRequired"]);
            }

            var maxFileSize = _configuration.GetValue<long>("Uploads:MaxFileSize", 10485760);
            if (request.File.Length > maxFileSize)
            {
                throw new InvalidOperationException(_localizer["FileSizeExceeded"]);
            }

            var uploadsPath = _configuration["Uploads:Path"] ?? "uploads";
            var userFolderPath = Path.Combine(uploadsPath, "diagnoses", userId.ToString());
            Directory.CreateDirectory(userFolderPath);

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(request.File.FileName)}";
            var filePath = Path.Combine(userFolderPath, fileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await request.File.CopyToAsync(fileStream);
            }

            var document = new Document
            {
                UserId = userId,
                FileName = fileName,
                OriginalFileName = request.File.FileName,
                ContentType = request.File.ContentType,
                FileSize = request.File.Length,
                FilePath = filePath
            };

            document = await documentRepository.AddAsync(document);
            await unitOfWork.CompleteAsync();

            var diagnosisDocument = new DiagnosisDocument
            {
                UserId = userId,
                DocumentId = document.Id,
                FileName = fileName,
                OriginalFileName = request.File.FileName,
                ContentType = request.File.ContentType,
                FileSize = request.File.Length,
                FilePath = filePath,
                ExtractionStatus = DiagnosisExtractionStatus.Pending
            };

            diagnosisDocumentRepository.Add(diagnosisDocument);
            await unitOfWork.CompleteAsync();

            _ = Task.Run(() => ProcessTesseractExtractionAsync(diagnosisDocument.Id));

            _logger.LogInformation("Diagnosis document uploaded successfully with Tesseract. ID {DocumentId}", diagnosisDocument.Id);

            return new DiagnosisExtractionResponse
            {
                Document = new DiagnosisDocumentDto
                {
                    Id = diagnosisDocument.Id,
                    DocumentId = diagnosisDocument.DocumentId,
                    FileName = diagnosisDocument.FileName,
                    OriginalFileName = diagnosisDocument.OriginalFileName,
                    ContentType = diagnosisDocument.ContentType,
                    FileSize = diagnosisDocument.FileSize,
                    FilePath = diagnosisDocument.FilePath,
                    ExtractionStatus = diagnosisDocument.ExtractionStatus,
                    ExtractionStatusName = diagnosisDocument.ExtractionStatus.ToString(),
                    RetryCount = diagnosisDocument.RetryCount,
                    CreatedAt = diagnosisDocument.CreatedAt,
                    UpdatedAt = diagnosisDocument.UpdatedAt
                },
                ExtractedDiagnoses = new List<ExtractedDiagnosisDto>(),
                NeedsManualReview = false,
                Message = "Document uploaded successfully. Tesseract extraction in progress."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading diagnosis document with Tesseract for user {UserId}", userId);
            throw;
        }
    }

    public async Task<DiagnosisExtractionResponse> GetExtractionStatusAsync(int userId, int documentId)
    {
        using var scope = _scopeFactory.CreateScope();
        var diagnosisDocumentRepository = scope.ServiceProvider.GetRequiredService<IDiagnosisDocumentRepository>();
        var diagnosisRepository = scope.ServiceProvider.GetRequiredService<IDiagnosisRepository>();

        var document = await diagnosisDocumentRepository.GetByIdAsync(documentId);
        if (document == null || document.UserId != userId)
        {
            throw new InvalidOperationException("Document not found");
        }

        var diagnoses = await diagnosisRepository.GetByDiagnosisDocumentIdAsync(documentId);

        return new DiagnosisExtractionResponse
        {
            Document = new DiagnosisDocumentDto
            {
                Id = document.Id,
                DocumentId = document.DocumentId,
                FileName = document.FileName,
                OriginalFileName = document.OriginalFileName,
                ContentType = document.ContentType,
                FileSize = document.FileSize,
                FilePath = document.FilePath,
                ExtractionStatus = document.ExtractionStatus,
                ExtractionStatusName = document.ExtractionStatus.ToString(),
                ExtractedAt = document.ExtractedAt,
                ExtractionError = document.ExtractionError,
                RetryCount = document.RetryCount,
                CreatedAt = document.CreatedAt,
                UpdatedAt = document.UpdatedAt
            },
            ExtractedDiagnoses = diagnoses.Select(d => new ExtractedDiagnosisDto
            {
                DiagnosisName = d.DiagnosisName,
                ICDCode = null,
                Description = d.Notes,
                Severity = d.Severity,
                Status = d.Status,
                Notes = d.Notes,
                Confidence = null
            }).ToList(),
            NeedsManualReview = document.ExtractionStatus == DiagnosisExtractionStatus.Failed,
            Message = document.ExtractionStatus == DiagnosisExtractionStatus.Completed ? "Extraction completed" : "Extraction in progress"
        };
    }

    public async Task<DiagnosisExtractionResponse> RetryExtractionAsync(int userId, int documentId)
    {
        using var scope = _scopeFactory.CreateScope();
        var diagnosisDocumentRepository = scope.ServiceProvider.GetRequiredService<IDiagnosisDocumentRepository>();

        var document = await diagnosisDocumentRepository.GetByIdAsync(documentId);
        if (document == null || document.UserId != userId)
        {
            throw new InvalidOperationException("Document not found");
        }

        document.ExtractionStatus = DiagnosisExtractionStatus.Pending;
        document.RetryCount++;
        diagnosisDocumentRepository.Update(document);
        
        using var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await unitOfWork.CompleteAsync();

        _ = Task.Run(() => ProcessExtractionAsync(documentId));

        return await GetExtractionStatusAsync(userId, documentId);
    }

    public async Task<List<DiagnosisDto>> UpdateExtractedDiagnosesAsync(int userId, int documentId, List<UpdateExtractedDiagnosisRequest> updates)
    {
        using var scope = _scopeFactory.CreateScope();
        var diagnosisDocumentRepository = scope.ServiceProvider.GetRequiredService<IDiagnosisDocumentRepository>();
        var diagnosisRepository = scope.ServiceProvider.GetRequiredService<IDiagnosisRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var document = await diagnosisDocumentRepository.GetByIdAsync(documentId);
        if (document == null || document.UserId != userId)
        {
            throw new InvalidOperationException("Document not found");
        }

        var updatedDiagnoses = new List<Diagnosis>();
        foreach (var update in updates)
        {
            var diagnosis = await diagnosisRepository.GetByIdAsync(update.Id);
            if (diagnosis != null && diagnosis.UserId == userId)
            {
                diagnosis.DiagnosisName = update.DiagnosisName;
                diagnosis.Severity = update.Severity;
                diagnosis.Status = update.Status;
                diagnosis.Notes = update.Notes;
                diagnosis.UpdatedAt = DateTime.UtcNow;
                diagnosisRepository.Update(diagnosis);
                updatedDiagnoses.Add(diagnosis);
            }
        }

        document.ExtractionStatus = DiagnosisExtractionStatus.ManuallyEdited;
        diagnosisDocumentRepository.Update(document);
        await unitOfWork.CompleteAsync();

        return updatedDiagnoses.Select(d => new DiagnosisDto
        {
            Id = d.Id,
            DiagnosisName = d.DiagnosisName,
            DateDiagnosed = d.DateDiagnosed,
            DoctorName = d.DoctorName,
            Severity = d.Severity,
            Status = d.Status,
            Notes = d.Notes,
            CreatedAt = d.CreatedAt,
            UpdatedAt = d.UpdatedAt
        }).ToList();
    }

    public async Task<bool> DeleteDiagnosisDocumentAsync(int userId, int documentId)
    {
        using var scope = _scopeFactory.CreateScope();
        var diagnosisDocumentRepository = scope.ServiceProvider.GetRequiredService<IDiagnosisDocumentRepository>();
        var diagnosisRepository = scope.ServiceProvider.GetRequiredService<IDiagnosisRepository>();
        var documentRepository = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var document = await diagnosisDocumentRepository.GetByIdAsync(documentId);
        if (document == null || document.UserId != userId)
        {
            return false;
        }

        var diagnoses = await diagnosisRepository.GetByDiagnosisDocumentIdAsync(documentId);
        foreach (var diagnosis in diagnoses)
        {
            diagnosisRepository.Delete(diagnosis);
        }

        diagnosisDocumentRepository.Delete(document);

        if (document.DocumentId.HasValue)
        {
            var mainDocument = await documentRepository.GetByIdAsync(document.DocumentId.Value);
            if (mainDocument != null)
            {
                documentRepository.Delete(mainDocument);
            }
        }

        await unitOfWork.CompleteAsync();

        if (File.Exists(document.FilePath))
        {
            File.Delete(document.FilePath);
        }

        return true;
    }

    public async Task<DiagnosisDocumentWithDiagnosesDto?> GetDiagnosisDocumentWithDiagnosesAsync(int userId, int documentId)
    {
        using var scope = _scopeFactory.CreateScope();
        var diagnosisDocumentRepository = scope.ServiceProvider.GetRequiredService<IDiagnosisDocumentRepository>();
        var diagnosisRepository = scope.ServiceProvider.GetRequiredService<IDiagnosisRepository>();

        var document = await diagnosisDocumentRepository.GetByIdAsync(documentId);
        if (document == null || document.UserId != userId)
        {
            return null;
        }

        var diagnoses = await diagnosisRepository.GetByDiagnosisDocumentIdAsync(documentId);

        return new DiagnosisDocumentWithDiagnosesDto
        {
            Id = document.Id,
            DocumentId = document.DocumentId,
            FileName = document.FileName,
            OriginalFileName = document.OriginalFileName,
            ContentType = document.ContentType,
            FileSize = document.FileSize,
            FilePath = document.FilePath,
            ExtractionStatus = document.ExtractionStatus,
            ExtractionStatusName = document.ExtractionStatus.ToString(),
            ExtractedAt = document.ExtractedAt,
            ExtractionError = document.ExtractionError,
            RetryCount = document.RetryCount,
            CreatedAt = document.CreatedAt,
            UpdatedAt = document.UpdatedAt,
            Diagnoses = diagnoses.Select(d => new DiagnosisDto
            {
                Id = d.Id,
                DiagnosisName = d.DiagnosisName,
                DateDiagnosed = d.DateDiagnosed,
                DoctorName = d.DoctorName,
                Severity = d.Severity,
                Status = d.Status,
                Notes = d.Notes,
                CreatedAt = d.CreatedAt,
                UpdatedAt = d.UpdatedAt
            }).ToList()
        };
    }

    public async Task<PaginatedResponse<DiagnosisDocumentDto>> GetDiagnosisDocumentsAsync(int userId, int page = 1, int pageSize = 10, string? search = null)
    {
        using var scope = _scopeFactory.CreateScope();
        var diagnosisDocumentRepository = scope.ServiceProvider.GetRequiredService<IDiagnosisDocumentRepository>();

        var documents = await diagnosisDocumentRepository.GetByUserIdAsync(userId, page, pageSize, search);
        var totalCount = await diagnosisDocumentRepository.GetCountByUserIdAsync(userId, search);

        return new PaginatedResponse<DiagnosisDocumentDto>
        {
            Items = documents.Select(d => new DiagnosisDocumentDto
            {
                Id = d.Id,
                DocumentId = d.DocumentId,
                FileName = d.FileName,
                OriginalFileName = d.OriginalFileName,
                ContentType = d.ContentType,
                FileSize = d.FileSize,
                FilePath = d.FilePath,
                ExtractionStatus = d.ExtractionStatus,
                ExtractionStatusName = d.ExtractionStatus.ToString(),
                ExtractedAt = d.ExtractedAt,
                ExtractionError = d.ExtractionError,
                RetryCount = d.RetryCount,
                CreatedAt = d.CreatedAt,
                UpdatedAt = d.UpdatedAt
            }).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<bool> UpdateOriginalFileNameAsync(int userId, int documentId, string newFileName)
    {
        using var scope = _scopeFactory.CreateScope();
        var diagnosisDocumentRepository = scope.ServiceProvider.GetRequiredService<IDiagnosisDocumentRepository>();
        var documentRepository = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        try
        {
            var diagnosisDocument = await diagnosisDocumentRepository.GetByIdAsync(documentId);
            if (diagnosisDocument == null || diagnosisDocument.UserId != userId)
            {
                return false;
            }

            // Update DiagnosisDocument
            diagnosisDocument.OriginalFileName = newFileName;
            diagnosisDocument.UpdatedAt = DateTime.UtcNow;
            diagnosisDocumentRepository.Update(diagnosisDocument);

            // Also update the related Document if it exists
            if (diagnosisDocument.DocumentId.HasValue)
            {
                var document = await documentRepository.GetByIdAsync(diagnosisDocument.DocumentId.Value);
                if (document != null && document.UserId == userId)
                {
                    document.OriginalFileName = newFileName;
                    document.UpdatedAt = DateTime.UtcNow;
                    documentRepository.Update(document);
                }
            }

            await unitOfWork.CompleteAsync();
            _logger.LogInformation("Updated original file name for diagnosis document {DocumentId} to {FileName}", documentId, newFileName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating original file name for diagnosis document {DocumentId}", documentId);
            throw;
        }
    }

    private async Task ProcessExtractionAsync(int documentId)
    {
        using var scope = _scopeFactory.CreateScope();
        var diagnosisDocumentRepository = scope.ServiceProvider.GetRequiredService<IDiagnosisDocumentRepository>();
        var diagnosisRepository = scope.ServiceProvider.GetRequiredService<IDiagnosisRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var geminiService = scope.ServiceProvider.GetRequiredService<IGeminiService>();

        try
        {
            var document = await diagnosisDocumentRepository.GetByIdAsync(documentId);
            if (document == null)
            {
                _logger.LogError("Document {DocumentId} not found", documentId);
                return;
            }

            document.ExtractionStatus = DiagnosisExtractionStatus.Processing;
            document.RetryCount++;
            diagnosisDocumentRepository.Update(document);
            await unitOfWork.CompleteAsync();

            // Extract using Gemini
            using var fileStream = new FileStream(document.FilePath, FileMode.Open, FileAccess.Read);
            var extractedDiagnoses = await geminiService.ExtractDiagnosesAsync(fileStream, document.ContentType, document.FileName, document.UserId);

            // Save extracted diagnoses
            foreach (var extracted in extractedDiagnoses)
            {
                var diagnosis = new Diagnosis
                {
                    UserId = document.UserId,
                    DiagnosisDocumentId = document.Id,
                    DiagnosisName = extracted.DiagnosisName,
                    Severity = extracted.Severity,
                    Status = extracted.Status,
                    Notes = (extracted.Description != null ? $"Description: {extracted.Description}\n" : "") + 
                             (extracted.ICDCode != null ? $"ICD Code: {extracted.ICDCode}\n" : "") + 
                             $"AI extracted with confidence: {extracted.Confidence:P0}"
                };
                diagnosisRepository.Add(diagnosis);
            }

            document.ExtractionStatus = DiagnosisExtractionStatus.Completed;
            document.ExtractedAt = DateTime.UtcNow;
            document.RawExtractionData = System.Text.Json.JsonSerializer.Serialize(extractedDiagnoses);
            diagnosisDocumentRepository.Update(document);
            await unitOfWork.CompleteAsync();

            _logger.LogInformation("Successfully extracted {Count} diagnoses for document {DocumentId}", extractedDiagnoses.Count, documentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing diagnosis extraction for document {DocumentId}", documentId);
            
            var document = await diagnosisDocumentRepository.GetByIdAsync(documentId);
            if (document != null)
            {
                document.ExtractionStatus = DiagnosisExtractionStatus.Failed;
                document.ExtractionError = ex.Message;
                document.ExtractedAt = DateTime.UtcNow;
                diagnosisDocumentRepository.Update(document);
                await unitOfWork.CompleteAsync();
            }
        }
    }

    private async Task ProcessTesseractExtractionAsync(int documentId)
    {
        await ProcessExtractionAsync(documentId);
    }
}
