using Microsoft.EntityFrameworkCore;
using PatientTracker.Application.Interfaces;
using PatientTracker.Domain.Entities;
using PatientTracker.Infrastructure.Data;

namespace PatientTracker.Infrastructure.Repositories;

public class DiagnosisRepository : IDiagnosisRepository
{
    private readonly ApplicationDbContext _context;

    public DiagnosisRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Diagnosis>> GetByUserIdAsync(int userId)
    {
        return await _context.Diagnoses
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.DateDiagnosed)
            .ToListAsync();
    }

    public async Task<Diagnosis?> GetByIdAsync(int id)
    {
        return await _context.Diagnoses.FindAsync(id);
    }

    public async Task<Diagnosis> CreateAsync(Diagnosis diagnosis)
    {
        _context.Diagnoses.Add(diagnosis);
        await _context.SaveChangesAsync();
        return diagnosis;
    }

    public async Task<Diagnosis> UpdateAsync(Diagnosis diagnosis)
    {
        _context.Diagnoses.Update(diagnosis);
        await _context.SaveChangesAsync();
        return diagnosis;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var diagnosis = await _context.Diagnoses.FindAsync(id);
        if (diagnosis == null) return false;

        _context.Diagnoses.Remove(diagnosis);
        await _context.SaveChangesAsync();
        return true;
    }
}
