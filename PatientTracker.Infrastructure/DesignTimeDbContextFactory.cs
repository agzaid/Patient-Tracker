using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using PatientTracker.Infrastructure.Data;

namespace PatientTracker.Infrastructure;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        
        // Use a connection string that works for design-time operations
        // This connects to master first to create the PatientTracker database if needed
        var connectionString = "Server=.;Database=PatientTracker;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";
        
        optionsBuilder.UseSqlServer(connectionString);
        
        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
