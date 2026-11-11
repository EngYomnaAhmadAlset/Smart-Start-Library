using Microsoft.EntityFrameworkCore;
using SmartStart.Application.DTOs;
using SmartStart.Application.Interfaces;
using SmartStart.Domain.Entities;

namespace SmartStart.Application.Services;

public class ReturnService : IReturnService
{
    private readonly IApplicationDbContext _context;

    public ReturnService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ReturnDetailsDto> ProcessReturnAsync(CreateReturnDto dto)
    {
        if (dto.QuantityReturned <= 0)
            throw new Exception("كمية الإرجاع يجب أن تكون أكبر من صفر");

        var item = await LoadInvoiceItemAsync(dto.InvoiceItemId)
            ?? throw new Exception("سطر الفاتورة غير موجود");

        EnsureReturnable(item, dto.QuantityReturned);

        var returnItem = new ReturnItem
        {
            InvoiceItemId = item.Id,
            QuantityReturned = dto.QuantityReturned,
            ProcessedByUserId = dto.ProcessedByUserId,
            ReturnedAt = DateTime.UtcNow
        };

        item.Product.RemainingQuantity += dto.QuantityReturned;
        _context.Returns.Add(returnItem);
        await _context.SaveChangesAsync();

        returnItem.InvoiceItem = item;
        if (dto.ProcessedByUserId is int userId)
        {
            returnItem.ProcessedByUser = await _context.Users.FindAsync(userId);
        }

        return MapDetails(returnItem);
    }

    public async Task<ReturnablePreviewDto?> PreviewAsync(string query)
    {
        var product = await FindProductAsync(query);
        if (product == null) return null;

        var item = await FindReturnableItemAsync(product.Id);
        if (item == null) return null;

        var available = AvailableQty(item);
        return new ReturnablePreviewDto(
            item.Id,
            item.Invoice.InvoiceNumber,
            product.Id,
            product.Name,
            product.Barcode,
            available,
            item.UnitSellingPrice,
            item.Invoice.CreatedAt);
    }

    public async Task<ReturnDetailsDto> ProcessByQueryAsync(CreateReturnByQueryDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Query))
            throw new Exception("أدخل اسم المنتج أو الباركود");

        var preview = await PreviewAsync(dto.Query)
            ?? throw new Exception("لا يوجد بيع قابل للإرجاع لهذا المنتج خلال 24 ساعة");

        return await ProcessReturnAsync(new CreateReturnDto(
            preview.InvoiceItemId,
            dto.QuantityReturned,
            dto.ProcessedByUserId));
    }

    public async Task<IReadOnlyList<ReturnDetailsDto>> GetSinceAsync(DateTime sinceUtc)
    {
        var since = DateTime.SpecifyKind(sinceUtc, DateTimeKind.Utc);
        var rows = await _context.Returns
            .Include(r => r.ProcessedByUser)
            .Include(r => r.InvoiceItem)
                .ThenInclude(i => i.Product)
            .Include(r => r.InvoiceItem)
                .ThenInclude(i => i.Invoice)
                    .ThenInclude(inv => inv.Cashier)
            .Where(r => r.ReturnedAt >= since)
            .OrderByDescending(r => r.ReturnedAt)
            .ToListAsync();

        return rows.Select(MapDetails).ToList();
    }

    private async Task<Product?> FindProductAsync(string query)
    {
        var term = query.Trim();
        var byBarcode = await _context.Products
            .FirstOrDefaultAsync(p => p.IsActive && p.Barcode == term);
        if (byBarcode != null) return byBarcode;

        var lower = term.ToLowerInvariant();
        var matches = await _context.Products
            .Where(p => p.IsActive && p.Name.ToLower().Contains(lower))
            .OrderBy(p => p.Name)
            .Take(2)
            .ToListAsync();

        return matches.Count == 1 ? matches[0] : null;
    }

    private async Task<InvoiceItem?> FindReturnableItemAsync(int productId)
    {
        var since = DateTime.UtcNow.AddHours(-24);
        var items = await _context.InvoiceItems
            .Include(i => i.Product)
            .Include(i => i.Returns)
            .Include(i => i.Invoice)
            .Where(i => i.ProductId == productId && i.Invoice.CreatedAt >= since)
            .OrderByDescending(i => i.Invoice.CreatedAt)
            .ToListAsync();

        return items.FirstOrDefault(i => AvailableQty(i) > 0);
    }

    private async Task<InvoiceItem?> LoadInvoiceItemAsync(int invoiceItemId)
    {
        return await _context.InvoiceItems
            .Include(i => i.Product)
            .Include(i => i.Returns)
            .Include(i => i.Invoice)
                .ThenInclude(inv => inv.Cashier)
            .FirstOrDefaultAsync(i => i.Id == invoiceItemId);
    }

    private static int AvailableQty(InvoiceItem item) =>
        item.Quantity - item.Returns.Sum(r => r.QuantityReturned);

    private static void EnsureReturnable(InvoiceItem item, int qty)
    {
        var available = AvailableQty(item);
        if (qty > available)
            throw new Exception(available <= 0
                ? "لا توجد كمية قابلة للإرجاع من هذا المنتج"
                : $"يمكن إرجاع {available} فقط من هذا السطر");
    }

    private static ReturnDetailsDto MapDetails(ReturnItem r)
    {
        var item = r.InvoiceItem;
        var refund = r.QuantityReturned * item.UnitSellingPrice;
        return new ReturnDetailsDto(
            r.Id,
            item.Id,
            item.Invoice.InvoiceNumber,
            item.ProductId,
            item.Product.Name,
            item.Product.Barcode,
            r.QuantityReturned,
            item.UnitSellingPrice,
            Math.Round(refund, 2),
            r.ProcessedByUser?.Username,
            r.ReturnedAt,
            item.Invoice.CreatedAt,
            item.Invoice.Cashier.Username);
    }
}
