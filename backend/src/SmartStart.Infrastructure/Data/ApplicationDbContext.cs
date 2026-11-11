using Microsoft.EntityFrameworkCore;
using SmartStart.Application.Interfaces;
using SmartStart.Domain.Entities;

namespace SmartStart.Infrastructure.Data;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<RestockEvent> RestockEvents => Set<RestockEvent>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public DbSet<ReturnItem> Returns => Set<ReturnItem>();
    public DbSet<BarcodeChangeLog> BarcodeChangeLogs => Set<BarcodeChangeLog>();
    public DbSet<PhotocopySettings> PhotocopySettings => Set<PhotocopySettings>();
    public DbSet<PhotocopyPaperType> PhotocopyPaperTypes => Set<PhotocopyPaperType>();
    public DbSet<PhotocopyInkLevel> PhotocopyInkLevels => Set<PhotocopyInkLevel>();
    public DbSet<PhotocopySale> PhotocopySales => Set<PhotocopySale>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Username).IsUnique();
            entity.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasIndex(p => p.Barcode).IsUnique();
            entity.HasIndex(p => p.Name);
            entity.Property(p => p.ProfitMarginPercent)
                .HasComputedColumnSql(
                    "COALESCE(ROUND(((\"SellingPrice\" - \"WholesalePrice\") / NULLIF(\"WholesalePrice\", 0)) * 100, 2), 0)",
                    stored: true);
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasIndex(i => i.QrCode).IsUnique();
            entity.Property(i => i.QrCode).HasMaxLength(32);
            entity.HasOne(i => i.Cashier)
                .WithMany()
                .HasForeignKey(i => i.CashierId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InvoiceItem>(entity =>
        {
            entity.Property(i => i.LineTotal)
                .HasComputedColumnSql("\"Quantity\" * \"UnitSellingPrice\"", stored: true);

            entity.Property(i => i.LineProfit)
                .HasComputedColumnSql("\"Quantity\" * (\"UnitSellingPrice\" - \"UnitWholesalePrice\")", stored: true);

            entity.HasOne(i => i.Product)
                .WithMany()
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ReturnItem>(entity =>
        {
            entity.ToTable("Returns");
            entity.HasOne(r => r.InvoiceItem)
                .WithMany(i => i.Returns)
                .HasForeignKey(r => r.InvoiceItemId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BarcodeChangeLog>(entity =>
        {
            entity.ToTable("BarcodeChangeLogs");
            entity.HasOne(l => l.Product)
                .WithMany()
                .HasForeignKey(l => l.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(l => l.ChangedByUser)
                .WithMany()
                .HasForeignKey(l => l.ChangedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(l => l.ProductId);
            entity.Property(l => l.OldBarcode).HasMaxLength(64);
            entity.Property(l => l.NewBarcode).HasMaxLength(64);
        });

        modelBuilder.Entity<PhotocopySettings>(entity =>
        {
            entity.Property(s => s.MaintenanceCost).HasPrecision(18, 4);
            entity.Property(s => s.ElectricityCost).HasPrecision(18, 4);
        });

        modelBuilder.Entity<PhotocopyPaperType>(entity =>
        {
            entity.HasIndex(p => p.Name).IsUnique();
            entity.Property(p => p.Name).HasMaxLength(120);
            entity.Property(p => p.WholesalePrice).HasPrecision(18, 4);
            entity.Property(p => p.SellingPrice).HasPrecision(18, 4);
        });

        modelBuilder.Entity<PhotocopyInkLevel>(entity =>
        {
            entity.HasIndex(i => i.Level).IsUnique();
            entity.Property(i => i.Level).HasConversion<string>().HasMaxLength(20);
            entity.Property(i => i.WholesalePerPage).HasPrecision(18, 4);
            entity.Property(i => i.SellingPerPage).HasPrecision(18, 4);
        });

        modelBuilder.Entity<PhotocopySale>(entity =>
        {
            entity.HasIndex(s => s.SaleNumber).IsUnique();
            entity.Property(s => s.SaleNumber).HasMaxLength(40);
            entity.Property(s => s.PaperName).HasMaxLength(120);
            entity.Property(s => s.InkLevel).HasConversion<string>().HasMaxLength(20);
            entity.Property(s => s.PaperWholesalePrice).HasPrecision(18, 4);
            entity.Property(s => s.PaperSellingPrice).HasPrecision(18, 4);
            entity.Property(s => s.InkWholesalePerPage).HasPrecision(18, 4);
            entity.Property(s => s.InkSellingPerPage).HasPrecision(18, 4);
            entity.Property(s => s.MaintenanceCost).HasPrecision(18, 4);
            entity.Property(s => s.ElectricityCost).HasPrecision(18, 4);
            entity.Property(s => s.TotalAmount).HasPrecision(18, 4);
            entity.Property(s => s.CostAmount).HasPrecision(18, 4);
            entity.Property(s => s.ProfitAmount).HasPrecision(18, 4);
            entity.Property(s => s.ProfitPercent).HasPrecision(18, 4);
            entity.HasOne(s => s.Cashier)
                .WithMany()
                .HasForeignKey(s => s.CashierId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(s => s.PaperType)
                .WithMany()
                .HasForeignKey(s => s.PaperTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
