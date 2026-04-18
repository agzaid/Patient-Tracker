using PatientTracker.Application.DTOs;

namespace PatientTracker.Application.Services;

public class TimelineService : ITimelineService
{
    private readonly IMedicationService _medicationService;
    private readonly ILabTestService _labTestService;
    private readonly IRadiologyService _radiologyService;
    private readonly IDiagnosisService _diagnosisService;
    private readonly ISurgeryService _surgeryService;

    public TimelineService(
        IMedicationService medicationService,
        ILabTestService labTestService,
        IRadiologyService radiologyService,
        IDiagnosisService diagnosisService,
        ISurgeryService surgeryService)
    {
        _medicationService = medicationService;
        _labTestService = labTestService;
        _radiologyService = radiologyService;
        _diagnosisService = diagnosisService;
        _surgeryService = surgeryService;
    }

    public async Task<IEnumerable<TimelineItemDto>> GetTimelineAsync(int userId, string? typeFilter = null, string? dateRange = null)
    {
        var items = new List<TimelineItemDto>();

        // Get all items
        var medications = await _medicationService.GetMedicationsAsync(userId);
        var labTests = await _labTestService.GetLabTestsAsync(userId);
        var radiologyScans = await _radiologyService.GetRadiologyScansAsync(userId);
        var diagnoses = await _diagnosisService.GetDiagnosesAsync(userId);
        var surgeries = await _surgeryService.GetSurgeriesAsync(userId);

        // Convert medications to timeline items
        foreach (var med in medications)
        {
            items.Add(new TimelineItemDto
            {
                Id = med.Id,
                Type = "medication",
                Title = med.Name,
                Subtitle = $"{med.Dosage ?? ""} {med.Frequency ?? ""}".Trim(),
                Date = med.StartDate ?? med.CreatedAt,
                Details = med.Notes,
                Status = med.IsCurrent ? "Active" : "Past"
            });
        }

        // Convert lab tests to timeline items
        foreach (var test in labTests)
        {
            items.Add(new TimelineItemDto
            {
                Id = test.Id,
                Type = "lab_test",
                Title = test.TestName,
                Subtitle = test.ResultValue != null ? $"{test.ResultValue} {test.ResultUnit ?? ""}" : "Pending",
                Date = test.TestDate,
                Details = test.Notes,
                Status = test.Status
            });
        }

        // Convert radiology scans to timeline items
        foreach (var scan in radiologyScans)
        {
            items.Add(new TimelineItemDto
            {
                Id = scan.Id,
                Type = "radiology",
                Title = $"{scan.ScanType}{(string.IsNullOrEmpty(scan.BodyPart) ? "" : $" - {scan.BodyPart}")}",
                Subtitle = scan.Description ?? "No description",
                Date = scan.ScanDate,
                Details = scan.DoctorNotes
            });
        }

        // Convert diagnoses to timeline items
        foreach (var diagnosis in diagnoses)
        {
            items.Add(new TimelineItemDto
            {
                Id = diagnosis.Id,
                Type = "diagnosis",
                Title = diagnosis.DiagnosisName,
                Subtitle = diagnosis.DoctorName != null ? $"Dr. {diagnosis.DoctorName}" : "No doctor specified",
                Date = diagnosis.DateDiagnosed ?? diagnosis.CreatedAt,
                Details = diagnosis.Notes,
                Status = diagnosis.Status
            });
        }

        // Convert surgeries to timeline items
        foreach (var surgery in surgeries)
        {
            items.Add(new TimelineItemDto
            {
                Id = surgery.Id,
                Type = "surgery",
                Title = surgery.SurgeryName,
                Subtitle = surgery.HospitalName ?? "No hospital specified",
                Date = surgery.SurgeryDate,
                Details = surgery.Notes
            });
        }

        // Apply filters
        var filteredItems = items.AsEnumerable();

        if (!string.IsNullOrEmpty(typeFilter) && typeFilter != "all")
        {
            filteredItems = filteredItems.Where(i => i.Type == typeFilter);
        }

        if (!string.IsNullOrEmpty(dateRange) && dateRange != "all")
        {
            var cutoff = dateRange switch
            {
                "30d" => DateTime.UtcNow.AddDays(-30),
                "6m" => DateTime.UtcNow.AddMonths(-6),
                "1y" => DateTime.UtcNow.AddYears(-1),
                _ => (DateTime?)null
            };

            if (cutoff.HasValue)
            {
                filteredItems = filteredItems.Where(i => i.Date >= cutoff.Value);
            }
        }

        // Sort by date (newest first)
        return filteredItems.OrderByDescending(i => i.Date);
    }

