using Microsoft.EntityFrameworkCore;
using SmartStart.Application.DTOs;
using SmartStart.Application.Interfaces;
using SmartStart.Domain.Entities;

namespace SmartStart.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IApplicationDbContext _context;

    public DashboardService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardDto> GetAsync()
    {
        var now = DateTime.UtcNow;
        var todayStart = DateTime.SpecifyKind(now.Date, DateTimeKind.Utc);
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var products = _context.Products.Where(p => p.IsActive);
        var productsCount = await products.CountAsync();
        var lowStockQuery = products.Where(p => p.RemainingQuantity <= p.MinStockThreshold);
        var lowStockCount = await lowStockQuery.CountAsync();
        var remainingUnits = await products.SumAsync(p => (int?)p.RemainingQuantity) ?? 0;
        var wholesaleValue = await products.SumAsync(p => (decimal?)(p.RemainingQuantity * p.WholesalePrice)) ?? 0;
        var sellingValue = await products.SumAsync(p => (decimal?)(p.RemainingQuantity * p.SellingPrice)) ?? 0;
        var paperWholesale = await _context.PhotocopyPaperTypes
            .SumAsync(p => (decimal?)(p.RemainingQuantity * p.WholesalePrice)) ?? 0;
        var paperSelling = await _context.PhotocopyPaperTypes
            .SumAsync(p => (decimal?)(p.RemainingQuantity * p.SellingPrice)) ?? 0;
        wholesaleValue += paperWholesale;
        sellingValue += paperSelling;

        var invoicesCount = await _context.Invoices.CountAsync()
            + await _context.PhotocopySales.CountAsync();
        var invoicesTodayCount = await _context.Invoices.CountAsync(i => i.CreatedAt >= todayStart)
            + await _context.PhotocopySales.CountAsync(i => i.CreatedAt >= todayStart);
        var salesToday = await NetSalesSinceAsync(todayStart);
        var salesMonth = await NetSalesSinceAsync(monthStart);
        var profitToday = await NetProfitSinceAsync(todayStart);
        var profitMonth = await NetProfitSinceAsync(monthStart);

        var returnsCount = await _context.Returns.CountAsync();
        var cashiers = _context.Users.Where(u => u.Role == UserRole.Cashier);
        var cashiersCount = await cashiers.CountAsync();
        var activeCashiers = await cashiers.CountAsync(u => u.IsActive);
        var restockCount = await _context.RestockEvents.CountAsync();

        var lowStock = await lowStockQuery
            .OrderBy(p => p.RemainingQuantity)
            .ThenBy(p => p.Name)
            .Take(8)
            .Select(p => new DashboardLowStockDto(
                p.Id,
                p.Name,
                p.Barcode,
                p.RemainingQuantity,
                p.MinStockThreshold))
            .ToListAsync();

        var recentRows = await _context.Invoices
            .OrderByDescending(i => i.CreatedAt)
            .Take(8)
            .Select(i => new
            {
                i.Id,
                i.InvoiceNumber,
                CashierName = i.Cashier.Username,
                i.CreatedAt,
                NetAmount = i.Items.Sum(it =>
                    (it.Quantity - it.Returns.Sum(r => r.QuantityReturned)) * it.UnitSellingPrice)
            })
            .ToListAsync();

        var recent = recentRows
            .Select(i => new DashboardInvoiceDto(
                i.Id,
                i.InvoiceNumber,
                i.CashierName,
                Math.Round(i.NetAmount, 2),
                i.CreatedAt))
            .ToList();

        var monthItems = await _context.InvoiceItems
            .Where(i => i.Invoice.CreatedAt >= monthStart)
            .Select(i => new
            {
                i.ProductId,
                Name = i.Product.Name,
                Barcode = i.Product.Barcode,
                NetQty = i.Quantity - i.Returns.Sum(r => r.QuantityReturned),
                UnitSellingPrice = i.UnitSellingPrice
            })
            .ToListAsync();

        var topSelling = monthItems
            .GroupBy(i => new { i.ProductId, i.Name, i.Barcode })
            .Select(g => new DashboardTopProductDto(
                g.Key.ProductId,
                g.Key.Name,
                g.Key.Barcode,
                g.Sum(x => x.NetQty),
                Math.Round(g.Sum(x => x.NetQty * x.UnitSellingPrice), 2)))
            .Where(x => x.QuantitySold > 0)
            .OrderByDescending(x => x.QuantitySold)
            .Take(5)
            .ToList();

        return new DashboardDto(
            productsCount,
            lowStockCount,
            remainingUnits,
            Math.Round(wholesaleValue, 2),
            Math.Round(sellingValue, 2),
            invoicesCount,
            invoicesTodayCount,
            Math.Round(salesToday, 2),
            Math.Round(profitToday, 2),
            Math.Round(salesMonth, 2),
            Math.Round(profitMonth, 2),
            returnsCount,
            cashiersCount,
            activeCashiers,
            restockCount,
            lowStock,
            recent,
            topSelling
        );
    }

    private async Task<decimal> NetSalesSinceAsync(DateTime sinceUtc)
    {
        var gross = await _context.Invoices
            .Where(i => i.CreatedAt >= sinceUtc)
            .SumAsync(i => (decimal?)i.TotalAmount) ?? 0;
        var copySales = await _context.PhotocopySales
            .Where(s => s.CreatedAt >= sinceUtc)
            .SumAsync(s => (decimal?)s.TotalAmount) ?? 0;
        var refunded = await _context.Returns
            .Where(r => r.InvoiceItem.Invoice.CreatedAt >= sinceUtc)
            .SumAsync(r => (decimal?)(r.QuantityReturned * r.InvoiceItem.UnitSellingPrice)) ?? 0;
        return gross + copySales - refunded;
    }

    private async Task<decimal> NetProfitSinceAsync(DateTime sinceUtc)
    {
        var gross = await _context.InvoiceItems
            .Where(i => i.Invoice.CreatedAt >= sinceUtc)
            .SumAsync(i => (decimal?)i.LineProfit) ?? 0;
        var copyProfit = await _context.PhotocopySales
            .Where(s => s.CreatedAt >= sinceUtc)
            .SumAsync(s => (decimal?)s.ProfitAmount) ?? 0;
        var reversed = await _context.Returns
            .Where(r => r.InvoiceItem.Invoice.CreatedAt >= sinceUtc)
            .SumAsync(r => (decimal?)(r.QuantityReturned *
                (r.InvoiceItem.UnitSellingPrice - r.InvoiceItem.UnitWholesalePrice))) ?? 0;
        return gross + copyProfit - reversed;
    }
}
