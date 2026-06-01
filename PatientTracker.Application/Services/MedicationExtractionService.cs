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

public class MedicationExtractionService : IMedicationExtractionService
{
    private readonly IMedicationDocumentRepository _medicationDocumentRepository;
    private readonly IMedicationRepository _medicationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MedicationExtractionService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IGeminiService _geminiService;
    private readonly IDocumentService _documentService;
    private readonly IStringLocalizer<ErrorMessages> _localizer;

    public MedicationExtractionService(
        IMedicationDocumentRepository medicationDocumentRepository,
        IMedicationRepository medicationRepository,
        IUnitOfWork unitOfWork,
        IConfiguration configuration,
        ILogger<MedicationExtractionService> logger,
        IServiceScopeFactory scopeFactory,
        IGeminiService geminiService,
        IDocumentService documentService,
        IStringLocalizer<ErrorMessages> localizer)
    {
        _medicationDocumentRepository = medicationDocumentRepository;
        _medicationRepository = medicationRepository;
        _unitOfWork = unitOfWork;
        _configuration = configuration;
        _logger = logger;
        _scopeFactory = scopeFactory;
        _geminiService = geminiService;
        _documentService = documentService;
        _localizer = localizer;
    }

    public async Task<MedicationExtractionResponse> UploadAndExtractAsync(int userId, UploadMedicationDocumentRequest request)
    {
        using var scope = _scopeFactory.CreateScope();
        var medicationDocumentRepository = scope.ServiceProvider.GetRequiredService<IMedicationDocumentRepository>();
        var documentRepository = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        try
        {
            _logger.LogInformation("Starting medication document upload for user {UserId}", userId);

            // Validate file
            if (request.File == null || request.File.Length == 0)
            {
                throw new InvalidOperationException(_localizer["FileRequired"]);
            }

            var maxFileSize = _configuration.GetValue<long>("Uploads:MaxFileSize", 10485760); // 10MB default
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
                filePath = await _documentService.SaveOptimizedImageAsync(request.File, $"medications/{userId}");
                contentType = "image/webp";
            }
            else
            {
                // Save document as-is
                filePath = await _documentService.SaveDocumentAsync(request.File, $"medications/{userId}");
                contentType = request.File.ContentType;
            }

            // Create document record
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

            // Create medication document record
            var medicationDocument = new MedicationDocument
            {
                UserId = userId,
                DocumentId = document.Id,
                FileName = Path.GetFileName(filePath),
                OriginalFileName = request.File.FileName,
                ContentType = contentType,
                FileSize = request.File.Length,
                FilePath = filePath,
                ExtractionStatus = MedicationExtractionStatus.Pending
            };

            medicationDocumentRepository.Add(medicationDocument);
            await unitOfWork.CompleteAsync();

            // Start background extraction
            _ = Task.Run(() => ProcessExtractionAsync(medicationDocument.Id));

            _logger.LogInformation("Medication document uploaded successfully with ID {DocumentId}", medicationDocument.Id);

            return new MedicationExtractionResponse
            {
                Document = new MedicationDocumentDto
                {
                    Id = medicationDocument.Id,
                    DocumentId = medicationDocument.DocumentId,
                    FileName = medicationDocument.FileName,
                    OriginalFileName = medicationDocument.OriginalFileName,
                    ContentType = medicationDocument.ContentType,
                    FileSize = medicationDocument.FileSize,
                    FilePath = medicationDocument.FilePath,
                    ExtractionStatus = medicationDocument.ExtractionStatus,
                    ExtractionStatusName = medicationDocument.ExtractionStatus.ToString(),
                    RetryCount = medicationDocument.RetryCount,
                    CreatedAt = medicationDocument.CreatedAt,
                    UpdatedAt = medicationDocument.UpdatedAt
                },
                ExtractedMedications = new List<ExtractedMedicationDto>(),
                NeedsManualReview = false,
                Message = "Document uploaded successfully. Extraction in progress."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading medication document for user {UserId}", userId);
            throw;
        }
    }

