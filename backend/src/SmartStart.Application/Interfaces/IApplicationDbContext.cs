using Microsoft.EntityFrameworkCore;
using SmartStart.Domain.Entities;

namespace SmartStart.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Product> Products { get; }
    DbSet<RestockEvent> RestockEvents { get; }
    DbSet<Invoice> Invoices { get; }
    DbSet<InvoiceItem> InvoiceItems { get; }
    DbSet<ReturnItem> Returns { get; }
    DbSet<BarcodeChangeLog> BarcodeChangeLogs { get; }
    DbSet<PhotocopySettings> PhotocopySettings { get; }
    DbSet<PhotocopyPaperType> PhotocopyPaperTypes { get; }
    DbSet<PhotocopyInkLevel> PhotocopyInkLevels { get; }
    DbSet<PhotocopySale> PhotocopySales { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}