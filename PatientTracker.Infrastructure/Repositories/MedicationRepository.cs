using Microsoft.EntityFrameworkCore;
using PatientTracker.Application.Interfaces;
using PatientTracker.Domain.Entities;
using PatientTracker.Infrastructure.Data;

namespace PatientTracker.Infrastructure.Repositories;

public class MedicationRepository : IMedicationRepository
{
    private readonly ApplicationDbContext _context;

    public MedicationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Medication>> GetByUserIdAsync(int userId)
    {
        return await _context.Medications
            .Where(m => m.UserId == userId)
            .OrderByDescending(m => m.StartDate)
            .ToListAsync();
    }

    public async Task<Medication?> GetByIdAsync(int id)
    {
        return await _context.Medications.FindAsync(id);
    }

    public async Task<Medication> CreateAsync(Medication medication)
    {
        _context.Medications.Add(medication);
        await _context.SaveChangesAsync();
        return medication;
    }

    public async Task<Medication> UpdateAsync(Medication medication)
    {
        _context.Medications.Update(medication);
        await _context.SaveChangesAsync();
        return medication;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var medication = await _context.Medications.FindAsync(id);
        if (medication == null) return false;

        _context.Medications.Remove(medication);
        await _context.SaveChangesAsync();
        return true;
    }
}