    public async Task<MedicationExtractionResponse> UploadAndExtractTesseractAsync(int userId, UploadMedicationDocumentRequest request)
    {
        using var scope = _scopeFactory.CreateScope();
        var medicationDocumentRepository = scope.ServiceProvider.GetRequiredService<IMedicationDocumentRepository>();
        var documentRepository = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        try
        {
            _logger.LogInformation("Starting medication document upload with Tesseract for user {UserId}", userId);

            // Validate file
            if (request.File == null || request.File.Length == 0)
            {
                throw new InvalidOperationException(_localizer["FileRequired"]);
            }

            var maxFileSize = _configuration.GetValue<long>("Uploads:MaxFileSize", 10485760);
            if (request.File.Length > maxFileSize)
            {
                throw new InvalidOperationException(_localizer["FileSizeExceeded"]);
            }

            // Save file
            var uploadsPath = _configuration["Uploads:Path"] ?? "uploads";
            var userFolderPath = Path.Combine(uploadsPath, "medications", userId.ToString());
            Directory.CreateDirectory(userFolderPath);

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(request.File.FileName)}";
            var filePath = Path.Combine(userFolderPath, fileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await request.File.CopyToAsync(fileStream);
            }

            // Create document record
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

            // Create medication document record
            var medicationDocument = new MedicationDocument
            {
                UserId = userId,
                DocumentId = document.Id,
                FileName = fileName,
                OriginalFileName = request.File.FileName,
                ContentType = request.File.ContentType,
                FileSize = request.File.Length,
                FilePath = filePath,
                ExtractionStatus = MedicationExtractionStatus.Pending
            };

            medicationDocumentRepository.Add(medicationDocument);
            await unitOfWork.CompleteAsync();

            // Start background extraction with Tesseract
            _ = Task.Run(() => ProcessTesseractExtractionAsync(medicationDocument.Id));

            _logger.LogInformation("Medication document uploaded successfully with Tesseract. ID {DocumentId}", medicationDocument.Id);

            return new MedicationExtractionResponse
            {
                Document = new MedicationDocumentDto
                {
                    Id = medicationDocument.Id,
                    DocumentId = medicationDocument.DocumentId,
                    FileName = medicationDocument.FileName,
                    OriginalFileName = medicationDocument.OriginalFileName,
                    ContentType = medicationDocument.ContentType,
                    FileSize = medicationDocument.FileSize,
                    FilePath = medicationDocument.FilePath,
                    ExtractionStatus = medicationDocument.ExtractionStatus,
                    ExtractionStatusName = medicationDocument.ExtractionStatus.ToString(),
                    RetryCount = medicationDocument.RetryCount,
                    CreatedAt = medicationDocument.CreatedAt,
                    UpdatedAt = medicationDocument.UpdatedAt
                },
                ExtractedMedications = new List<ExtractedMedicationDto>(),
                NeedsManualReview = false,
                Message = "Document uploaded successfully. Tesseract extraction in progress."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading medication document with Tesseract for user {UserId}", userId);
            throw;
        }
    }

    public async Task<MedicationExtractionResponse> GetExtractionStatusAsync(int userId, int documentId)
    {
        using var scope = _scopeFactory.CreateScope();
        var medicationDocumentRepository = scope.ServiceProvider.GetRequiredService<IMedicationDocumentRepository>();
        var medicationRepository = scope.ServiceProvider.GetRequiredService<IMedicationRepository>();

        var document = await medicationDocumentRepository.GetByIdAsync(documentId);
        if (document == null || document.UserId != userId)
        {
            throw new InvalidOperationException("Document not found");
        }

        var medications = await medicationRepository.GetByMedicationDocumentIdAsync(documentId);

        return new MedicationExtractionResponse
        {
            Document = new MedicationDocumentDto
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
            ExtractedMedications = medications.Select(m => new ExtractedMedicationDto
            {
                MedicationName = m.Name,
                Dosage = m.Dosage,
                Frequency = m.Frequency,
                Route = null,
                Duration = null,
                Instructions = m.Notes,
                Confidence = null
            }).ToList(),
            NeedsManualReview = document.ExtractionStatus == MedicationExtractionStatus.Failed,
            Message = document.ExtractionStatus == MedicationExtractionStatus.Completed ? "Extraction completed" : "Extraction in progress"
        };
    }

    public async Task<MedicationExtractionResponse> RetryExtractionAsync(int userId, int documentId)
    {
        using var scope = _scopeFactory.CreateScope();
        var medicationDocumentRepository = scope.ServiceProvider.GetRequiredService<IMedicationDocumentRepository>();

        var document = await medicationDocumentRepository.GetByIdAsync(documentId);
        if (document == null || document.UserId != userId)
        {
            throw new InvalidOperationException("Document not found");
        }

        document.ExtractionStatus = MedicationExtractionStatus.Pending;
        document.RetryCount++;
        medicationDocumentRepository.Update(document);
        
        using var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await unitOfWork.CompleteAsync();

        // Start background extraction
        _ = Task.Run(() => ProcessExtractionAsync(documentId));

        return await GetExtractionStatusAsync(userId, documentId);
    }

