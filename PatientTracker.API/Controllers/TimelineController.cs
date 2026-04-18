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
    /// Get timeline for the authenticated user (paginated)
    /// </summary>
    /// <param name="parameters">Query parameters for pagination, search, and filters</param>
    /// <returns>Paginated list of timeline items</returns>
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<TimelineItemDto>>> GetTimeline([FromQuery] TimelineQueryParameters parameters)
    {
        try
        {
            var userId = GetUserId();
            var paginatedTimeline = await _timelineService.GetTimelinePaginatedAsync(userId, parameters.Page, parameters.PageSize, parameters.Search, parameters.TypeFilter, parameters.DateRange);
            return Ok(paginatedTimeline);
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
