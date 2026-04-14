using Microsoft.EntityFrameworkCore;
using PatientTracker.Application.Interfaces;
using PatientTracker.Domain.Entities;
using PatientTracker.Infrastructure.Data;

namespace PatientTracker.Infrastructure.Repositories;

public class LabTestRepository : ILabTestRepository
{
    private readonly ApplicationDbContext _context;

    public LabTestRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<LabTest>> GetByUserIdAsync(int userId)
    {
        return await _context.LabTests
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.TestDate)
            .ToListAsync();
    }

    public async Task<LabTest?> GetByIdAsync(int id)
    {
        return await _context.LabTests.FindAsync(id);
    }

    public async Task<LabTest> CreateAsync(LabTest labTest)
    {
        _context.LabTests.Add(labTest);
        await _context.SaveChangesAsync();
        return labTest;
    }

    public async Task<LabTest> UpdateAsync(LabTest labTest)
    {
        _context.LabTests.Update(labTest);
        await _context.SaveChangesAsync();
        return labTest;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var labTest = await _context.LabTests.FindAsync(id);
        if (labTest == null) return false;

        _context.LabTests.Remove(labTest);
        await _context.SaveChangesAsync();
        return true;
    }
}
