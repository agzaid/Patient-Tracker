using Microsoft.Extensions.Logging;
using PatientTracker.Application.Interfaces;
using PatientTracker.Domain.Entities;

namespace PatientTracker.Application.Services;

public class RateLimitingService : IRateLimitingService
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RateLimitingService> _logger;
    
    private const int MaxRequestsPerDay = 5;
    private const int MaxRequestsPerMinute = 2;

    public RateLimitingService(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ILogger<RateLimitingService> logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> CanMakeGeminiRequestAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return false;
        }

        var now = DateTime.UtcNow;

        // Check if it's a new day (reset daily counter)
        if (user.LastGeminiRequestTime.HasValue && user.LastGeminiRequestTime.Value.Date != now.Date)
        {
            user.GeminiRequestsToday = 0;
            _userRepository.Update(user);
            await _unitOfWork.CompleteAsync();
        }

        // Check if it's been more than a minute (reset minute counter)
        if (user.LastGeminiRequestTime.HasValue && (now - user.LastGeminiRequestTime.Value).TotalMinutes >= 1)
        {
            user.GeminiRequestsLastMinute = 0;
        }

        // Check daily limit
        if (user.GeminiRequestsToday >= MaxRequestsPerDay)
        {
            _logger.LogWarning("User {UserId} has exceeded daily Gemini request limit of {MaxRequests}", userId, MaxRequestsPerDay);
            return false;
        }

        // Check minute limit
        if (user.GeminiRequestsLastMinute >= MaxRequestsPerMinute)
        {
            _logger.LogWarning("User {UserId} has exceeded per-minute Gemini request limit of {MaxRequests}", userId, MaxRequestsPerMinute);
            return false;
        }

        return true;
    }

    public async Task RecordGeminiRequestAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return;
        }

        var now = DateTime.UtcNow;

        // Check if it's a new day (reset daily counter)
        if (user.LastGeminiRequestTime.HasValue && user.LastGeminiRequestTime.Value.Date != now.Date)
        {
            user.GeminiRequestsToday = 0;
        }

        // Check if it's been more than a minute (reset minute counter)
        if (user.LastGeminiRequestTime.HasValue && (now - user.LastGeminiRequestTime.Value).TotalMinutes >= 1)
        {
            user.GeminiRequestsLastMinute = 0;
        }

        // Increment counters
        user.GeminiRequestsToday++;
        user.GeminiRequestsLastMinute++;
        user.AllGeminiRequests++;
        user.LastGeminiRequestTime = now;
        user.UpdatedAt = now;

        _userRepository.Update(user);
        await _unitOfWork.CompleteAsync();

        _logger.LogInformation("Recorded Gemini request for user {UserId}. Daily: {DailyCount}/{MaxDaily}, Minute: {MinuteCount}/{MaxMinute}, Total: {TotalCount}",
            userId, user.GeminiRequestsToday, MaxRequestsPerDay, user.GeminiRequestsLastMinute, MaxRequestsPerMinute, user.AllGeminiRequests);
    }

    public async Task ResetDailyCountersAsync()
    {
        var users = await _userRepository.GetAllAsync();
        var now = DateTime.UtcNow;

        foreach (var user in users)
        {
            if (user.LastGeminiRequestTime.HasValue && user.LastGeminiRequestTime.Value.Date != now.Date)
            {
                user.GeminiRequestsToday = 0;
                user.UpdatedAt = now;
                _userRepository.Update(user);
            }
        }

        await _unitOfWork.CompleteAsync();
        _logger.LogInformation("Reset daily Gemini request counters for users");
    }
}
