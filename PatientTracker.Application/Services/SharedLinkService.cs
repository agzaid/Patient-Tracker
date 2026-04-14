using PatientTracker.Application.DTOs;
using PatientTracker.Application.Interfaces;
using PatientTracker.Domain.Entities;
using System.Text.Json;

namespace PatientTracker.Application.Services;

public class SharedLinkService : ISharedLinkService
{
    private readonly ISharedLinkRepository _sharedLinkRepository;
    private readonly IUserRepository _userRepository;
    private readonly IProfileService _profileService;
    private readonly IMedicationService _medicationService;
    private readonly ILabTestService _labTestService;
    private readonly IRadiologyService _radiologyService;
    private readonly IDiagnosisService _diagnosisService;
    private readonly ISurgeryService _surgeryService;
    private readonly IUnitOfWork _unitOfWork;

    public SharedLinkService(
        ISharedLinkRepository sharedLinkRepository,
        IUserRepository userRepository,
        IProfileService profileService,
        IMedicationService medicationService,
        ILabTestService labTestService,
        IRadiologyService radiologyService,
        IDiagnosisService diagnosisService,
        ISurgeryService surgeryService,
        IUnitOfWork unitOfWork)
    {
        _sharedLinkRepository = sharedLinkRepository;
        _userRepository = userRepository;
        _profileService = profileService;
        _medicationService = medicationService;
        _labTestService = labTestService;
        _radiologyService = radiologyService;
        _diagnosisService = diagnosisService;
        _surgeryService = surgeryService;
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<SharedLinkDto>> GetSharedLinksAsync(int userId)
    {
        var links = await _sharedLinkRepository.GetByUserIdAsync(userId);
        return links.Select(l => new SharedLinkDto
        {
            Id = l.Id,
            Token = l.Token,
            ExpiresAt = l.ExpiresAt,
            Categories = string.IsNullOrEmpty(l.Categories) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(l.Categories) ?? new List<string>(),
            IsActive = l.IsActive,
            AccessCount = l.AccessCount,
            CreatedAt = l.CreatedAt,
            UpdatedAt = l.UpdatedAt
        });
    }

    public async Task<SharedLinkDto> CreateSharedLinkAsync(int userId, CreateSharedLinkRequest request)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new InvalidOperationException("User not found");
        }

        var token = Guid.NewGuid().ToString();
        DateTime? expiresAt = null;

        if (request.Expiry != "never")
        {
            var d = DateTime.UtcNow;
            expiresAt = request.Expiry switch
            {
                "24h" => d.AddHours(24),
                "7d" => d.AddDays(7),
                "30d" => d.AddDays(30),
                _ => null
            };
        }

        var link = new SharedLink
        {
            UserId = userId,
            Token = token,
            ExpiresAt = expiresAt,
            Categories = JsonSerializer.Serialize(request.Categories),
            IsActive = true,
            AccessCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _sharedLinkRepository.Add(link);
        await _unitOfWork.CompleteAsync();

        return new SharedLinkDto
        {
            Id = link.Id,
            Token = link.Token,
            ExpiresAt = link.ExpiresAt,
            Categories = request.Categories,
            IsActive = link.IsActive,
            AccessCount = link.AccessCount,
            CreatedAt = link.CreatedAt,
            UpdatedAt = link.UpdatedAt
        };
    }

    public async Task<bool> DeleteSharedLinkAsync(int id, int userId)
    {
        var link = await _sharedLinkRepository.GetByIdAsync(id);
        if (link == null || link.UserId != userId)
        {
            return false;
        }

        _sharedLinkRepository.Delete(link);
        await _unitOfWork.CompleteAsync();
        return true;
    }

    public async Task<bool> ToggleSharedLinkAsync(int id, int userId)
    {
        var link = await _sharedLinkRepository.GetByIdAsync(id);
        if (link == null || link.UserId != userId)
        {
            return false;
        }

        link.IsActive = !link.IsActive;
        link.UpdatedAt = DateTime.UtcNow;
        _sharedLinkRepository.Update(link);
        await _unitOfWork.CompleteAsync();
        return true;
    }

    public async Task<SharedProfileResponse?> GetSharedProfileAsync(string token)
    {
        var link = await _sharedLinkRepository.GetByTokenAsync(token);
        if (link == null || !link.IsActive || (link.ExpiresAt.HasValue && link.ExpiresAt.Value < DateTime.UtcNow))
        {
            return null;
        }

        // Increment access count
        await _sharedLinkRepository.IncrementAccessCountAsync(token);

        var categories = string.IsNullOrEmpty(link.Categories) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(link.Categories) ?? new List<string>();
        var userId = link.UserId;

        var response = new SharedProfileResponse();

        // Always include profile
        var profile = await _profileService.GetProfileAsync(userId);
        if (profile != null)
        {
            response.Profile = profile;
        }

        // Include categories based on what was shared
        if (categories.Contains("medications"))
        {
            var medications = await _medicationService.GetMedicationsAsync(userId);
            response.Medications = medications.ToList();
        }

        if (categories.Contains("lab_tests"))
        {
            var labTests = await _labTestService.GetLabTestsAsync(userId);
            response.LabTests = labTests.ToList();
        }

        if (categories.Contains("radiology"))
        {
            var radiologyScans = await _radiologyService.GetRadiologyScansAsync(userId);
            response.RadiologyScans = radiologyScans.ToList();
        }

        if (categories.Contains("diagnoses"))
        {
            var diagnoses = await _diagnosisService.GetDiagnosesAsync(userId);
            response.Diagnoses = diagnoses.ToList();
        }

        if (categories.Contains("surgeries"))
        {
            var surgeries = await _surgeryService.GetSurgeriesAsync(userId);
            response.Surgeries = surgeries.ToList();
        }

        return response;
    }
}
