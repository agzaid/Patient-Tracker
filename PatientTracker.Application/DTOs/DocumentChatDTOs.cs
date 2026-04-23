using System.ComponentModel.DataAnnotations;

namespace PatientTracker.Application.DTOs;

public class DocumentChatRequest
{
    [Required]
    public int DocumentId { get; set; }
    
    [Required]
    [MaxLength(1000)]
    public string Message { get; set; } = string.Empty;
    
    public bool IncludeHistory { get; set; } = true;
}

public class DocumentChatResponse
{
    public string Answer { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public List<DocumentChatMessageDto> History { get; set; } = new();
}

public class DocumentChatMessageDto
{
    public int Id { get; set; }
    public int DocumentId { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Response { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public bool IsUserMessage { get; set; }
}

public class GetChatHistoryRequest
{
    [Required]
    public int DocumentId { get; set; }
    
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class GetChatHistoryParameters
{
    private int _page = 1;
    private int _pageSize = 50;

    [Required]
    public int DocumentId { get; set; }

    public int Page 
    { 
        get => _page; 
        set => _page = Math.Max(1, value); 
    }

    public int PageSize 
    { 
        get => _pageSize; 
        set => _pageSize = Math.Max(1, Math.Min(100, value)); 
    }
}

public class PaginatedChatResponse
{
    public List<DocumentChatMessageDto> Messages { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public bool HasNext { get; set; }
    public bool HasPrevious { get; set; }
}
