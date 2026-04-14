using PatientTracker.Application.DTOs;
using PatientTracker.Application.Interfaces;
using PatientTracker.Domain.Entities;

namespace PatientTracker.Application.Services;

public class RadiologyService : IRadiologyService
{
    private readonly IRadiologyRepository _radiologyRepository;
    private readonly IUserRepository _userRepository;

    public RadiologyService(IRadiologyRepository radiologyRepository, IUserRepository userRepository)
    {
        _radiologyRepository = radiologyRepository;
        _userRepository = userRepository;
    }

    public async Task<IEnumerable<RadiologyScanDto>> GetRadiologyScansAsync(int userId)
    {
        var radiologyScans = await _radiologyRepository.GetByUserIdAsync(userId);
        return radiologyScans.Select(r => new RadiologyScanDto
        {
            Id = r.Id,
            ScanType = r.ScanType,
            BodyPart = r.BodyPart,
            ScanDate = r.ScanDate,
            Description = r.Description,
            DoctorNotes = r.DoctorNotes,
            ReportUrl = r.ReportUrl,
            HospitalName = r.HospitalName,
            DoctorName = r.DoctorName,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        });
    }

    public async Task<RadiologyScanDto?> GetRadiologyScanAsync(int id, int userId)
    {
        var radiology = await _radiologyRepository.GetByIdAsync(id);
        if (radiology == null || radiology.UserId != userId)
            return null;

        return new RadiologyScanDto
        {
            Id = radiology.Id,
            ScanType = radiology.ScanType,
            BodyPart = radiology.BodyPart,
            ScanDate = radiology.ScanDate,
            Description = radiology.Description,
            DoctorNotes = radiology.DoctorNotes,
            ReportUrl = radiology.ReportUrl,
            HospitalName = radiology.HospitalName,
            DoctorName = radiology.DoctorName,
            CreatedAt = radiology.CreatedAt,
            UpdatedAt = radiology.UpdatedAt
        };
    }

    public async Task<RadiologyScanDto> CreateRadiologyScanAsync(int userId, CreateRadiologyScanRequest request)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            throw new InvalidOperationException("User not found");

        var radiology = new RadiologyScan
        {
            UserId = userId,
            ScanType = request.ScanType,
            BodyPart = request.BodyPart,
            ScanDate = request.ScanDate,
            Description = request.Description,
            DoctorNotes = request.DoctorNotes,
            ReportUrl = request.ReportUrl,
            HospitalName = request.HospitalName,
            DoctorName = request.DoctorName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var createdRadiology = await _radiologyRepository.CreateAsync(radiology);

        return new RadiologyScanDto
        {
            Id = createdRadiology.Id,
            ScanType = createdRadiology.ScanType,
            BodyPart = createdRadiology.BodyPart,
            ScanDate = createdRadiology.ScanDate,
            Description = createdRadiology.Description,
            DoctorNotes = createdRadiology.DoctorNotes,
            ReportUrl = createdRadiology.ReportUrl,
            HospitalName = createdRadiology.HospitalName,
            DoctorName = createdRadiology.DoctorName,
            CreatedAt = createdRadiology.CreatedAt,
            UpdatedAt = createdRadiology.UpdatedAt
        };
    }

    public async Task<RadiologyScanDto> UpdateRadiologyScanAsync(int id, int userId, UpdateRadiologyScanRequest request)
    {
        var scan = await _radiologyRepository.GetByIdAsync(id);
        if (scan == null || scan.UserId != userId)
            throw new InvalidOperationException("Radiology scan not found or access denied");

        scan.ScanType = request.ScanType;
        scan.BodyPart = request.BodyPart;
        scan.ScanDate = request.ScanDate;
        scan.Description = request.Description;
        scan.DoctorNotes = request.DoctorNotes;
        scan.ReportUrl = request.ReportUrl;
        scan.HospitalName = request.HospitalName;
        scan.DoctorName = request.DoctorName;
        scan.UpdatedAt = DateTime.UtcNow;

        var updatedScan = await _radiologyRepository.UpdateAsync(scan);

        return new RadiologyScanDto
        {
            Id = updatedScan.Id,
            ScanType = updatedScan.ScanType,
            BodyPart = updatedScan.BodyPart,
            ScanDate = updatedScan.ScanDate,
            Description = updatedScan.Description,
            DoctorNotes = updatedScan.DoctorNotes,
            ReportUrl = updatedScan.ReportUrl,
            HospitalName = updatedScan.HospitalName,
            DoctorName = updatedScan.DoctorName,
            CreatedAt = updatedScan.CreatedAt,
            UpdatedAt = updatedScan.UpdatedAt
        };
    }

    public async Task<bool> DeleteRadiologyScanAsync(int id, int userId)
    {
        var scan = await _radiologyRepository.GetByIdAsync(id);
        if (scan == null || scan.UserId != userId)
            return false;

        return await _radiologyRepository.DeleteAsync(id);
    }
}
