using PatientTracker.Application.DTOs;
using PatientTracker.Application.Interfaces;
using PatientTracker.Domain.Entities;

namespace PatientTracker.Application.Services;

public class LabTestService : ILabTestService
{
    private readonly ILabTestRepository _labTestRepository;
    private readonly IUserRepository _userRepository;

    public LabTestService(ILabTestRepository labTestRepository, IUserRepository userRepository)
    {
        _labTestRepository = labTestRepository;
        _userRepository = userRepository;
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
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new InvalidOperationException("User not found");
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

        var createdLabTest = await _labTestRepository.CreateAsync(labTest);

        return new LabTestDto
        {
            Id = createdLabTest.Id,
            TestName = createdLabTest.TestName,
            TestDate = createdLabTest.TestDate,
            ResultValue = createdLabTest.ResultValue,
            ResultUnit = createdLabTest.ResultUnit,
            NormalRange = createdLabTest.NormalRange,
            Status = createdLabTest.Status,
            Notes = createdLabTest.Notes,
            ReportUrl = createdLabTest.ReportUrl,
            CreatedAt = createdLabTest.CreatedAt,
            UpdatedAt = createdLabTest.UpdatedAt
        };
    }

    public async Task<LabTestDto> UpdateLabTestAsync(int id, int userId, UpdateLabTestRequest request)
    {
        var labTest = await _labTestRepository.GetByIdAsync(id);
        if (labTest == null || labTest.UserId != userId)
        {
            throw new InvalidOperationException("Lab test not found or access denied");
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

        var updatedLabTest = await _labTestRepository.UpdateAsync(labTest);

        return new LabTestDto
        {
            Id = updatedLabTest.Id,
            TestName = updatedLabTest.TestName,
            TestDate = updatedLabTest.TestDate,
            ResultValue = updatedLabTest.ResultValue,
            ResultUnit = updatedLabTest.ResultUnit,
            NormalRange = updatedLabTest.NormalRange,
            Status = updatedLabTest.Status,
            Notes = updatedLabTest.Notes,
            ReportUrl = updatedLabTest.ReportUrl,
            CreatedAt = updatedLabTest.CreatedAt,
            UpdatedAt = updatedLabTest.UpdatedAt
        };
    }

    public async Task<bool> DeleteLabTestAsync(int id, int userId)
    {
        var labTest = await _labTestRepository.GetByIdAsync(id);
        if (labTest == null || labTest.UserId != userId)
        {
            return false;
        }

        return await _labTestRepository.DeleteAsync(id);
    }
}
