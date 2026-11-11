using Microsoft.EntityFrameworkCore;
using SmartStart.Application.DTOs;
using SmartStart.Application.Interfaces;
using SmartStart.Domain.Entities;

namespace SmartStart.Application.Services;

public class ReportService : IReportService
{
    private readonly IApplicationDbContext _context;

    public ReportService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ProfitReportDto> GetProfitReportAsync(DateTime startDate, DateTime endDate)
    {
        var items = await _context.InvoiceItems
            .Include(i => i.Invoice)
            .Include(i => i.Product)
            .Include(i => i.Returns)
            .Where(i => i.Invoice.CreatedAt >= startDate && i.Invoice.CreatedAt <= endDate)
            .ToListAsync();

        var productLines = items
            .GroupBy(i => new { i.ProductId, Name = i.Product?.Name ?? "غير معروف", Barcode = i.Product?.Barcode ?? "" })
            .Select(g =>
            {
                var qtySold = g.Sum(x => x.Quantity - x.Returns.Sum(r => r.QuantityReturned));
                var revenue = g.Sum(x =>
                {
                    var returned = x.Returns.Sum(r => r.QuantityReturned);
                    return (x.Quantity - returned) * x.UnitSellingPrice;
                });
                var profit = g.Sum(x =>
                {
                    var returned = x.Returns.Sum(r => r.QuantityReturned);
                    return (x.Quantity - returned) * (x.UnitSellingPrice - x.UnitWholesalePrice);
                });

                return new ProductProfitLineDto(
                    g.Key.ProductId,
                    g.Key.Name,
                    g.Key.Barcode,
                    qtySold,
                    Math.Round(revenue, 2),
                    Math.Round(profit, 2)
                );
            })
            .Where(p => p.QuantitySold > 0)
            .OrderByDescending(p => p.TotalProfit)
            .ToList();

        var copySales = await _context.PhotocopySales
            .Where(s => s.CreatedAt >= startDate && s.CreatedAt <= endDate)
            .ToListAsync();

        var copyLines = copySales
            .GroupBy(s => new { s.PaperTypeId, s.PaperName, s.InkLevel })
            .Select(g => new ProductProfitLineDto(
                -((g.Key.PaperTypeId * 10) + (int)g.Key.InkLevel),
                $"تصوير — {g.Key.PaperName} / {InkLabel(g.Key.InkLevel)}",
                "COPY",
                g.Sum(x => x.PageCount),
                Math.Round(g.Sum(x => x.TotalAmount), 2),
                Math.Round(g.Sum(x => x.ProfitAmount), 2)
            ))
            .OrderByDescending(p => p.TotalProfit)
            .ToList();

        var allLines = productLines.Concat(copyLines).ToList();

        decimal totalSales = allLines.Sum(p => p.TotalRevenue);
        decimal totalProfit = allLines.Sum(p => p.TotalProfit);
        decimal totalCost = totalSales - totalProfit;
        int totalInvoices = items.Select(i => i.InvoiceId).Distinct().Count() + copySales.Count;

        return new ProfitReportDto(
            totalSales,
            totalCost,
            totalProfit,
            totalInvoices,
            startDate,
            endDate,
            allLines
        );
    }

    public async Task<IEnumerable<TopSellingProductDto>> GetTopSellingProductsAsync(
        int count = 10,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var query = _context.InvoiceItems
            .Include(i => i.Product)
            .Include(i => i.Invoice)
            .Include(i => i.Returns)
            .AsQueryable();

        if (startDate.HasValue)
            query = query.Where(i => i.Invoice.CreatedAt >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(i => i.Invoice.CreatedAt <= endDate.Value);

        var items = await query.ToListAsync();

        return items
            .GroupBy(i => new { i.ProductId, Name = i.Product?.Name ?? "غير معروف", Barcode = i.Product?.Barcode ?? "" })
            .Select(g =>
            {
                var qty = g.Sum(x => x.Quantity - x.Returns.Sum(r => r.QuantityReturned));
                var revenue = g.Sum(x =>
                {
                    var returned = x.Returns.Sum(r => r.QuantityReturned);
                    return (x.Quantity - returned) * x.UnitSellingPrice;
                });
                return new TopSellingProductDto(g.Key.ProductId, g.Key.Name, g.Key.Barcode, qty, Math.Round(revenue, 2));
            })
            .Where(x => x.TotalQuantitySold > 0)
            .OrderByDescending(x => x.TotalQuantitySold)
            .Take(count)
            .ToList();
    }

    private static string InkLabel(InkCoverageLevel level) => level switch
    {
        Domain.Entities.InkCoverageLevel.Low => "منخفض",
        Domain.Entities.InkCoverageLevel.Medium => "متوسط",
        Domain.Entities.InkCoverageLevel.High => "عالي",
        _ => level.ToString()
    };
}
