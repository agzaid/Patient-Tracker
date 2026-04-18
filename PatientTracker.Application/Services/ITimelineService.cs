using PatientTracker.Application.DTOs;

namespace PatientTracker.Application.Services;

public interface ITimelineService
{
    Task<IEnumerable<TimelineItemDto>> GetTimelineAsync(int userId, string? typeFilter = null, string? dateRange = null);
    Task<PaginatedResponse<TimelineItemDto>> GetTimelinePaginatedAsync(int userId, int page = 1, int pageSize = 10, string? search = null, string? typeFilter = "all", string? dateRange = "all");
}
