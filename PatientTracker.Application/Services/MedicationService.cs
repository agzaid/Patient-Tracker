using Microsoft.Extensions.Localization;
using PatientTracker.Application.DTOs;
using PatientTracker.Application.Interfaces;
using PatientTracker.Application.Resources;
using PatientTracker.Domain.Entities;
using PatientTracker.Domain.Enums;

namespace PatientTracker.Application.Services;

public class MedicationService : IMedicationService
{
    private readonly IMedicationRepository _medicationRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDocumentRepository _documentRepository;
    private readonly IStringLocalizer<ErrorMessages> _localizer;

    public MedicationService(IMedicationRepository medicationRepository, IUserRepository userRepository, IUnitOfWork unitOfWork, IDocumentRepository documentRepository, IStringLocalizer<ErrorMessages> localizer)
    {
        _medicationRepository = medicationRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _documentRepository = documentRepository;
        _localizer = localizer;
    }

    public async Task<IEnumerable<MedicationDto>> GetMedicationsAsync(int userId)
    {
        var medications = await _medicationRepository.GetByUserIdAsync(userId);
        return medications.Select(m => new MedicationDto
        {
            Id = m.Id,
            Name = m.Name,
            Dosage = m.Dosage,
            Frequency = m.Frequency,
            StartDate = m.StartDate,
            EndDate = m.EndDate,
            IsCurrent = m.IsCurrent,
            Notes = m.Notes,
            PrescriptionUrl = m.PrescriptionUrl,
            CreatedAt = m.CreatedAt,
            UpdatedAt = m.UpdatedAt
        });
    }

    public async Task<MedicationDto?> GetMedicationAsync(int id, int userId)
    {
        var medication = await _medicationRepository.GetByIdAsync(id);
        if (medication == null || medication.UserId != userId)
        {
            return null;
        }

        return new MedicationDto
        {
            Id = medication.Id,
            Name = medication.Name,
            Dosage = medication.Dosage,
            Frequency = medication.Frequency,
            StartDate = medication.StartDate,
            EndDate = medication.EndDate,
            IsCurrent = medication.IsCurrent,
            Notes = medication.Notes,
            PrescriptionUrl = medication.PrescriptionUrl,
            CreatedAt = medication.CreatedAt,
            UpdatedAt = medication.UpdatedAt
        };
    }

    public async Task<MedicationDto> CreateMedicationAsync(int userId, CreateMedicationRequest request)
    {
        // Verify user exists
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new InvalidOperationException(_localizer["UserNotFound"]);
        }

        var medication = new Medication
        {
            UserId = userId,
            Name = request.Name,
            Dosage = request.Dosage,
            Frequency = request.Frequency,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            IsCurrent = request.IsCurrent,
            Notes = request.Notes,
            PrescriptionUrl = request.PrescriptionUrl,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _medicationRepository.Add(medication);
        await _unitOfWork.CompleteAsync();

        // If there are any temporary documents for this user with ParentEntityType = Medication and ParentEntityId = null,
        // update them to link to this medication
        if (request.DocumentIds != null && request.DocumentIds.Any())
        {
            var documents = await _documentRepository.GetByIdsAsync(request.DocumentIds);
            foreach (var document in documents)
            {
                if (document.UserId == userId && document.ParentEntityType == ParentEntityType.Medication && document.ParentEntityId == null)
                {
                    document.ParentEntityId = medication.Id;
                    document.UpdatedAt = DateTime.UtcNow;
                    _documentRepository.Update(document);
                }
            }
            await _unitOfWork.CompleteAsync();
        }

        return new MedicationDto
        {
            Id = medication.Id,
            Name = medication.Name,
            Dosage = medication.Dosage,
            Frequency = medication.Frequency,
            StartDate = medication.StartDate,
            EndDate = medication.EndDate,
            IsCurrent = medication.IsCurrent,
            Notes = medication.Notes,
            PrescriptionUrl = medication.PrescriptionUrl,
            CreatedAt = medication.CreatedAt,
            UpdatedAt = medication.UpdatedAt
        };
    }

    public async Task<MedicationDto> UpdateMedicationAsync(int id, int userId, UpdateMedicationRequest request)
    {
        var medication = await _medicationRepository.GetByIdAsync(id);
        if (medication == null || medication.UserId != userId)
        {
            throw new InvalidOperationException(_localizer["MedicationNotFound"]);
        }

        medication.Name = request.Name;
        medication.Dosage = request.Dosage;
        medication.Frequency = request.Frequency;
        medication.StartDate = request.StartDate;
        medication.EndDate = request.EndDate;
        medication.IsCurrent = request.IsCurrent;
        medication.Notes = request.Notes;
        medication.PrescriptionUrl = request.PrescriptionUrl;
        medication.UpdatedAt = DateTime.UtcNow;

        _medicationRepository.Update(medication);
        await _unitOfWork.CompleteAsync();

        return new MedicationDto
        {
            Id = medication.Id,
            Name = medication.Name,
            Dosage = medication.Dosage,
            Frequency = medication.Frequency,
            StartDate = medication.StartDate,
            EndDate = medication.EndDate,
            IsCurrent = medication.IsCurrent,
            Notes = medication.Notes,
            PrescriptionUrl = medication.PrescriptionUrl,
            CreatedAt = medication.CreatedAt,
            UpdatedAt = medication.UpdatedAt
        };
    }

    public async Task<bool> DeleteMedicationAsync(int id, int userId)
    {
        var medication = await _medicationRepository.GetByIdAsync(id);
        if (medication == null || medication.UserId != userId)
        {
            return false;
        }

        _medicationRepository.Delete(medication);
        await _unitOfWork.CompleteAsync();
        return true;
    }
}