    public async Task<PaginatedResponse<TimelineItemDto>> GetTimelinePaginatedAsync(int userId, int page = 1, int pageSize = 10, string? search = null, string? typeFilter = "all", string? dateRange = "all")
    {
        // Ensure page and pageSize are valid
        page = Math.Max(1, page);
        pageSize = Math.Max(1, Math.Min(100, pageSize)); // Limit max page size to 100

        var items = new List<TimelineItemDto>();

        // Get all items
        var medications = await _medicationService.GetMedicationsAsync(userId);
        var labTests = await _labTestService.GetLabTestsAsync(userId);
        var radiologyScans = await _radiologyService.GetRadiologyScansAsync(userId);
        var diagnoses = await _diagnosisService.GetDiagnosesAsync(userId);
        var surgeries = await _surgeryService.GetSurgeriesAsync(userId);

        // Convert medications to timeline items
        foreach (var med in medications)
        {
            items.Add(new TimelineItemDto
            {
                Id = med.Id,
                Type = "medication",
                Title = med.Name,
                Subtitle = $"{med.Dosage ?? ""} {med.Frequency ?? ""}".Trim(),
                Date = med.StartDate ?? med.CreatedAt,
                Details = med.Notes,
                Status = med.IsCurrent ? "Active" : "Past"
            });
        }

        // Convert lab tests to timeline items
        foreach (var test in labTests)
        {
            items.Add(new TimelineItemDto
            {
                Id = test.Id,
                Type = "lab_test",
                Title = test.TestName,
                Subtitle = test.ResultValue != null ? $"{test.ResultValue} {test.ResultUnit ?? ""}" : "Pending",
                Date = test.TestDate,
                Details = test.Notes,
                Status = test.Status
            });
        }

        // Convert radiology scans to timeline items
        foreach (var scan in radiologyScans)
        {
            items.Add(new TimelineItemDto
            {
                Id = scan.Id,
                Type = "radiology",
                Title = $"{scan.ScanType}{(string.IsNullOrEmpty(scan.BodyPart) ? "" : $" - {scan.BodyPart}")}",
                Subtitle = scan.Description ?? "No description",
                Date = scan.ScanDate,
                Details = scan.DoctorNotes
            });
        }

        // Convert diagnoses to timeline items
        foreach (var diagnosis in diagnoses)
        {
            items.Add(new TimelineItemDto
            {
                Id = diagnosis.Id,
                Type = "diagnosis",
                Title = diagnosis.DiagnosisName,
                Subtitle = diagnosis.DoctorName != null ? $"Dr. {diagnosis.DoctorName}" : "No doctor specified",
                Date = diagnosis.DateDiagnosed ?? diagnosis.CreatedAt,
                Details = diagnosis.Notes,
                Status = diagnosis.Status
            });
        }

        // Convert surgeries to timeline items
        foreach (var surgery in surgeries)
        {
            items.Add(new TimelineItemDto
            {
                Id = surgery.Id,
                Type = "surgery",
                Title = surgery.SurgeryName,
                Subtitle = surgery.HospitalName ?? "No hospital specified",
                Date = surgery.SurgeryDate,
                Details = surgery.Notes
            });
        }

        // Apply filters
        var filteredItems = items.AsEnumerable();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(search))
        {
            filteredItems = filteredItems.Where(i => 
                i.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (i.Subtitle != null && i.Subtitle.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                (i.Details != null && i.Details.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                (i.Status != null && i.Status.Contains(search, StringComparison.OrdinalIgnoreCase)));
        }

        // Apply type filter
        if (!string.IsNullOrEmpty(typeFilter) && typeFilter != "all")
        {
            filteredItems = filteredItems.Where(i => i.Type == typeFilter);
        }

        // Apply date range filter
        if (!string.IsNullOrEmpty(dateRange) && dateRange != "all")
        {
            var cutoff = dateRange switch
            {
                "30d" => DateTime.UtcNow.AddDays(-30),
                "6m" => DateTime.UtcNow.AddMonths(-6),
                "1y" => DateTime.UtcNow.AddYears(-1),
                _ => (DateTime?)null
            };

            if (cutoff.HasValue)
            {
                filteredItems = filteredItems.Where(i => i.Date >= cutoff.Value);
            }
        }

        // Sort by date (newest first)
        var sortedItems = filteredItems.OrderByDescending(i => i.Date);
        var totalCount = sortedItems.Count();

        // Apply pagination
        var paginatedItems = sortedItems
            .Skip((page - 1) * pageSize)
            .Take(pageSize);

        return new PaginatedResponse<TimelineItemDto>
        {
            Items = paginatedItems,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}