    public async Task<List<MedicationDto>> UpdateExtractedMedicationsAsync(int userId, int documentId, List<UpdateExtractedMedicationRequest> updates)
    {
        using var scope = _scopeFactory.CreateScope();
        var medicationDocumentRepository = scope.ServiceProvider.GetRequiredService<IMedicationDocumentRepository>();
        var medicationRepository = scope.ServiceProvider.GetRequiredService<IMedicationRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var document = await medicationDocumentRepository.GetByIdAsync(documentId);
        if (document == null || document.UserId != userId)
        {
            throw new InvalidOperationException("Document not found");
        }

        var updatedMedications = new List<Medication>();
        foreach (var update in updates)
        {
            var medication = await medicationRepository.GetByIdAsync(update.Id);
            if (medication != null && medication.UserId == userId)
            {
                medication.Name = update.MedicationName;
                medication.Dosage = update.Dosage;
                medication.Frequency = update.Frequency;
                medication.Notes = update.Instructions;
                medication.UpdatedAt = DateTime.UtcNow;
                medicationRepository.Update(medication);
                updatedMedications.Add(medication);
            }
        }

        document.ExtractionStatus = MedicationExtractionStatus.ManuallyEdited;
        medicationDocumentRepository.Update(document);
        await unitOfWork.CompleteAsync();

        return updatedMedications.Select(m => new MedicationDto
        {
            Id = m.Id,
            Name = m.Name,
            Dosage = m.Dosage,
            Frequency = m.Frequency,
            StartDate = m.StartDate,
            EndDate = m.EndDate,
            IsCurrent = m.IsCurrent,
            Notes = m.Notes,
            PrescriptionUrl = m.PrescriptionUrl,
            CreatedAt = m.CreatedAt,
            UpdatedAt = m.UpdatedAt
        }).ToList();
    }

    public async Task<bool> DeleteMedicationDocumentAsync(int userId, int documentId)
    {
        using var scope = _scopeFactory.CreateScope();
        var medicationDocumentRepository = scope.ServiceProvider.GetRequiredService<IMedicationDocumentRepository>();
        var medicationRepository = scope.ServiceProvider.GetRequiredService<IMedicationRepository>();
        var documentRepository = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var document = await medicationDocumentRepository.GetByIdAsync(documentId);
        if (document == null || document.UserId != userId)
        {
            return false;
        }

        // Delete associated medications
        var medications = await medicationRepository.GetByMedicationDocumentIdAsync(documentId);
        foreach (var medication in medications)
        {
            medicationRepository.Delete(medication);
        }

        // Delete document
        medicationDocumentRepository.Delete(document);

        // Delete main document if exists
        if (document.DocumentId.HasValue)
        {
            var mainDocument = await documentRepository.GetByIdAsync(document.DocumentId.Value);
            if (mainDocument != null)
            {
                documentRepository.Delete(mainDocument);
            }
        }

        await unitOfWork.CompleteAsync();

        // Delete file
        if (File.Exists(document.FilePath))
        {
            File.Delete(document.FilePath);
        }

        return true;
    }

