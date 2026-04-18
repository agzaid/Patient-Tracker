using System.ComponentModel.DataAnnotations;

namespace PatientTracker.Application.DTOs;

public class TimelineQueryParameters : QueryParameters
{
    public string? TypeFilter { get; set; } = "all";
    public string? DateRange { get; set; } = "all";
}
