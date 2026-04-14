using Microsoft.EntityFrameworkCore;
using PatientTracker.Application.Interfaces;
using PatientTracker.Domain.Entities;
using PatientTracker.Infrastructure.Data;

namespace PatientTracker.Infrastructure.Repositories;

public class DiagnosisRepository : GenericRepository<Diagnosis>, IDiagnosisRepository
{
    public DiagnosisRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Diagnosis>> GetByUserIdAsync(int userId)
    {
        return await _context.Diagnoses
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.DateDiagnosed)
            .ToListAsync();
    }
}
