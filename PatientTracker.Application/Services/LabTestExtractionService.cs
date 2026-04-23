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

namespace PatientTracker.Application.Services;

public class LabTestExtractionService : ILabTestExtractionService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IGeminiService _geminiService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<LabTestExtractionService> _logger;
    private readonly IStringLocalizer<ErrorMessages> _localizer;

    public LabTestExtractionService(
        IServiceScopeFactory scopeFactory,
        IGeminiService geminiService,
        IConfiguration configuration,
        ILogger<LabTestExtractionService> logger,
        IStringLocalizer<ErrorMessages> localizer)
    {
        _scopeFactory = scopeFactory;
        _geminiService = geminiService;
        _configuration = configuration;
        _logger = logger;
        _localizer = localizer;
    }

    public async Task<LabTestExtractionResponse> UploadAndExtractAsync(int userId, UploadLabTestDocumentRequest request)
    {
        using var scope = _scopeFactory.CreateScope();
        var labTestDocumentRepository = scope.ServiceProvider.GetRequiredService<ILabTestDocumentRepository>();
        var documentRepository = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        try
        {
            _logger.LogInformation("Starting lab test document upload for user {UserId}", userId);

            // Validate file
            if (request.File == null || request.File.Length == 0)
            {
                throw new ValidationException(new Dictionary<string, string[]> { { "File", new[] { _localizer["NoFileProvided"].Value } } });
            }

            // Check file size (max 10MB)
            var maxSize = _configuration.GetValue<long>("LabTestExtraction:MaxFileSize", 10 * 1024 * 1024);
            if (request.File.Length > maxSize)
            {
                throw new ValidationException(new Dictionary<string, string[]> { { "FileSize", new[] { string.Format(_localizer["FileSizeExceedsMaximum"].Value, maxSize / (1024 * 1024)) } } });
            }

            // Create user folder structure like DocumentService
            var userFolder = userId.ToString();
            var documentTypeFolder = "lab-reports"; // Lab test documents are lab reports
            var fullUserFolder = Path.Combine(userFolder, documentTypeFolder);
            
            // Save the file
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(request.File.FileName)}";
            var uploadsPath = _configuration["Uploads:Path"] ?? "uploads";
            var userDirectory = Path.Combine(uploadsPath, fullUserFolder);
            var filePath = Path.Combine(userDirectory, fileName);
            
            Directory.CreateDirectory(userDirectory);
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await request.File.CopyToAsync(fileStream);
            }

            // Create Document entity first
            var document = new Document
            {
                UserId = userId,
                FileName = fileName,
                OriginalFileName = request.File.FileName,
                ContentType = request.File.ContentType,
                FileSize = request.File.Length,
                FilePath = filePath,
                DocumentType = PatientTracker.Domain.Enums.DocumentType.LabReport,
                ParentEntityType = PatientTracker.Domain.Enums.ParentEntityType.None,
                ParentEntityId = null
            };

            document = await documentRepository.AddAsync(document);
            await unitOfWork.CompleteAsync(); // Save Document first to get the ID

            // Create LabTestDocument entity with reference to Document
            var labTestDocument = new LabTestDocument
            {
                UserId = userId,
                DocumentId = document.Id,
                FileName = fileName,
                OriginalFileName = request.File.FileName,
                ContentType = request.File.ContentType,
                FileSize = request.File.Length,
                FilePath = filePath,
                ExtractionStatus = LabTestExtractionStatus.Pending
            };

            labTestDocumentRepository.Add(labTestDocument);
            await unitOfWork.CompleteAsync(); // Save LabTestDocument

            // Start extraction in background
            _ = Task.Run(async () => await ProcessExtractionAsync(labTestDocument.Id));

            // Return response
            return new LabTestExtractionResponse
            {
                Document = MapToDto(labTestDocument),
                NeedsManualReview = false,
                Message = "Document uploaded successfully. Extraction in progress..."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading lab test document for user {UserId}", userId);
            throw;
        }
    }

    private async Task ProcessExtractionAsync(int documentId)
    {
        using var scope = _scopeFactory.CreateScope();
        var labTestDocumentRepository = scope.ServiceProvider.GetRequiredService<ILabTestDocumentRepository>();
        var labTestRepository = scope.ServiceProvider.GetRequiredService<ILabTestRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        try
        {
            var document = await labTestDocumentRepository.GetByIdAsync(documentId);
            if (document == null)
            {
                _logger.LogError("Document {DocumentId} not found", documentId);
                return;
            }

            // Update status to Processing
            document.ExtractionStatus = LabTestExtractionStatus.Processing;
            document.RetryCount++;
            labTestDocumentRepository.Update(document);
            await unitOfWork.CompleteAsync();

            // Extract using Gemini
            using var fileStream = new FileStream(document.FilePath, FileMode.Open, FileAccess.Read);
            var extractedTests = await _geminiService.ExtractLabTestsAsync(fileStream, document.ContentType, document.FileName);

            // Save extracted tests
            var labTests = new List<LabTest>();
            foreach (var extracted in extractedTests)
            {
                var labTest = new LabTest
                {
                    UserId = document.UserId,
                    LabTestDocumentId = document.Id,
                    TestName = extracted.TestName,
                    ResultValue = extracted.ResultValue,
                    ResultUnit = extracted.ResultUnit,
                    NormalRange = extracted.NormalRange,
                    Status = extracted.Status ?? "normal",
                    TestDate = DateTime.UtcNow, // Can be updated later
                    Notes = $"AI extracted with confidence: {extracted.Confidence:P0}"
                };
                labTests.Add(labTest);
            }

            // Save all lab tests
            labTestRepository.AddRange(labTests);

            // Update document status
            document.ExtractionStatus = LabTestExtractionStatus.Completed;
            document.ExtractedAt = DateTime.UtcNow;
            document.RawExtractionData = System.Text.Json.JsonSerializer.Serialize(extractedTests);
            labTestDocumentRepository.Update(document);

            await unitOfWork.CompleteAsync();

            _logger.LogInformation("Successfully extracted {Count} lab tests for document {DocumentId}", extractedTests.Count, documentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing extraction for document {DocumentId}", documentId);
            
            // Update document with error
            var document = await labTestDocumentRepository.GetByIdAsync(documentId);
            if (document != null)
            {
                document.ExtractionStatus = LabTestExtractionStatus.Failed;
                document.ExtractionError = ex.Message;
                labTestDocumentRepository.Update(document);
                await unitOfWork.CompleteAsync();
            }
        }
    }

    public async Task<LabTestExtractionResponse> GetExtractionStatusAsync(int userId, int documentId)
    {
        using var scope = _scopeFactory.CreateScope();
        var labTestDocumentRepository = scope.ServiceProvider.GetRequiredService<ILabTestDocumentRepository>();
        var labTestRepository = scope.ServiceProvider.GetRequiredService<ILabTestRepository>();

        try
        {
            var document = await labTestDocumentRepository.GetByIdAsync(documentId);
            if (document == null || document.UserId != userId)
            {
                throw new BusinessException(ErrorCodes.DocumentNotFound, _localizer["DocumentNotFound"]);
            }

            var labTests = await labTestRepository.GetByDocumentIdAsync(documentId);
            var documentDto = MapToDto(document);
            documentDto.ExtractedLabTests = labTests.Select(MapToDto).ToList();

            var needsManualReview = document.ExtractionStatus == LabTestExtractionStatus.Failed ||
                                   (document.ExtractionStatus == LabTestExtractionStatus.Completed && 
                                    labTests.Any(t => t.Notes?.Contains("confidence") == true));

            return new LabTestExtractionResponse
            {
                Document = documentDto,
                ExtractedTests = labTests.Select(t => new ExtractedLabTestDto
                {
                    TestName = t.TestName,
                    ResultValue = t.ResultValue,
                    ResultUnit = t.ResultUnit,
                    NormalRange = t.NormalRange,
                    Status = t.Status,
                    Confidence = ExtractConfidenceFromNotes(t.Notes)
                }).ToList(),
                NeedsManualReview = needsManualReview,
                Message = GetStatusMessage(document.ExtractionStatus)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting extraction status for document {DocumentId}", documentId);
            throw;
        }
    }

    public async Task<LabTestExtractionResponse> RetryExtractionAsync(int userId, int documentId)
    {
        using var scope = _scopeFactory.CreateScope();
        var labTestDocumentRepository = scope.ServiceProvider.GetRequiredService<ILabTestDocumentRepository>();
        var labTestRepository = scope.ServiceProvider.GetRequiredService<ILabTestRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        try
        {
            var document = await labTestDocumentRepository.GetByIdAsync(documentId);
            if (document == null || document.UserId != userId)
            {
                throw new BusinessException(ErrorCodes.DocumentNotFound, _localizer["DocumentNotFound"]);
            }

            // Delete existing extracted tests
            var existingTests = await labTestRepository.GetByDocumentIdAsync(documentId);
            labTestRepository.DeleteRange(existingTests);

            // Reset document status
            document.ExtractionStatus = LabTestExtractionStatus.Pending;
            document.ExtractionError = null;
            labTestDocumentRepository.Update(document);
            await unitOfWork.CompleteAsync();

            // Start extraction again
            _ = Task.Run(async () => await ProcessExtractionAsync(documentId));

            return new LabTestExtractionResponse
            {
                Document = MapToDto(document),
                NeedsManualReview = false,
                Message = "Extraction retry initiated..."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrying extraction for document {DocumentId}", documentId);
            throw;
        }
    }

    public async Task<List<LabTestDto>> UpdateExtractedTestsAsync(int userId, int documentId, List<UpdateExtractedLabTestRequest> updates)
    {
        using var scope = _scopeFactory.CreateScope();
        var labTestDocumentRepository = scope.ServiceProvider.GetRequiredService<ILabTestDocumentRepository>();
        var labTestRepository = scope.ServiceProvider.GetRequiredService<ILabTestRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        try
        {
            var document = await labTestDocumentRepository.GetByIdAsync(documentId);
            if (document == null || document.UserId != userId)
            {
                throw new BusinessException(ErrorCodes.DocumentNotFound, _localizer["DocumentNotFound"]);
            }

            var labTests = await labTestRepository.GetByDocumentIdAsync(documentId);
            
            foreach (var update in updates)
            {
                var labTest = labTests.FirstOrDefault(t => t.Id == update.Id);
                if (labTest != null)
                {
                    labTest.TestName = update.TestName;
                    labTest.ResultValue = update.ResultValue;
                    labTest.ResultUnit = update.ResultUnit;
                    labTest.NormalRange = update.NormalRange;
                    labTest.Status = update.Status;
                    labTest.Notes = update.Notes;
                    labTest.UpdatedAt = DateTime.UtcNow;
                    labTestRepository.Update(labTest);
                }
            }

            // Mark as manually edited
            document.ExtractionStatus = LabTestExtractionStatus.ManuallyEdited;
            labTestDocumentRepository.Update(document);
            
            await unitOfWork.CompleteAsync();

            var updatedTests = await labTestRepository.GetByDocumentIdAsync(documentId);
            return updatedTests.Select(MapToDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating extracted tests for document {DocumentId}", documentId);
            throw;
        }
    }

    public async Task<bool> DeleteLabTestDocumentAsync( int userId, int labTestDocumentId)
    {
        using var scope = _scopeFactory.CreateScope();
        var labTestDocumentRepository = scope.ServiceProvider.GetRequiredService<ILabTestDocumentRepository>();
        var labTestRepository = scope.ServiceProvider.GetRequiredService<ILabTestRepository>();
        var documentRepository = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        try
        {
            // 1. Get the lab test document
            var labTestDocument = await labTestDocumentRepository.GetByIdAsync(labTestDocumentId);
            if (labTestDocument == null || labTestDocument.UserId != userId)
            {
                return false;
            }

            // 2. Delete extracted lab tests first (child records)
            var labTests = await labTestRepository.GetByDocumentIdAsync(labTestDocumentId);
            if (labTests.Any())
            {
                labTestRepository.DeleteRange(labTests);
            }

            // 3. Delete the actual document file if DocumentId exists
            if (labTestDocument.DocumentId.HasValue)
            {
                var document = await documentRepository.GetByIdAsync(labTestDocument.DocumentId.Value);
                if (document != null)
                {
                    // Delete physical file
                    if (File.Exists(document.FilePath))
                    {
                        File.Delete(document.FilePath);
                    }

                    // Delete thumbnail if exists
                    if (!string.IsNullOrEmpty(document.ThumbnailPath) && File.Exists(document.ThumbnailPath))
                    {
                        File.Delete(document.ThumbnailPath);
                    }

                    // Delete document record
                    documentRepository.Delete(document);
                }
            }

            // 4. Delete the lab test document
            labTestDocumentRepository.Delete(labTestDocument);

            await unitOfWork.CompleteAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting lab test document {DocumentId}", labTestDocumentId);
            throw;
        }
    }
    private LabTestDocumentDto MapToDto(LabTestDocument document)
    {
        return new LabTestDocumentDto
        {
            Id = document.Id,
            DocumentId = document.DocumentId,
            FileName = document.FileName,
            OriginalFileName = document.OriginalFileName,
            ContentType = document.ContentType,
            FileSize = document.FileSize,
            FilePath = document.FilePath,
            ThumbnailPath = document.ThumbnailPath,
            ExtractionStatus = document.ExtractionStatus,
            ExtractionStatusName = document.ExtractionStatus.ToString(),
            ExtractedAt = document.ExtractedAt,
            ExtractionError = document.ExtractionError,
            RetryCount = document.RetryCount,
            CreatedAt = document.CreatedAt,
            UpdatedAt = document.UpdatedAt
        };
    }

    private LabTestDto MapToDto(LabTest labTest)
    {
        return new LabTestDto
        {
            Id = labTest.Id,
            TestName = labTest.TestName,
            ResultValue = labTest.ResultValue,
            ResultUnit = labTest.ResultUnit,
            NormalRange = labTest.NormalRange,
            Status = labTest.Status,
            Notes = labTest.Notes,
            TestDate = labTest.TestDate,
            ReportUrl = labTest.ReportUrl,
            CreatedAt = labTest.CreatedAt,
            UpdatedAt = labTest.UpdatedAt
        };
    }

    public async Task<LabTestDocumentWithTestsDto?> GetLabTestDocumentWithTestsAsync(int userId, int documentId)
    {
        using var scope = _scopeFactory.CreateScope();
        var labTestDocumentRepository = scope.ServiceProvider.GetRequiredService<ILabTestDocumentRepository>();
        var labTestRepository = scope.ServiceProvider.GetRequiredService<ILabTestRepository>();

        try
        {
            var document = await labTestDocumentRepository.GetByIdAsync(documentId);
            if (document == null || document.UserId != userId)
            {
                return null;
            }

            var labTests = await labTestRepository.GetByDocumentIdAsync(documentId);
            
            // Create base URL for document access
            var baseUrl = _configuration["ApiBaseUrl"] ?? "https://localhost:7001";
            var documentUrl = $"{baseUrl}/api/documents/file/{document.DocumentId}";
            var thumbnailUrl = !string.IsNullOrEmpty(document.ThumbnailPath) 
                ? $"{baseUrl}/api/documents/thumbnail/{document.DocumentId}" 
                : null;

            var result = new LabTestDocumentWithTestsDto
            {
                Id = document.Id,
                DocumentId = document.DocumentId,
                FileName = document.FileName,
                OriginalFileName = document.OriginalFileName,
                ContentType = document.ContentType,
                FileSize = document.FileSize,
                FilePath = document.FilePath,
                ThumbnailPath = document.ThumbnailPath,
                DocumentUrl = documentUrl,
                ThumbnailUrl = thumbnailUrl,
                ExtractionStatus = document.ExtractionStatus,
                ExtractionStatusName = document.ExtractionStatus.ToString(),
                ExtractedAt = document.ExtractedAt,
                ExtractionError = document.ExtractionError,
                RetryCount = document.RetryCount,
                CreatedAt = document.CreatedAt,
                UpdatedAt = document.UpdatedAt,
                LabTests = labTests.Select(MapToDto).ToList()
            };

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting lab test document with tests for document {DocumentId}", documentId);
            throw;
        }
    }

    public async Task<PaginatedResponse<LabTestDocumentDto>> GetLabTestDocumentsAsync(int userId, int page = 1, int pageSize = 10, string? search = null)
    {
        using var scope = _scopeFactory.CreateScope();
        var labTestDocumentRepository = scope.ServiceProvider.GetRequiredService<ILabTestDocumentRepository>();

        try
        {
            // Validate pagination parameters
            page = Math.Max(1, page);
            pageSize = Math.Max(1, Math.Min(100, pageSize));

            // Get total count
            var totalCount = await labTestDocumentRepository.CountByUserIdAsync(userId, search);
            
            // Get paginated documents
            var documents = await labTestDocumentRepository.GetByUserIdAsync(userId, page, pageSize, search);
            
            // Map to DTOs
            var documentDtos = documents.Select(MapToDto).ToList();

            return new PaginatedResponse<LabTestDocumentDto>
            {
                Items = documentDtos,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting lab test documents for user {UserId}", userId);
            throw;
        }
    }

    private decimal? ExtractConfidenceFromNotes(string? notes)
    {
        if (string.IsNullOrEmpty(notes))
            return null;

        // Look for confidence in the format "AI extracted with confidence: 99%"
        if (notes.Contains("confidence:"))
        {
            var parts = notes.Split(':');
            if (parts.Length > 1)
            {
                var confidenceStr = parts[1].Trim().TrimEnd('%');
                if (double.TryParse(confidenceStr, out var confidence))
                {
                    return (decimal)(confidence / 100); // Convert percentage to decimal
                }
            }
        }
        return null;
    }

    private string GetStatusMessage(LabTestExtractionStatus status)
    {
        return status switch
        {
            LabTestExtractionStatus.Pending => "Waiting to process...",
            LabTestExtractionStatus.Processing => "Extracting data using AI...",
            LabTestExtractionStatus.Completed => "Extraction completed successfully",
            LabTestExtractionStatus.Failed => "Extraction failed. Please retry or enter manually.",
            LabTestExtractionStatus.ManuallyEdited => "Data has been manually edited",
            _ => "Unknown status"
        };
    }
}
