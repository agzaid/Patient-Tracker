using Microsoft.Extensions.Localization;
using PatientTracker.Application.DTOs;
using PatientTracker.Application.Interfaces;
using PatientTracker.Application.Common;
using PatientTracker.Application.Resources;
using PatientTracker.Domain.Entities;
using System.Text.Json;

namespace PatientTracker.Application.Services;

public class ProfileService : IProfileService
{
    private readonly IProfileRepository _profileRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStringLocalizer<ErrorMessages> _localizer;

    public ProfileService(IProfileRepository profileRepository, IUserRepository userRepository, IUnitOfWork unitOfWork, IStringLocalizer<ErrorMessages> localizer)
    {
        _profileRepository = profileRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _localizer = localizer;
    }

    public async Task<ProfileDto?> GetProfileAsync(int userId)
    {
        var profile = await _profileRepository.GetByUserIdAsync(userId);
        if (profile == null) return null;

        return new ProfileDto
        {
            Id = profile.Id,
            FullName = profile.FullName,
            DateOfBirth = profile.DateOfBirth,
            Gender = profile.Gender,
            BloodType = profile.BloodType,
            Phone = profile.Phone,
            Email = profile.Email,
            Address = profile.Address,
            Allergies = string.IsNullOrEmpty(profile.Allergies) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(profile.Allergies) ?? new List<string>(),
            ChronicDiseases = string.IsNullOrEmpty(profile.ChronicDiseases) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(profile.ChronicDiseases) ?? new List<string>(),
            EmergencyContactName = profile.EmergencyContactName,
            EmergencyContactPhone = profile.EmergencyContactPhone,
            EmergencyContactRelation = profile.EmergencyContactRelation,
            CreatedAt = profile.CreatedAt,
            UpdatedAt = profile.UpdatedAt
        };
    }

    public async Task<ProfileDto> CreateProfileAsync(int userId, CreateProfileRequest request)
    {
        // Check if user exists
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new BusinessException(ErrorCodes.UserNotFound, _localizer["UserNotFound"]);
        }

        var existingProfile = await _profileRepository.GetByUserIdAsync(userId);
        if (existingProfile != null)
        {
            throw new BusinessException(ErrorCodes.ProfileAlreadyExists, _localizer["ProfileAlreadyExists"]);
        }

        var profile = new Profile
        {
            UserId = userId,
            FullName = request.FullName,
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender,
            BloodType = request.BloodType,
            Phone = request.Phone,
            Email = request.Email,
            Address = request.Address,
            Allergies = JsonSerializer.Serialize(request.Allergies),
            ChronicDiseases = JsonSerializer.Serialize(request.ChronicDiseases),
            EmergencyContactName = request.EmergencyContactName,
            EmergencyContactPhone = request.EmergencyContactPhone,
            EmergencyContactRelation = request.EmergencyContactRelation,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _profileRepository.Add(profile);
        await _unitOfWork.CompleteAsync();

        return new ProfileDto
        {
            Id = profile.Id,
            FullName = profile.FullName,
            DateOfBirth = profile.DateOfBirth,
            Gender = profile.Gender,
            BloodType = profile.BloodType,
            Phone = profile.Phone,
            Email = profile.Email,
            Address = profile.Address,
            Allergies = request.Allergies,
            ChronicDiseases = request.ChronicDiseases,
            EmergencyContactName = profile.EmergencyContactName,
            EmergencyContactPhone = profile.EmergencyContactPhone,
            EmergencyContactRelation = profile.EmergencyContactRelation,
            CreatedAt = profile.CreatedAt,
            UpdatedAt = profile.UpdatedAt
        };
    }

    public async Task<ProfileDto> UpdateProfileAsync(int userId, UpdateProfileRequest request)
    {
        var profile = await _profileRepository.GetByUserIdAsync(userId);
        if (profile == null)
        {
            // Create profile if it doesn't exist
            return await CreateProfileAsync(userId, new CreateProfileRequest
            {
                FullName = request.FullName,
                DateOfBirth = request.DateOfBirth,
                Gender = request.Gender,
                BloodType = request.BloodType,
                Phone = request.Phone,
                Email = request.Email,
                Address = request.Address,
                Allergies = request.Allergies,
                ChronicDiseases = request.ChronicDiseases,
                EmergencyContactName = request.EmergencyContactName,
                EmergencyContactPhone = request.EmergencyContactPhone,
                EmergencyContactRelation = request.EmergencyContactRelation
            });
        }

        profile.FullName = request.FullName;
        profile.DateOfBirth = request.DateOfBirth;
        profile.Gender = request.Gender;
        profile.BloodType = request.BloodType;
        profile.Phone = request.Phone;
        profile.Email = request.Email;
        profile.Address = request.Address;
        profile.Allergies = JsonSerializer.Serialize(request.Allergies);
        profile.ChronicDiseases = JsonSerializer.Serialize(request.ChronicDiseases);
        profile.EmergencyContactName = request.EmergencyContactName;
        profile.EmergencyContactPhone = request.EmergencyContactPhone;
        profile.EmergencyContactRelation = request.EmergencyContactRelation;
        profile.UpdatedAt = DateTime.UtcNow;

        _profileRepository.Update(profile);
        await _unitOfWork.CompleteAsync();

        return new ProfileDto
        {
            Id = profile.Id,
            FullName = profile.FullName,
            DateOfBirth = profile.DateOfBirth,
            Gender = profile.Gender,
            BloodType = profile.BloodType,
            Phone = profile.Phone,
            Email = profile.Email,
            Address = profile.Address,
            Allergies = request.Allergies,
            ChronicDiseases = request.ChronicDiseases,
            EmergencyContactName = profile.EmergencyContactName,
            EmergencyContactPhone = profile.EmergencyContactPhone,
            EmergencyContactRelation = profile.EmergencyContactRelation,
            CreatedAt = profile.CreatedAt,
            UpdatedAt = profile.UpdatedAt
        };
    }

    public async Task<bool> DeleteProfileAsync(int userId)
    {
        var profile = await _profileRepository.GetByUserIdAsync(userId);
        if (profile == null) return false;

        _profileRepository.Delete(profile);
        await _unitOfWork.CompleteAsync();
        
        return true;
    }
}
