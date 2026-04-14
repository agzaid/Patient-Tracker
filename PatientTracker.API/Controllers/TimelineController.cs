using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatientTracker.Application.DTOs;
using PatientTracker.Application.Services;
using System.Security.Claims;

namespace PatientTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TimelineController : ControllerBase
{
    private readonly ITimelineService _timelineService;

    public TimelineController(ITimelineService timelineService)
    {
        _timelineService = timelineService;
    }

    /// <summary>
    /// Get timeline for the authenticated user
    /// </summary>
    /// <param name="typeFilter">Filter by type (all, medication, lab_test, radiology, diagnosis, surgery)</param>
    /// <param name="dateRange">Filter by date range (all, 30d, 6m, 1y)</param>
    /// <returns>Timeline items</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TimelineItemDto>>> GetTimeline([FromQuery] string? typeFilter = "all", [FromQuery] string? dateRange = "all")
    {
        try
        {
            var userId = GetUserId();
            var timeline = await _timelineService.GetTimelineAsync(userId, typeFilter, dateRange);
            return Ok(timeline);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while fetching timeline" });
        }
    }

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user identifier");
        }
        return userId;
    }
}
