namespace PatientTracker.Application.Services;

public interface IRateLimitingService
{
    Task<bool> CanMakeGeminiRequestAsync(int userId);
    Task RecordGeminiRequestAsync(int userId);
    Task ResetDailyCountersAsync();
}