    public async Task<MedicationDocumentWithMedicationsDto?> GetMedicationDocumentWithMedicationsAsync(int userId, int documentId)
    {
        using var scope = _scopeFactory.CreateScope();
        var medicationDocumentRepository = scope.ServiceProvider.GetRequiredService<IMedicationDocumentRepository>();
        var medicationRepository = scope.ServiceProvider.GetRequiredService<IMedicationRepository>();

        var document = await medicationDocumentRepository.GetByIdAsync(documentId);
        if (document == null || document.UserId != userId)
        {
            return null;
        }

        var medications = await medicationRepository.GetByMedicationDocumentIdAsync(document.Id);

        return new MedicationDocumentWithMedicationsDto
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
            Medications = medications.Select(m => new MedicationDto
            {
                Id = m.Id,
                Name = m.Name,
                Dosage = m.Dosage,
                Frequency = m.Frequency,
                StartDate = m.StartDate,
                EndDate = m.EndDate,
                IsCurrent = m.IsCurrent,
                Notes = m.Notes,
                PrescriptionUrl = m.PrescriptionUrl,
                CreatedAt = m.CreatedAt,
                UpdatedAt = m.UpdatedAt
            }).ToList()
        };
    }

    public async Task<PaginatedResponse<MedicationDocumentDto>> GetMedicationDocumentsAsync(int userId, int page = 1, int pageSize = 10, string? search = null)
    {
        using var scope = _scopeFactory.CreateScope();
        var medicationDocumentRepository = scope.ServiceProvider.GetRequiredService<IMedicationDocumentRepository>();

        var documents = await medicationDocumentRepository.GetByUserIdAsync(userId, page, pageSize, search);
        var totalCount = await medicationDocumentRepository.GetCountByUserIdAsync(userId, search);

        return new PaginatedResponse<MedicationDocumentDto>
        {
            Items = documents.Select(d => new MedicationDocumentDto
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
        var medicationDocumentRepository = scope.ServiceProvider.GetRequiredService<IMedicationDocumentRepository>();
        var documentRepository = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        try
        {
            var medicationDocument = await medicationDocumentRepository.GetByIdAsync(documentId);
            if (medicationDocument == null || medicationDocument.UserId != userId)
            {
                return false;
            }

            // Update MedicationDocument
            medicationDocument.OriginalFileName = newFileName;
            medicationDocument.UpdatedAt = DateTime.UtcNow;
            medicationDocumentRepository.Update(medicationDocument);

            // Also update the related Document if it exists
            if (medicationDocument.DocumentId.HasValue)
            {
                var document = await documentRepository.GetByIdAsync(medicationDocument.DocumentId.Value);
                if (document != null && document.UserId == userId)
                {
                    document.OriginalFileName = newFileName;
                    document.UpdatedAt = DateTime.UtcNow;
                    documentRepository.Update(document);
                }
            }

            await unitOfWork.CompleteAsync();
            _logger.LogInformation("Updated original file name for medication document {DocumentId} to {FileName}", documentId, newFileName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating original file name for medication document {DocumentId}", documentId);
            throw;
        }
    }

    private async Task ProcessExtractionAsync(int documentId)
    {
        using var scope = _scopeFactory.CreateScope();
        var medicationDocumentRepository = scope.ServiceProvider.GetRequiredService<IMedicationDocumentRepository>();
        var medicationRepository = scope.ServiceProvider.GetRequiredService<IMedicationRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var geminiService = scope.ServiceProvider.GetRequiredService<IGeminiService>();

        try
        {
            var document = await medicationDocumentRepository.GetByIdAsync(documentId);
            if (document == null)
            {
                _logger.LogError("Document {DocumentId} not found", documentId);
                return;
            }

            document.ExtractionStatus = MedicationExtractionStatus.Processing;
            document.RetryCount++;
            medicationDocumentRepository.Update(document);
            await unitOfWork.CompleteAsync();

            // Extract using Gemini
            using var fileStream = new FileStream(document.FilePath, FileMode.Open, FileAccess.Read);
            var extractedMedications = await geminiService.ExtractMedicationsAsync(fileStream, document.ContentType, document.FileName, document.UserId);

            // Save extracted medications
            foreach (var extracted in extractedMedications)
            {
                var medication = new Medication
                {
                    UserId = document.UserId,
                    MedicationDocumentId = document.Id,
                    Name = extracted.MedicationName,
                    Dosage = extracted.Dosage,
                    Frequency = extracted.Frequency,
                    Notes = extracted.Instructions + (extracted.Route != null ? $"\nRoute: {extracted.Route}" : "") + (extracted.Duration != null ? $"\nDuration: {extracted.Duration}" : "") + $"\nAI extracted with confidence: {extracted.Confidence:P0}"
                };
                medicationRepository.Add(medication);
            }

            document.ExtractionStatus = MedicationExtractionStatus.Completed;
            document.ExtractedAt = DateTime.UtcNow;
            document.RawExtractionData = System.Text.Json.JsonSerializer.Serialize(extractedMedications);
            medicationDocumentRepository.Update(document);
            await unitOfWork.CompleteAsync();

            _logger.LogInformation("Successfully extracted {Count} medications for document {DocumentId}", extractedMedications.Count, documentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing medication extraction for document {DocumentId}", documentId);
            
            var document = await medicationDocumentRepository.GetByIdAsync(documentId);
            if (document != null)
            {
                document.ExtractionStatus = MedicationExtractionStatus.Failed;
                document.ExtractionError = ex.Message;
                document.ExtractedAt = DateTime.UtcNow;
                medicationDocumentRepository.Update(document);
                await unitOfWork.CompleteAsync();
            }
        }
    }

    private async Task ProcessTesseractExtractionAsync(int documentId)
    {
        // Placeholder for Tesseract extraction
        // For now, fall back to Gemini extraction
        await ProcessExtractionAsync(documentId);
    }
}
