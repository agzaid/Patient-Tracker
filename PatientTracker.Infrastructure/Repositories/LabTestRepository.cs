using Microsoft.EntityFrameworkCore;
using PatientTracker.Application.Interfaces;
using PatientTracker.Domain.Entities;
using PatientTracker.Infrastructure.Data;

namespace PatientTracker.Infrastructure.Repositories;

public class LabTestRepository : GenericRepository<LabTest>, ILabTestRepository
{
    public LabTestRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<LabTest>> GetByUserIdAsync(int userId)
    {
        return await _context.LabTests
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.TestDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<LabTest>> GetByUserIdAsync(int userId, int page, int pageSize)
    {
        return await _context.LabTests
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.TestDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<LabTest>> GetByUserIdAsync(int userId, int page, int pageSize, string? search)
    {
        var query = _context.LabTests.Where(l => l.UserId == userId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(l => 
                l.TestName.Contains(search) ||
                (l.ResultValue != null && l.ResultValue.Contains(search)) ||
                (l.ResultUnit != null && l.ResultUnit.Contains(search)) ||
                (l.Status != null && l.Status.Contains(search)) ||
                (l.Notes != null && l.Notes.Contains(search)));
        }

        return await query
            .OrderByDescending(l => l.TestDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> CountByUserIdAsync(int userId)
    {
        return await _context.LabTests
            .CountAsync(l => l.UserId == userId);
    }

    public async Task<int> CountByUserIdAsync(int userId, string? search)
    {
        var query = _context.LabTests.Where(l => l.UserId == userId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(l => 
                l.TestName.Contains(search) ||
                (l.ResultValue != null && l.ResultValue.Contains(search)) ||
                (l.ResultUnit != null && l.ResultUnit.Contains(search)) ||
                (l.Status != null && l.Status.Contains(search)) ||
                (l.Notes != null && l.Notes.Contains(search)));
        }

        return await query.CountAsync();
    }

    public async Task<IEnumerable<LabTest>> GetByDocumentIdAsync(int documentId)
    {
        return await _context.LabTests
            .Where(l => l.LabTestDocumentId == documentId)
            .OrderBy(l => l.TestName)
            .ToListAsync();
    }

    public void AddRange(IEnumerable<LabTest> labTests)
    {
        _context.LabTests.AddRange(labTests);
    }

    public void DeleteRange(IEnumerable<LabTest> labTests)
    {
        _context.LabTests.RemoveRange(labTests);
    }
}
