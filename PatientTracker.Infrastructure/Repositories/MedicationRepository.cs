using Microsoft.EntityFrameworkCore;
using PatientTracker.Application.Interfaces;
using PatientTracker.Domain.Entities;
using PatientTracker.Infrastructure.Data;

namespace PatientTracker.Infrastructure.Repositories;

public class MedicationRepository : GenericRepository<Medication>, IMedicationRepository
{
    public MedicationRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Medication>> GetByUserIdAsync(int userId)
    {
        return await _context.Medications
            .Where(m => m.UserId == userId)
            .OrderByDescending(m => m.StartDate)
            .ToListAsync();
    }
}
