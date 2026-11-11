using Microsoft.EntityFrameworkCore;
using SmartStart.Application.DTOs;
using SmartStart.Application.Interfaces;
using SmartStart.Domain.Entities;

namespace SmartStart.Application.Services;

public class InvoiceService : IInvoiceService
{
    private readonly IApplicationDbContext _context;

    public InvoiceService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<InvoiceResponseDto> CreateInvoiceAsync(CreateInvoiceDto dto)
    {
        if (dto.Items == null || dto.Items.Count == 0)
            throw new Exception("لا يمكن إنشاء فاتورة فارغة");

        var cashier = await _context.Users.FindAsync(dto.CashierId)
            ?? throw new Exception("الكاشير غير موجود");

        if (!cashier.IsActive)
            throw new Exception("حساب الكاشير غير نشط");

        var productIds = dto.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _context.Products
            .Where(p => productIds.Contains(p.Id) && p.IsActive)
            .ToDictionaryAsync(p => p.Id);

        var invoice = new Invoice
        {
            InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(100, 999)}",
            QrCode = await GenerateInvoiceQrAsync(),
            CashierId = dto.CashierId,
            CreatedAt = DateTime.UtcNow,
            Items = new List<InvoiceItem>()
        };

        foreach (var group in dto.Items.GroupBy(i => i.ProductId))
        {
            var quantity = group.Sum(i => i.Quantity);
            if (quantity <= 0)
                throw new Exception("الكمية يجب أن تكون أكبر من صفر");

            if (!products.TryGetValue(group.Key, out var product))
                throw new Exception($"المنتج رقم {group.Key} غير موجود");

            if (product.RemainingQuantity < quantity)
                throw new Exception($"الكمية المتاحة من المنتج '{product.Name}' هي {product.RemainingQuantity} فقط");

            product.RemainingQuantity -= quantity;

            invoice.Items.Add(new InvoiceItem
            {
                ProductId = product.Id,
                Quantity = quantity,
                UnitWholesalePrice = product.WholesalePrice,
                UnitSellingPrice = product.SellingPrice
            });
        }

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();

        var created = await GetInvoiceByIdAsync(invoice.Id)
            ?? throw new Exception("خطأ أثناء استرجاع الفاتورة");

        var entity = await _context.Invoices.FindAsync(invoice.Id);
        if (entity != null)
        {
            entity.TotalAmount = created.TotalAmount;
            await _context.SaveChangesAsync();
        }

        return created;
    }

    public async Task<InvoiceResponseDto?> GetInvoiceByIdAsync(int id)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Items)
            .ThenInclude(item => item.Product)
            .Include(i => i.Items)
            .ThenInclude(item => item.Returns)
            .FirstOrDefaultAsync(i => i.Id == id);

        return invoice != null ? MapToDto(invoice) : null;
    }

    public async Task<InvoiceResponseDto> GetInvoiceByScanAsync(string code)
    {
        var token = (code ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(token))
            throw new Exception("امسح رمز الفاتورة");

        var invoice = await _context.Invoices
            .Include(i => i.Items)
            .ThenInclude(item => item.Product)
            .Include(i => i.Items)
            .ThenInclude(item => item.Returns)
            .FirstOrDefaultAsync(i =>
                i.QrCode.ToLower() == token.ToLower()
                || i.InvoiceNumber.ToLower() == token.ToLower());

        if (invoice == null)
            throw new Exception("الفاتورة غير موجودة");

        if (invoice.CreatedAt < DateTime.UtcNow.AddHours(-24))
            throw new Exception("مرّ على الفاتورة أكثر من 24 ساعة");

        return MapToDto(invoice);
    }

    public async Task<IEnumerable<InvoiceResponseDto>> GetRecentInvoicesAsync(int count = 20)
    {
        var invoices = await _context.Invoices
            .Include(i => i.Items)
            .ThenInclude(item => item.Product)
            .Include(i => i.Items)
            .ThenInclude(item => item.Returns)
            .OrderByDescending(i => i.CreatedAt)
            .Take(count)
            .ToListAsync();

        return invoices.Select(MapToDto);
    }

    public async Task<IEnumerable<InvoiceResponseDto>> GetInvoicesSinceAsync(DateTime sinceUtc, int? cashierId = null)
    {
        var since = DateTime.SpecifyKind(sinceUtc, DateTimeKind.Utc);
        var query = _context.Invoices
            .Include(i => i.Items)
            .ThenInclude(item => item.Product)
            .Include(i => i.Items)
            .ThenInclude(item => item.Returns)
            .Where(i => i.CreatedAt >= since);

        if (cashierId is int id)
            query = query.Where(i => i.CashierId == id);

        var invoices = await query
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();

        return invoices.Select(MapToDto);
    }

    private static InvoiceResponseDto MapToDto(Invoice invoice)
    {
        var itemDtos = invoice.Items.Select(item => new InvoiceItemResponseDto(
            item.Id,
            item.ProductId,
            item.Product?.Name ?? "منتج غير معروف",
            item.Product?.Barcode ?? "",
            item.Quantity,
            item.UnitWholesalePrice,
            item.UnitSellingPrice,
            item.LineTotal,
            item.LineProfit,
            item.Quantity - item.Returns.Sum(r => r.QuantityReturned)
        )).ToList();

        return new InvoiceResponseDto(
            invoice.Id,
            invoice.InvoiceNumber,
            invoice.QrCode,
            invoice.CashierId,
            itemDtos.Sum(i => i.LineTotal),
            itemDtos.Sum(i => i.LineProfit),
            invoice.CreatedAt,
            itemDtos
        );
    }

    private async Task<string> GenerateInvoiceQrAsync()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        for (var attempt = 0; attempt < 40; attempt++)
        {
            var chars = new char[10];
            for (var i = 0; i < chars.Length; i++)
                chars[i] = alphabet[Random.Shared.Next(alphabet.Length)];
            var code = "SSINV-" + new string(chars);
            if (!await _context.Invoices.AnyAsync(i => i.QrCode == code))
                return code;
        }

        throw new Exception("تعذر توليد رمز الفاتورة");
    }
}
