using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SmartStart.Infrastructure.Data;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        
        // نص الاتصال الافتراضي لوقت التصميم والـ Migrations
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=smartstart_db;Username=postgres;Password=123456");

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}