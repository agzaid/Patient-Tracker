using PatientTracker.Application.DTOs;
using PatientTracker.Application.Interfaces;
using PatientTracker.Domain.Entities;

namespace PatientTracker.Application.Services;

public class DiagnosisService : IDiagnosisService
{
    private readonly IDiagnosisRepository _diagnosisRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DiagnosisService(IDiagnosisRepository diagnosisRepository, IUserRepository userRepository, IUnitOfWork unitOfWork)
    {
        _diagnosisRepository = diagnosisRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<DiagnosisDto>> GetDiagnosesAsync(int userId)
    {
        var diagnoses = await _diagnosisRepository.GetByUserIdAsync(userId);
        return diagnoses.Select(d => new DiagnosisDto
        {
            Id = d.Id,
            DiagnosisName = d.DiagnosisName,
            DateDiagnosed = d.DateDiagnosed,
            DoctorName = d.DoctorName,
            Severity = d.Severity,
            Status = d.Status,
            Notes = d.Notes,
            CreatedAt = d.CreatedAt,
            UpdatedAt = d.UpdatedAt
        });
    }

    public async Task<DiagnosisDto?> GetDiagnosisAsync(int id, int userId)
    {
        var diagnosis = await _diagnosisRepository.GetByIdAsync(id);
        if (diagnosis == null || diagnosis.UserId != userId)
        {
            return null;
        }

        return new DiagnosisDto
        {
            Id = diagnosis.Id,
            DiagnosisName = diagnosis.DiagnosisName,
            DateDiagnosed = diagnosis.DateDiagnosed,
            DoctorName = diagnosis.DoctorName,
            Severity = diagnosis.Severity,
            Status = diagnosis.Status,
            Notes = diagnosis.Notes,
            CreatedAt = diagnosis.CreatedAt,
            UpdatedAt = diagnosis.UpdatedAt
        };
    }

    public async Task<DiagnosisDto> CreateDiagnosisAsync(int userId, CreateDiagnosisRequest request)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new InvalidOperationException("User not found");
        }

        var diagnosis = new Diagnosis
        {
            UserId = userId,
            DiagnosisName = request.DiagnosisName,
            DateDiagnosed = request.DateDiagnosed,
            DoctorName = request.DoctorName,
            Severity = request.Severity,
            Status = request.Status,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _diagnosisRepository.Add(diagnosis);
        await _unitOfWork.CompleteAsync();

        return new DiagnosisDto
        {
            Id = diagnosis.Id,
            DiagnosisName = diagnosis.DiagnosisName,
            DateDiagnosed = diagnosis.DateDiagnosed,
            DoctorName = diagnosis.DoctorName,
            Severity = diagnosis.Severity,
            Status = diagnosis.Status,
            Notes = diagnosis.Notes,
            CreatedAt = diagnosis.CreatedAt,
            UpdatedAt = diagnosis.UpdatedAt
        };
    }

    public async Task<DiagnosisDto> UpdateDiagnosisAsync(int id, int userId, UpdateDiagnosisRequest request)
    {
        var diagnosis = await _diagnosisRepository.GetByIdAsync(id);
        if (diagnosis == null || diagnosis.UserId != userId)
        {
            throw new InvalidOperationException("Diagnosis not found or access denied");
        }

        diagnosis.DiagnosisName = request.DiagnosisName;
        diagnosis.DateDiagnosed = request.DateDiagnosed;
        diagnosis.DoctorName = request.DoctorName;
        diagnosis.Severity = request.Severity;
        diagnosis.Status = request.Status;
        diagnosis.Notes = request.Notes;
        diagnosis.UpdatedAt = DateTime.UtcNow;

        _diagnosisRepository.Update(diagnosis);
        await _unitOfWork.CompleteAsync();

        return new DiagnosisDto
        {
            Id = diagnosis.Id,
            DiagnosisName = diagnosis.DiagnosisName,
            DateDiagnosed = diagnosis.DateDiagnosed,
            DoctorName = diagnosis.DoctorName,
            Severity = diagnosis.Severity,
            Status = diagnosis.Status,
            Notes = diagnosis.Notes,
            CreatedAt = diagnosis.CreatedAt,
            UpdatedAt = diagnosis.UpdatedAt
        };
    }

    public async Task<bool> DeleteDiagnosisAsync(int id, int userId)
    {
        var diagnosis = await _diagnosisRepository.GetByIdAsync(id);
        if (diagnosis == null || diagnosis.UserId != userId)
        {
            return false;
        }

        _diagnosisRepository.Delete(diagnosis);
        await _unitOfWork.CompleteAsync();
        return true;
    }
}
