using PatientTracker.Application.DTOs;
using PatientTracker.Application.Interfaces;
using PatientTracker.Domain.Entities;

namespace PatientTracker.Application.Services;

public class SurgeryService : ISurgeryService
{
    private readonly ISurgeryRepository _surgeryRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SurgeryService(ISurgeryRepository surgeryRepository, IUserRepository userRepository, IUnitOfWork unitOfWork)
    {
        _surgeryRepository = surgeryRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<SurgeryDto>> GetSurgeriesAsync(int userId)
    {
        var surgeries = await _surgeryRepository.GetByUserIdAsync(userId);
        return surgeries.Select(s => new SurgeryDto
        {
            Id = s.Id,
            SurgeryName = s.SurgeryName,
            SurgeryDate = s.SurgeryDate,
            HospitalName = s.HospitalName,
            SurgeonName = s.SurgeonName,
            Description = s.Description,
            Notes = s.Notes,
            ReportUrl = s.ReportUrl,
            CreatedAt = s.CreatedAt,
            UpdatedAt = s.UpdatedAt
        });
    }

    public async Task<SurgeryDto?> GetSurgeryAsync(int id, int userId)
    {
        var surgery = await _surgeryRepository.GetByIdAsync(id);
        if (surgery == null || surgery.UserId != userId)
        {
            return null;
        }

        return new SurgeryDto
        {
            Id = surgery.Id,
            SurgeryName = surgery.SurgeryName,
            SurgeryDate = surgery.SurgeryDate,
            HospitalName = surgery.HospitalName,
            SurgeonName = surgery.SurgeonName,
            Description = surgery.Description,
            Notes = surgery.Notes,
            ReportUrl = surgery.ReportUrl,
            CreatedAt = surgery.CreatedAt,
            UpdatedAt = surgery.UpdatedAt
        };
    }

    public async Task<SurgeryDto> CreateSurgeryAsync(int userId, CreateSurgeryRequest request)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new InvalidOperationException("User not found");
        }

        var surgery = new Surgery
        {
            UserId = userId,
            SurgeryName = request.SurgeryName,
            SurgeryDate = request.SurgeryDate,
            HospitalName = request.HospitalName,
            SurgeonName = request.SurgeonName,
            Description = request.Description,
            Notes = request.Notes,
            ReportUrl = request.ReportUrl,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _surgeryRepository.Add(surgery);
        await _unitOfWork.CompleteAsync();

        return new SurgeryDto
        {
            Id = surgery.Id,
            SurgeryName = surgery.SurgeryName,
            SurgeryDate = surgery.SurgeryDate,
            HospitalName = surgery.HospitalName,
            SurgeonName = surgery.SurgeonName,
            Description = surgery.Description,
            Notes = surgery.Notes,
            ReportUrl = surgery.ReportUrl,
            CreatedAt = surgery.CreatedAt,
            UpdatedAt = surgery.UpdatedAt
        };
    }

    public async Task<SurgeryDto> UpdateSurgeryAsync(int id, int userId, UpdateSurgeryRequest request)
    {
        var surgery = await _surgeryRepository.GetByIdAsync(id);
        if (surgery == null || surgery.UserId != userId)
        {
            throw new InvalidOperationException("Surgery not found or access denied");
        }

        surgery.SurgeryName = request.SurgeryName;
        surgery.SurgeryDate = request.SurgeryDate;
        surgery.HospitalName = request.HospitalName;
        surgery.SurgeonName = request.SurgeonName;
        surgery.Description = request.Description;
        surgery.Notes = request.Notes;
        surgery.ReportUrl = request.ReportUrl;
        surgery.UpdatedAt = DateTime.UtcNow;

        _surgeryRepository.Update(surgery);
        await _unitOfWork.CompleteAsync();

        return new SurgeryDto
        {
            Id = surgery.Id,
            SurgeryName = surgery.SurgeryName,
            SurgeryDate = surgery.SurgeryDate,
            HospitalName = surgery.HospitalName,
            SurgeonName = surgery.SurgeonName,
            Description = surgery.Description,
            Notes = surgery.Notes,
            ReportUrl = surgery.ReportUrl,
            CreatedAt = surgery.CreatedAt,
            UpdatedAt = surgery.UpdatedAt
        };
    }

    public async Task<bool> DeleteSurgeryAsync(int id, int userId)
    {
        var surgery = await _surgeryRepository.GetByIdAsync(id);
        if (surgery == null || surgery.UserId != userId)
        {
            return false;
        }

        _surgeryRepository.Delete(surgery);
        await _unitOfWork.CompleteAsync();
        return true;
    }
}
