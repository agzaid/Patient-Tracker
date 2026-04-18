using Microsoft.Extensions.Localization;
using PatientTracker.Application.DTOs;
using PatientTracker.Application.Interfaces;
using PatientTracker.Application.Resources;
using PatientTracker.Domain.Entities;
using PatientTracker.Domain.Enums;

namespace PatientTracker.Application.Services;

public class LabTestService : ILabTestService
{
    private readonly ILabTestRepository _labTestRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDocumentRepository _documentRepository;
    private readonly IStringLocalizer<ErrorMessages> _localizer;

    public LabTestService(ILabTestRepository labTestRepository, IUserRepository userRepository, IUnitOfWork unitOfWork, IDocumentRepository documentRepository, IStringLocalizer<ErrorMessages> localizer)
    {
        _labTestRepository = labTestRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _documentRepository = documentRepository;
        _localizer = localizer;
    }

    public async Task<IEnumerable<LabTestDto>> GetLabTestsAsync(int userId)
    {
        var labTests = await _labTestRepository.GetByUserIdAsync(userId);
        return labTests.Select(l => new LabTestDto
        {
            Id = l.Id,
            TestName = l.TestName,
            TestDate = l.TestDate,
            ResultValue = l.ResultValue,
            ResultUnit = l.ResultUnit,
            NormalRange = l.NormalRange,
            Status = l.Status,
            Notes = l.Notes,
            ReportUrl = l.ReportUrl,
            CreatedAt = l.CreatedAt,
            UpdatedAt = l.UpdatedAt
        });
    }

    public async Task<PaginatedResponse<LabTestDto>> GetLabTestsPaginatedAsync(int userId, int page = 1, int pageSize = 10, string? search = null)
    {
        // Ensure page and pageSize are valid
        page = Math.Max(1, page);
        pageSize = Math.Max(1, Math.Min(100, pageSize)); // Limit max page size to 100

        var totalCount = await _labTestRepository.CountByUserIdAsync(userId, search);
        var labTests = await _labTestRepository.GetByUserIdAsync(userId, page, pageSize, search);

        var labTestDtos = labTests.Select(l => new LabTestDto
        {
            Id = l.Id,
            TestName = l.TestName,
            TestDate = l.TestDate,
            ResultValue = l.ResultValue,
            ResultUnit = l.ResultUnit,
            NormalRange = l.NormalRange,
            Status = l.Status,
            Notes = l.Notes,
            ReportUrl = l.ReportUrl,
            CreatedAt = l.CreatedAt,
            UpdatedAt = l.UpdatedAt
        });

        return new PaginatedResponse<LabTestDto>
        {
            Items = labTestDtos,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<LabTestDto?> GetLabTestAsync(int id, int userId)
    {
        var labTest = await _labTestRepository.GetByIdAsync(id);
        if (labTest == null || labTest.UserId != userId)
        {
            return null;
        }

        return new LabTestDto
        {
            Id = labTest.Id,
            TestName = labTest.TestName,
            TestDate = labTest.TestDate,
            ResultValue = labTest.ResultValue,
            ResultUnit = labTest.ResultUnit,
            NormalRange = labTest.NormalRange,
            Status = labTest.Status,
            Notes = labTest.Notes,
            ReportUrl = labTest.ReportUrl,
            CreatedAt = labTest.CreatedAt,
            UpdatedAt = labTest.UpdatedAt
        };
    }

    public async Task<LabTestDto> CreateLabTestAsync(int userId, CreateLabTestRequest request)
    {
        // Verify user exists
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new InvalidOperationException(_localizer["UserNotFound"]);
        }

        var labTest = new LabTest
        {
            UserId = userId,
            TestName = request.TestName,
            TestDate = request.TestDate,
            ResultValue = request.ResultValue,
            ResultUnit = request.ResultUnit,
            NormalRange = request.NormalRange,
            Status = request.Status,
            Notes = request.Notes,
            ReportUrl = request.ReportUrl,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _labTestRepository.Add(labTest);
        await _unitOfWork.CompleteAsync();

        // If there are any temporary documents for this user with ParentEntityType = LabTest and ParentEntityId = null,
        // update them to link to this lab test
        if (request.DocumentIds != null && request.DocumentIds.Any())
        {
            var documents = await _documentRepository.GetByIdsAsync(request.DocumentIds);
            foreach (var document in documents)
            {
                if (document.UserId == userId && document.ParentEntityType == ParentEntityType.LabTest && document.ParentEntityId == null)
                {
                    document.ParentEntityId = labTest.Id;
                    document.UpdatedAt = DateTime.UtcNow;
                    _documentRepository.Update(document);
                }
            }
            await _unitOfWork.CompleteAsync();
        }

        return new LabTestDto
        {
            Id = labTest.Id,
            TestName = labTest.TestName,
            TestDate = labTest.TestDate,
            ResultValue = labTest.ResultValue,
            ResultUnit = labTest.ResultUnit,
            NormalRange = labTest.NormalRange,
            Status = labTest.Status,
            Notes = labTest.Notes,
            ReportUrl = labTest.ReportUrl,
            CreatedAt = labTest.CreatedAt,
            UpdatedAt = labTest.UpdatedAt
        };
    }

    public async Task<LabTestDto> UpdateLabTestAsync(int id, int userId, UpdateLabTestRequest request)
    {
        var labTest = await _labTestRepository.GetByIdAsync(id);
        if (labTest == null || labTest.UserId != userId)
        {
            throw new InvalidOperationException(_localizer["LabTestNotFound"]);
        }

        labTest.TestName = request.TestName;
        labTest.TestDate = request.TestDate;
        labTest.ResultValue = request.ResultValue;
        labTest.ResultUnit = request.ResultUnit;
        labTest.NormalRange = request.NormalRange;
        labTest.Status = request.Status;
        labTest.Notes = request.Notes;
        labTest.ReportUrl = request.ReportUrl;
        labTest.UpdatedAt = DateTime.UtcNow;

        _labTestRepository.Update(labTest);
        await _unitOfWork.CompleteAsync();

        return new LabTestDto
        {
            Id = labTest.Id,
            TestName = labTest.TestName,
            TestDate = labTest.TestDate,
            ResultValue = labTest.ResultValue,
            ResultUnit = labTest.ResultUnit,
            NormalRange = labTest.NormalRange,
            Status = labTest.Status,
            Notes = labTest.Notes,
            ReportUrl = labTest.ReportUrl,
            CreatedAt = labTest.CreatedAt,
            UpdatedAt = labTest.UpdatedAt
        };
    }

    public async Task<bool> DeleteLabTestAsync(int id, int userId)
    {
        var labTest = await _labTestRepository.GetByIdAsync(id);
        if (labTest == null || labTest.UserId != userId)
        {
            return false;
        }

        _labTestRepository.Delete(labTest);
        await _unitOfWork.CompleteAsync();
        return true;
    }
}
