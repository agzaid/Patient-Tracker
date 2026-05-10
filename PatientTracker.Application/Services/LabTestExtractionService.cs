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

    public async Task<LabTestExtractionResponse> UploadAndExtractTesseractAsync(int userId, UploadLabTestDocumentRequest request)
    {
        using var scope = _scopeFactory.CreateScope();
        var labTestDocumentRepository = scope.ServiceProvider.GetRequiredService<ILabTestDocumentRepository>();
        var documentRepository = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        try
        {
            _logger.LogInformation("Starting Tesseract lab test document upload for user {UserId}", userId);

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
            var documentTypeFolder = "lab-reports";
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
            await unitOfWork.CompleteAsync();

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
            await unitOfWork.CompleteAsync();

            // Start Tesseract extraction in background
            _ = Task.Run(async () => await ProcessTesseractExtractionAsync(labTestDocument.Id));

            // Return response
            return new LabTestExtractionResponse
            {
                Document = MapToDto(labTestDocument),
                NeedsManualReview = false,
                Message = "Document uploaded successfully. Tesseract extraction in progress..."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading Tesseract lab test document for user {UserId}", userId);
            throw;
        }
    }

    private async Task ProcessTesseractExtractionAsync(int documentId)
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

            // Preprocess image for better OCR
            var preprocessedImagePath = await PreprocessImageAsync(document.FilePath);

            // Extract using Tesseract OCR
            var extractedText = await ExtractTextWithTesseractAsync(preprocessedImagePath);

            // Cleanup preprocessed image
            if (preprocessedImagePath != document.FilePath && File.Exists(preprocessedImagePath))
            {
                File.Delete(preprocessedImagePath);
            }

            if (string.IsNullOrEmpty(extractedText))
            {
                document.ExtractionStatus = LabTestExtractionStatus.Failed;
                document.ExtractionError = "Tesseract OCR failed to extract text";
                document.ExtractedAt = DateTime.UtcNow;
                labTestDocumentRepository.Update(document);
                await unitOfWork.CompleteAsync();
                return;
            }

            // Use Gemini AI to parse the extracted text into structured lab tests
            var extractedTests = await ParseExtractedTextWithGeminiAsync(extractedText, document.UserId);

            // Save extracted tests
            var labTests = new List<LabTest>();
            foreach (var extracted in extractedTests)
            {
                var labTest = new LabTest
                {
                    UserId = document.UserId,
                    LabTestDocumentId = document.Id,
                    TestName = extracted.TestName,
                    TestDate = DateTime.UtcNow,
                    ResultValue = extracted.ResultValue,
                    ResultUnit = extracted.ResultUnit,
                    NormalRange = extracted.NormalRange,
                    Status = extracted.Status,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                labTests.Add(labTest);
            }

            labTestRepository.AddRange(labTests);

            // Update document status
            document.ExtractionStatus = LabTestExtractionStatus.Completed;
            document.ExtractedAt = DateTime.UtcNow;
            labTestDocumentRepository.Update(document);
            await unitOfWork.CompleteAsync();

            _logger.LogInformation("Tesseract+Gemini extraction completed for document {DocumentId}. Extracted {Count} lab tests", documentId, extractedTests.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Tesseract extraction for document {DocumentId}", documentId);
            
            var document = await labTestDocumentRepository.GetByIdAsync(documentId);
            if (document != null)
            {
                document.ExtractionStatus = LabTestExtractionStatus.Failed;
                document.ExtractionError = ex.Message;
                document.ExtractedAt = DateTime.UtcNow;
                labTestDocumentRepository.Update(document);
                await unitOfWork.CompleteAsync();
            }
        }
    }

    private async Task<string> PreprocessImageAsync(string imagePath)
    {
        // Skip image preprocessing due to API compatibility issues
        // The key improvements are PSM 6 and Gemini AI parsing
        return await Task.FromResult(imagePath);
    }

    private async Task<List<ExtractedLabTestDto>> ParseExtractedTextWithGeminiAsync(string extractedText, int userId)
    {
        try
        {
            // Create a prompt for Gemini to parse the lab test text
            var prompt = $@"You are a medical lab test parser. Extract lab test results from the following text and return them as a JSON array.

Text to parse:
{extractedText}

Return format: JSON array with objects containing:
- testName: name of the lab test
- resultValue: the numeric or text result
- resultUnit: the unit of measurement (if present)
- normalRange: the reference range (if present)
- status: 'normal' or 'abnormal' based on whether result is within range

Example output format:
[
  {{
    ""testName"": ""Glucose"",
    ""resultValue"": ""95"",
    ""resultUnit"": ""mg/dL"",
    ""normalRange"": ""70-100"",
    ""status"": ""normal""
  }}
]

Return ONLY the JSON array, no other text.";

            // Create a temporary file with the text
            var tempTextFile = Path.GetTempFileName();
            await File.WriteAllTextAsync(tempTextFile, prompt);
            
            // Use Gemini to parse the text
            using var fileStream = new FileStream(tempTextFile, FileMode.Open, FileAccess.Read);
            var parsedTests = await _geminiService.ExtractLabTestsAsync(fileStream, "text/plain", "lab_test_prompt.txt", userId);
            
            // Cleanup
            File.Delete(tempTextFile);
            
            return parsedTests;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gemini parsing failed, falling back to regex parsing");
            // Fallback to the regex-based parsing
            return ParseExtractedTextToLabTests(extractedText);
        }
    }

    private async Task<string> ExtractTextWithTesseractAsync(string imagePath)
    {
        try
        {
            // Get tessdata path from configuration
            var tessDataPath = _configuration["Tesseract:TessDataPath"] ?? "./tessdata";
            var language = _configuration["Tesseract:Language"] ?? "eng";

            // Ensure the tessdata directory exists
            if (!Directory.Exists(tessDataPath))
            {
                throw new BusinessException(ErrorCodes.ConfigurationError, $"Tessdata directory not found at: {tessDataPath}");
            }

            // Check if the language data file exists
            var languageDataFile = Path.Combine(tessDataPath, $"{language}.traineddata");
            if (!File.Exists(languageDataFile))
            {
                throw new BusinessException(ErrorCodes.ConfigurationError, $"Tesseract language data file not found: {languageDataFile}");
            }

            // Check if the image file exists
            if (!File.Exists(imagePath))
            {
                throw new BusinessException(ErrorCodes.FileNotFound, $"Image file not found: {imagePath}");
            }

            // Validate that the file is an image (Tesseract only works with images)
            var extension = Path.GetExtension(imagePath).ToLowerInvariant();
            var validExtensions = new[] { ".png", ".jpg", ".jpeg", ".bmp", ".tiff", ".tif", ".gif" };
            if (!validExtensions.Contains(extension))
            {
                throw new BusinessException(ErrorCodes.ValidationError, $"Tesseract OCR only supports image files. Unsupported file type: {extension}");
            }

            using var engine = new TesseractEngine(tessDataPath, language, EngineMode.Default);
            
            // Set Page Segmentation Mode to 6 (Assume a single uniform block of text)
            // PSM 6 is better for lab reports with columns and tables
            engine.SetVariable("tessedit_pageseg_mode", "6");
            
            using var img = Pix.LoadFromFile(imagePath);
            using var page = engine.Process(img);
            
            // Get text with preserved layout
            var text = page.GetText();
            return text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting text with Tesseract from {ImagePath}", imagePath);
            throw;
        }
    }

    private List<ExtractedLabTestDto> ParseExtractedTextToLabTests(string extractedText)
    {
        // Enhanced parsing logic for lab test tabular data
        var labTests = new List<ExtractedLabTestDto>();
        
        if (string.IsNullOrWhiteSpace(extractedText))
            return labTests;

        var lines = extractedText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        
        // Try to detect column positions from the first few lines
        var columnPositions = DetectColumnPositions(lines.Take(5).ToList());
        
        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmedLine))
                continue;

            // Skip header lines
            if (IsHeaderLine(trimmedLine))
                continue;

            var labTest = ParseLabTestLine(trimmedLine, columnPositions);
            if (labTest != null)
            {
                labTests.Add(labTest);
            }
        }

        return labTests;
    }

    private List<int> DetectColumnPositions(List<string> sampleLines)
    {
        // Simple column detection based on common lab test patterns
        // This is a basic implementation - can be enhanced with more sophisticated detection
        var positions = new List<int>();
        
        // Look for patterns that suggest column breaks (multiple spaces or tabs)
        foreach (var line in sampleLines)
        {
            var spaces = new List<int>();
            for (int i = 0; i < line.Length - 1; i++)
            {
                if (line[i] == ' ' && line[i + 1] == ' ')
                {
                    spaces.Add(i);
                }
            }
            
            if (spaces.Count > 0)
            {
                positions.AddRange(spaces);
            }
        }
        
        // Return distinct positions sorted
        return positions.Distinct().OrderBy(p => p).ToList();
    }

    private bool IsHeaderLine(string line)
    {
        var lowerLine = line.ToLower();
        var headerKeywords = new[] { "test", "result", "unit", "reference", "range", "normal", "status", "flag", "lab", "report" };
        
        return headerKeywords.Any(keyword => lowerLine.Contains(keyword) && 
               line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length <= 6);
    }

    private ExtractedLabTestDto? ParseLabTestLine(string line, List<int> columnPositions)
    {
        // Try multiple parsing strategies
        
        // Strategy 1: Use column positions if detected
        if (columnPositions.Count >= 2)
        {
            var parsed = ParseByColumnPositions(line, columnPositions);
            if (parsed != null)
                return parsed;
        }
        
        // Strategy 2: Parse by tabs
        if (line.Contains('\t'))
        {
            var parsed = ParseByTabs(line);
            if (parsed != null)
                return parsed;
        }
        
        // Strategy 3: Parse by multiple spaces (common in lab reports)
        if (line.Contains("  "))
        {
            var parsed = ParseByMultipleSpaces(line);
            if (parsed != null)
                return parsed;
        }
        
        // Strategy 4: Try regex pattern for common lab test format
        return ParseByRegex(line);
    }

    private ExtractedLabTestDto? ParseByColumnPositions(string line, List<int> columnPositions)
    {
        try
        {
            var parts = new List<string>();
            int startPos = 0;
            
            foreach (var pos in columnPositions)
            {
                if (pos > startPos)
                {
                    parts.Add(line.Substring(startPos, pos - startPos).Trim());
                    startPos = pos;
                }
            }
            
            if (startPos < line.Length)
            {
                parts.Add(line.Substring(startPos).Trim());
            }
            
            if (parts.Count >= 2)
            {
                return CreateLabTestFromParts(parts);
            }
        }
        catch
        {
            // Fall through to other strategies
        }
        
        return null;
    }

    private ExtractedLabTestDto? ParseByTabs(string line)
    {
        var parts = line.Split('\t', StringSplitOptions.RemoveEmptyEntries).ToList();
        if (parts.Count >= 2)
        {
            return CreateLabTestFromParts(parts);
        }
        return null;
    }

    private ExtractedLabTestDto? ParseByMultipleSpaces(string line)
    {
        var parts = line.Split(new[] { "  " }, StringSplitOptions.RemoveEmptyEntries)
                         .Select(p => p.Trim())
                         .Where(p => !string.IsNullOrWhiteSpace(p))
                         .ToList();
        
        if (parts.Count >= 2)
        {
            return CreateLabTestFromParts(parts);
        }
        return null;
    }

    private ExtractedLabTestDto? ParseByRegex(string line)
    {
        // Try to match common lab test patterns
        // Pattern: TestName followed by numeric result and unit
        var patterns = new[]
        {
            @"^([A-Za-z][A-Za-z0-9\s\-]+)\s+(\d+\.?\d*)\s*([a-zA-Z/µ]+)?\s*([<>=]+\s*\d+\.?\d*\s*-\s*\d+\.?\d*)?",
            @"^([A-Za-z][A-Za-z0-9\s\-]+)\s+(\d+\.?\d*)\s*([a-zA-Z/µ]+)",
            @"^([A-Za-z][A-Za-z0-9\s\-]+)\s+(\d+\.?\d*)"
        };
        
        foreach (var pattern in patterns)
        {
            var match = System.Text.RegularExpressions.Regex.Match(line, pattern);
            if (match.Success)
            {
                return new ExtractedLabTestDto
                {
                    TestName = match.Groups[1].Value.Trim(),
                    ResultValue = match.Groups[2].Value.Trim(),
                    ResultUnit = match.Groups.Count > 2 ? match.Groups[3].Value.Trim() : null,
                    NormalRange = match.Groups.Count > 3 ? match.Groups[4].Value.Trim() : null,
                    Status = "normal"
                };
            }
        }
        
        return null;
    }

    private ExtractedLabTestDto CreateLabTestFromParts(List<string> parts)
    {
        var labTest = new ExtractedLabTestDto
        {
            TestName = parts[0],
            Status = "normal"
        };
        
        // Try to intelligently assign values based on content
        if (parts.Count >= 2)
        {
            // Second part is likely the result
            if (IsNumeric(parts[1]))
            {
                labTest.ResultValue = parts[1];
                
                // Third part might be unit
                if (parts.Count >= 3)
                {
                    labTest.ResultUnit = parts[2];
                    
                    // Fourth part might be reference range
                    if (parts.Count >= 4)
                    {
                        labTest.NormalRange = string.Join(" ", parts.Skip(3));
                    }
                }
            }
            else
            {
                // If second part is not numeric, it might be part of the test name
                labTest.TestName = $"{parts[0]} {parts[1]}";
                
                if (parts.Count >= 3 && IsNumeric(parts[2]))
                {
                    labTest.ResultValue = parts[2];
                    
                    if (parts.Count >= 4)
                    {
                        labTest.ResultUnit = parts[3];
                        
                        if (parts.Count >= 5)
                        {
                            labTest.NormalRange = string.Join(" ", parts.Skip(4));
                        }
                    }
                }
            }
        }
        
        // Detect abnormal status
        if (labTest.NormalRange != null && labTest.ResultValue != null)
        {
            labTest.Status = DetectStatus(labTest.ResultValue, labTest.NormalRange);
        }
        
        return labTest;
    }

    private bool IsNumeric(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
        
        // Remove common prefixes/suffixes
        var cleanValue = value.Trim().TrimStart('<', '>', '=', '±', '+', '-');
        return decimal.TryParse(cleanValue, out _);
    }

    private string DetectStatus(string result, string referenceRange)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(result) || string.IsNullOrWhiteSpace(referenceRange))
                return "normal";
            
            var cleanResult = result.Trim().TrimStart('<', '>', '=', '±', '+', '-');
            if (!decimal.TryParse(cleanResult, out var resultValue))
                return "normal";
            
            // Parse reference range (e.g., "3.5 - 5.5" or "< 10" or "> 20")
            var rangeParts = referenceRange.Split(new[] { '-', '–', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            
            if (rangeParts.Length >= 2)
            {
                if (decimal.TryParse(rangeParts[0], out var min) && decimal.TryParse(rangeParts[1], out var max))
                {
                    if (resultValue < min || resultValue > max)
                        return "abnormal";
                }
            }
            else if (referenceRange.StartsWith("<"))
            {
                if (decimal.TryParse(referenceRange.TrimStart('<', ' ', '='), out var max) && resultValue >= max)
                    return "abnormal";
            }
            else if (referenceRange.StartsWith(">"))
            {
                if (decimal.TryParse(referenceRange.TrimStart('>', ' ', '='), out var min) && resultValue <= min)
                    return "abnormal";
            }
        }
        catch
        {
            // If parsing fails, assume normal
        }
        
        return "normal";
    }

    private async Task ProcessExtractionAsync(int documentId)
    {
        using var scope = _scopeFactory.CreateScope();
        var labTestDocumentRepository = scope.ServiceProvider.GetRequiredService<ILabTestDocumentRepository>();
        var labTestRepository = scope.ServiceProvider.GetRequiredService<ILabTestRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var geminiService = scope.ServiceProvider.GetRequiredService<IGeminiService>();

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
            var extractedTests = await geminiService.ExtractLabTestsAsync(fileStream, document.ContentType, document.FileName, document.UserId);

            // Save extracted tests
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
                labTestRepository.Add(labTest);
            }

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
