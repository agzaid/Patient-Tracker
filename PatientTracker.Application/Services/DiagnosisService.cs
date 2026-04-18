using Microsoft.Extensions.Localization;
using PatientTracker.Application.DTOs;
using PatientTracker.Application.Interfaces;
using PatientTracker.Application.Resources;
using PatientTracker.Domain.Entities;
using PatientTracker.Domain.Enums;

namespace PatientTracker.Application.Services;

public class DiagnosisService : IDiagnosisService
{
    private readonly IDiagnosisRepository _diagnosisRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDocumentRepository _documentRepository;
    private readonly IStringLocalizer<ErrorMessages> _localizer;

    public DiagnosisService(IDiagnosisRepository diagnosisRepository, IUserRepository userRepository, IUnitOfWork unitOfWork, IDocumentRepository documentRepository, IStringLocalizer<ErrorMessages> localizer)
    {
        _diagnosisRepository = diagnosisRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _documentRepository = documentRepository;
        _localizer = localizer;
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

    public async Task<PaginatedResponse<DiagnosisDto>> GetDiagnosesPaginatedAsync(int userId, int page = 1, int pageSize = 10, string? search = null)
    {
        // Ensure page and pageSize are valid
        page = Math.Max(1, page);
        pageSize = Math.Max(1, Math.Min(100, pageSize)); // Limit max page size to 100

        var totalCount = await _diagnosisRepository.CountByUserIdAsync(userId, search);
        var diagnoses = await _diagnosisRepository.GetByUserIdAsync(userId, page, pageSize, search);

        var diagnosisDtos = diagnoses.Select(d => new DiagnosisDto
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

        return new PaginatedResponse<DiagnosisDto>
        {
            Items = diagnosisDtos,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
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
        // Verify user exists
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new InvalidOperationException(_localizer["UserNotFound"]);
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

        // If there are any temporary documents for this user with ParentEntityType = Diagnosis and ParentEntityId = null,
        // update them to link to this diagnosis
        if (request.DocumentIds != null && request.DocumentIds.Any())
        {
            var documents = await _documentRepository.GetByIdsAsync(request.DocumentIds);
            foreach (var document in documents)
            {
                if (document.UserId == userId && document.ParentEntityType == ParentEntityType.Diagnosis && document.ParentEntityId == null)
                {
                    document.ParentEntityId = diagnosis.Id;
                    document.UpdatedAt = DateTime.UtcNow;
                    _documentRepository.Update(document);
                }
            }
            await _unitOfWork.CompleteAsync();
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

    public async Task<DiagnosisDto> UpdateDiagnosisAsync(int id, int userId, UpdateDiagnosisRequest request)
    {
        var diagnosis = await _diagnosisRepository.GetByIdAsync(id);
        if (diagnosis == null || diagnosis.UserId != userId)
        {
            throw new InvalidOperationException(_localizer["DiagnosisNotFound"]);
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
