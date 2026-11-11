using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using SmartStart.Domain.Entities;

namespace SmartStart.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await EnsureDatabaseExistsAsync(db);
        await db.Database.MigrateAsync();

        if (!await db.Users.AnyAsync(u => u.Role == UserRole.Admin))
        {
            db.Users.Add(new User
            {
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                Role = UserRole.Admin,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        if (!await db.PhotocopySettings.AnyAsync())
        {
            db.PhotocopySettings.Add(new PhotocopySettings
            {
                MaintenanceCost = 10m,
                ElectricityCost = 20m
            });
        }

        if (!await db.PhotocopyInkLevels.AnyAsync())
        {
            db.PhotocopyInkLevels.AddRange(
                new PhotocopyInkLevel { Level = InkCoverageLevel.Low },
                new PhotocopyInkLevel { Level = InkCoverageLevel.Medium },
                new PhotocopyInkLevel { Level = InkCoverageLevel.High }
            );
        }

        await db.SaveChangesAsync();
    }

    private static async Task EnsureDatabaseExistsAsync(ApplicationDbContext db)
    {
        var connectionString = db.Database.GetConnectionString()
            ?? throw new InvalidOperationException("Connection string is missing");

        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        var database = builder.Database;
        if (string.IsNullOrWhiteSpace(database))
            throw new InvalidOperationException("Database name is missing");

        builder.Database = "postgres";
        await using var conn = new NpgsqlConnection(builder.ConnectionString);
        await conn.OpenAsync();

        await using var existsCmd = new NpgsqlCommand("SELECT 1 FROM pg_database WHERE datname = @name", conn);
        existsCmd.Parameters.AddWithValue("name", database);
        if (await existsCmd.ExecuteScalarAsync() is not null)
            return;

        var safeName = database.Replace("\"", string.Empty);
        await using var createCmd = new NpgsqlCommand($@"CREATE DATABASE ""{safeName}""", conn);
        await createCmd.ExecuteNonQueryAsync();
    }
}
