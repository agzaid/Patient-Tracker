using PatientTracker.Application.DTOs;

namespace PatientTracker.Application.Services;

public interface ITimelineService
{
    Task<IEnumerable<TimelineItemDto>> GetTimelineAsync(int userId, string? typeFilter = null, string? dateRange = null);
}
