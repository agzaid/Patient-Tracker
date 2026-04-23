using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PatientTracker.Application.DTOs;
using PatientTracker.Application.Interfaces;
using PatientTracker.Domain.Entities;

namespace PatientTracker.Application.Services;

public interface IDocumentChatService
{
    Task<DocumentChatResponse> AskAboutDocumentAsync(int userId, DocumentChatRequest request);
    Task<PaginatedChatResponse> GetChatHistoryAsync(int userId, GetChatHistoryRequest request);
    Task<bool> DeleteChatHistoryAsync(int userId, int documentId);
}

public class DocumentChatService : IDocumentChatService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IGeminiService _geminiService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DocumentChatService> _logger;

    public DocumentChatService(
        IServiceScopeFactory scopeFactory,
        IGeminiService geminiService,
        IConfiguration configuration,
        ILogger<DocumentChatService> logger)
    {
        _scopeFactory = scopeFactory;
        _geminiService = geminiService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<DocumentChatResponse> AskAboutDocumentAsync(int userId, DocumentChatRequest request)
    {
        using var scope = _scopeFactory.CreateScope();
        var documentRepository = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();
        var chatRepository = scope.ServiceProvider.GetRequiredService<IDocumentChatMessageRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        try
        {
            // Verify document exists and user has access
            var document = await documentRepository.GetByIdAsync(request.DocumentId);
            if (document == null || document.UserId != userId)
            {
                throw new InvalidOperationException("Document not found or access denied");
            }

            // Get chat history if requested
            var history = new List<DocumentChatMessage>();
            if (request.IncludeHistory)
            {
                history = (await chatRepository.GetByDocumentIdAsync(request.DocumentId, userId))
                    .Take(10) // Limit to last 10 messages to avoid context overflow
                    .ToList();
            }

            // Read document content if it's an image or PDF
            string? base64Content = null;
            if (document.ContentType.StartsWith("image/") || document.ContentType == "application/pdf")
            {
                base64Content = await ReadDocumentAsBase64Async(document.FilePath);
            }

            // Build prompt with history
            var prompt = BuildChatPrompt(history, request.Message);

            // Get AI response
            var aiResponse = await _geminiService.GenerateResponseAsync(prompt, base64Content, document.ContentType);

            // Save chat message
            var chatMessage = new DocumentChatMessage
            {
                DocumentId = request.DocumentId,
                UserId = userId,
                UserMessage = request.Message,
                AiResponse = aiResponse
            };

            chatRepository.Add(chatMessage);
            await unitOfWork.CompleteAsync();

            // Build response
            var response = new DocumentChatResponse
            {
                Answer = aiResponse,
                Timestamp = DateTime.UtcNow,
                History = history.Select(m => new DocumentChatMessageDto
                {
                    Id = m.Id,
                    DocumentId = m.DocumentId,
                    Message = m.UserMessage,
                    Response = m.AiResponse,
                    Timestamp = m.CreatedAt,
                    IsUserMessage = true
                }).ToList()
            };

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat request for document {DocumentId}", request.DocumentId);
            throw;
        }
    }

    public async Task<PaginatedChatResponse> GetChatHistoryAsync(int userId, GetChatHistoryRequest request)
    {
        using var scope = _scopeFactory.CreateScope();
        var chatRepository = scope.ServiceProvider.GetRequiredService<IDocumentChatMessageRepository>();

        try
        {
            // Verify document access
            var documentRepository = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();
            var document = await documentRepository.GetByIdAsync(request.DocumentId);
            if (document == null || document.UserId != userId)
            {
                throw new InvalidOperationException("Document not found or access denied");
            }

            // Get paginated chat history
            var totalCount = await chatRepository.CountByDocumentIdAsync(request.DocumentId, userId);
            var messages = await chatRepository.GetByDocumentIdAsync(
                request.DocumentId, 
                userId, 
                request.Page, 
                request.PageSize);

            return new PaginatedChatResponse
            {
                Messages = messages.Select(m => new DocumentChatMessageDto
                {
                    Id = m.Id,
                    DocumentId = m.DocumentId,
                    Message = m.UserMessage,
                    Response = m.AiResponse,
                    Timestamp = m.CreatedAt,
                    IsUserMessage = true
                }).ToList(),
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize,
                HasNext = request.Page * request.PageSize < totalCount,
                HasPrevious = request.Page > 1
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching chat history for document {DocumentId}", request.DocumentId);
            throw;
        }
    }

    public async Task<bool> DeleteChatHistoryAsync(int userId, int documentId)
    {
        using var scope = _scopeFactory.CreateScope();
        var chatRepository = scope.ServiceProvider.GetRequiredService<IDocumentChatMessageRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        try
        {
            // Verify document access
            var documentRepository = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();
            var document = await documentRepository.GetByIdAsync(documentId);
            if (document == null || document.UserId != userId)
            {
                return false;
            }

            // Delete chat history
            var messages = await chatRepository.GetByDocumentIdAsync(documentId, userId);
            foreach (var message in messages)
            {
                chatRepository.Delete(message);
            }

            await unitOfWork.CompleteAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting chat history for document {DocumentId}", documentId);
            throw;
        }
    }

    private async Task<string> ReadDocumentAsBase64Async(string filePath)
    {
        try
        {
            var bytes = await File.ReadAllBytesAsync(filePath);
            return Convert.ToBase64String(bytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading document from {FilePath}", filePath);
            throw new InvalidOperationException("Failed to read document");
        }
    }

    private string BuildChatPrompt(List<DocumentChatMessage> history, string currentQuestion)
    {
        var prompt = @"You are a medical AI assistant helping a patient understand their medical documents. ";

        if (history.Any())
        {
            prompt += "\nPREVIOUS CONVERSATION:\n";
            foreach (var msg in history.TakeLast(5)) // Only include last 5 messages
            {
                prompt += $"\nPatient: {msg.UserMessage}\nAssistant: {msg.AiResponse}\n";
            }
        }

        prompt += $"\nCURRENT QUESTION: {currentQuestion}\n\n";
        prompt += @"Please provide a clear, helpful answer based on the document content. 
If the information is not in the document, say so clearly.
Always remind the patient to consult with their healthcare provider for medical advice.";

        return prompt;
    }
}
