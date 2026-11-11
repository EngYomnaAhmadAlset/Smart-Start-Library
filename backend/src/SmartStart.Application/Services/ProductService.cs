using Microsoft.EntityFrameworkCore;
using SmartStart.Application.DTOs;
using SmartStart.Application.Interfaces;
using SmartStart.Domain.Entities;

namespace SmartStart.Application.Services;

public class ProductService : IProductService
{
    private readonly IApplicationDbContext _context;

    public ProductService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ProductResponseDto> CreateProductAsync(CreateProductDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new Exception("اسم المنتج مطلوب");

        if (dto.WholesalePrice < 0)
            throw new Exception("سعر الجملة لا يمكن أن يكون سالباً");

        if (dto.SellingPrice <= 0)
            throw new Exception("سعر البيع يجب أن يكون أكبر من صفر");

        if (dto.InitialQuantity < 0)
            throw new Exception("الكمية لا يمكن أن تكون سالبة");

        var barcode = string.IsNullOrWhiteSpace(dto.Barcode)
            ? (await GenerateBarcodesAsync(1))[0]
            : dto.Barcode.Trim();

        if (await _context.Products.AnyAsync(p => p.Barcode == barcode))
            throw new Exception("الباركود مستخدم مسبقاً — امسحي باركود مختلف أو استخدمي التوليد التلقائي");

        var product = new Product
        {
            Name = dto.Name.Trim(),
            Barcode = barcode,
            WholesalePrice = dto.WholesalePrice,
            SellingPrice = dto.SellingPrice,
            TotalReceivedQuantity = dto.InitialQuantity,
            RemainingQuantity = dto.InitialQuantity,
            MinStockThreshold = dto.MinStockThreshold <= 0 ? 5 : dto.MinStockThreshold,
            CreatedAt = DateTime.UtcNow
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        if (dto.InitialQuantity > 0)
        {
            _context.RestockEvents.Add(new RestockEvent
            {
                ProductId = product.Id,
                QuantityAdded = dto.InitialQuantity,
                PreviousWholesalePrice = dto.WholesalePrice,
                EnteredWholesalePrice = dto.WholesalePrice,
                ResultingWholesalePrice = dto.WholesalePrice,
                RestockedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }

        return MapToDto(product);
    }

    public async Task<ImportProductsResultDto> ImportProductsAsync(IReadOnlyList<ImportProductRowDto> rows)
    {
        if (rows == null || rows.Count == 0)
            throw new Exception("لا صفوف صالحة للاستيراد");

        var valid = new List<ImportProductRowDto>(rows.Count);
        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.Name))
                continue;
            if (row.WholesalePrice < 0 || row.SellingPrice <= 0)
                continue;
            if (row.OriginalQuantity < 0 || row.RemainingQuantity < 0)
                continue;
            if (row.RemainingQuantity > row.OriginalQuantity)
                continue;
            valid.Add(row);
        }

        if (valid.Count == 0)
            throw new Exception("لا صفوف صالحة للاستيراد");

        var lastByName = new Dictionary<string, ImportProductRowDto>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in valid)
            lastByName[NormalizeProductName(row.Name)] = row;

        var existing = await _context.Products.Where(p => p.IsActive).ToListAsync();
        var byName = new Dictionary<string, Product>(StringComparer.OrdinalIgnoreCase);
        foreach (var product in existing)
        {
            var key = NormalizeProductName(product.Name);
            if (!byName.ContainsKey(key))
                byName[key] = product;
        }

        var pendingCreate = new List<ImportProductRowDto>();
        var updated = new List<Product>();

        foreach (var (key, row) in lastByName)
        {
            if (byName.TryGetValue(key, out var product))
            {
                if (!ImportFieldsDiffer(product, row))
                    continue;
                ApplyImportFields(product, row);
                updated.Add(product);
            }
            else
            {
                pendingCreate.Add(row);
            }
        }

        var now = DateTime.UtcNow;
        var created = new List<Product>(pendingCreate.Count);
        if (pendingCreate.Count > 0)
        {
            var barcodes = await GenerateUniqueBarcodesAsync(pendingCreate.Count);
            for (var i = 0; i < pendingCreate.Count; i++)
            {
                var row = pendingCreate[i];
                var product = new Product
                {
                    Name = row.Name.Trim(),
                    Barcode = barcodes[i],
                    WholesalePrice = row.WholesalePrice,
                    SellingPrice = row.SellingPrice,
                    TotalReceivedQuantity = row.OriginalQuantity,
                    RemainingQuantity = row.RemainingQuantity,
                    MinStockThreshold = 5,
                    CreatedAt = now
                };
                _context.Products.Add(product);
                created.Add(product);
            }
        }

        if (created.Count > 0 || updated.Count > 0)
            await _context.SaveChangesAsync();

        foreach (var product in created.Where(p => p.TotalReceivedQuantity > 0))
        {
            _context.RestockEvents.Add(new RestockEvent
            {
                ProductId = product.Id,
                QuantityAdded = product.TotalReceivedQuantity,
                PreviousWholesalePrice = product.WholesalePrice,
                EnteredWholesalePrice = product.WholesalePrice,
                ResultingWholesalePrice = product.WholesalePrice,
                RestockedAt = now
            });
        }

        if (created.Any(p => p.TotalReceivedQuantity > 0))
            await _context.SaveChangesAsync();

        var touched = created.Concat(updated).Select(MapToDto).ToList();
        return new ImportProductsResultDto(
            created.Count + updated.Count,
            created.Count,
            updated.Count,
            touched,
            created.Select(MapToDto).ToList());
    }

    private static string NormalizeProductName(string name) =>
        string.Join(' ', name.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static bool ImportFieldsDiffer(Product product, ImportProductRowDto row) =>
        product.TotalReceivedQuantity != row.OriginalQuantity
        || product.RemainingQuantity != row.RemainingQuantity
        || decimal.Round(product.WholesalePrice, 2) != decimal.Round(row.WholesalePrice, 2)
        || decimal.Round(product.SellingPrice, 2) != decimal.Round(row.SellingPrice, 2);

    private static void ApplyImportFields(Product product, ImportProductRowDto row)
    {
        product.WholesalePrice = row.WholesalePrice;
        product.SellingPrice = row.SellingPrice;
        product.TotalReceivedQuantity = row.OriginalQuantity;
        product.RemainingQuantity = row.RemainingQuantity;
    }

    public async Task<ProductResponseDto> UpdateProductAsync(int id, UpdateProductDto dto)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id && p.IsActive)
            ?? throw new Exception("المنتج غير موجود");

        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new Exception("اسم المنتج مطلوب");

        if (string.IsNullOrWhiteSpace(dto.Barcode))
            throw new Exception("الباركود مطلوب");

        if (dto.WholesalePrice < 0)
            throw new Exception("سعر الجملة لا يمكن أن يكون سالباً");

        if (dto.SellingPrice <= 0)
            throw new Exception("سعر البيع يجب أن يكون أكبر من صفر");

        if (dto.TotalReceivedQuantity < 0)
            throw new Exception("الكمية الأصلية لا يمكن أن تكون سالبة");

        if (dto.RemainingQuantity < 0)
            throw new Exception("الكمية المتبقية لا يمكن أن تكون سالبة");

        if (dto.RemainingQuantity > dto.TotalReceivedQuantity)
            throw new Exception("الكمية المتبقية لا يمكن أن تكون أكبر من الكمية الأصلية");

        var barcode = dto.Barcode.Trim();
        if (await _context.Products.AnyAsync(p => p.Barcode == barcode && p.Id != id))
            throw new Exception("الباركود مستخدم مسبقاً لمنتج آخر");

        product.Name = dto.Name.Trim();
        product.Barcode = barcode;
        product.WholesalePrice = dto.WholesalePrice;
        product.SellingPrice = dto.SellingPrice;
        product.MinStockThreshold = dto.MinStockThreshold < 0 ? 0 : dto.MinStockThreshold;
        product.TotalReceivedQuantity = dto.TotalReceivedQuantity;
        product.RemainingQuantity = dto.RemainingQuantity;

        await _context.SaveChangesAsync();
        return MapToDto(product);
    }

    public async Task DeleteProductAsync(int id)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id && p.IsActive)
            ?? throw new Exception("المنتج غير موجود");

        // Soft delete — keeps invoice history intact
        product.IsActive = false;
        await _context.SaveChangesAsync();
    }

    public async Task<ProductResponseDto> RestockProductAsync(RestockProductDto dto)
    {
        var product = await _context.Products.FindAsync(dto.ProductId)
            ?? throw new Exception("المنتج غير موجود");

        if (dto.QuantityAdded <= 0)
            throw new Exception("الكمية المضافة يجب أن تكون أكبر من صفر");

        if (dto.NewWholesalePrice < 0)
            throw new Exception("سعر الجملة لا يمكن أن يكون سالباً");

        decimal previousPrice = product.WholesalePrice;
        decimal newPrice = dto.NewWholesalePrice;
        // Averaging only makes sense when both prices are known; a zero keeps the other value.
        decimal resultingPrice = (previousPrice > 0 && newPrice > 0 && previousPrice != newPrice)
            ? Math.Round((previousPrice + newPrice) / 2m, 2)
            : (newPrice > 0 ? newPrice : previousPrice);

        _context.RestockEvents.Add(new RestockEvent
        {
            ProductId = product.Id,
            QuantityAdded = dto.QuantityAdded,
            PreviousWholesalePrice = previousPrice,
            EnteredWholesalePrice = newPrice,
            ResultingWholesalePrice = resultingPrice,
            RestockedByUserId = dto.RestockedByUserId,
            RestockedAt = DateTime.UtcNow
        });

        product.WholesalePrice = resultingPrice;
        product.TotalReceivedQuantity += dto.QuantityAdded;
        product.RemainingQuantity += dto.QuantityAdded;

        await _context.SaveChangesAsync();
        return MapToDto(product);
    }

    public async Task<ProductResponseDto?> GetByBarcodeAsync(string barcode)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Barcode == barcode && p.IsActive);

        return product != null ? MapToDto(product) : null;
    }

    public async Task<ProductResponseDto?> GetByIdAsync(int id)
    {
        var product = await _context.Products.FindAsync(id);
        return product != null && product.IsActive ? MapToDto(product) : null;
    }

    public async Task<IEnumerable<ProductResponseDto>> SearchProductsAsync(string? searchTerm)
    {
        var query = _context.Products.Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            var lower = term.ToLowerInvariant();
            query = query.Where(p =>
                p.Name.ToLower().Contains(lower) ||
                p.Barcode.ToLower().Contains(lower) ||
                p.Barcode == term);
        }

        var products = await query.OrderBy(p => p.Name).ToListAsync();
        return products.Select(MapToDto);
    }

    public async Task<ProductHistoryDto?> GetHistoryAsync(int productId)
    {
        var exists = await _context.Products.AnyAsync(p => p.Id == productId && p.IsActive);
        if (!exists) return null;

        var items = await _context.InvoiceItems
            .Include(i => i.Invoice)
                .ThenInclude(inv => inv.Cashier)
            .Include(i => i.Returns)
            .Where(i => i.ProductId == productId)
            .ToListAsync();

        var sales = items
            .OrderByDescending(i => i.Invoice.CreatedAt)
            .Select(i => new ProductSaleHistoryDto(
                i.Id,
                i.Invoice.InvoiceNumber,
                i.Quantity,
                i.UnitSellingPrice,
                i.Invoice.CreatedAt,
                i.Invoice.Cashier.Username,
                i.Returns
                    .OrderByDescending(r => r.ReturnedAt)
                    .Select(r => new ProductReturnHistoryDto(r.QuantityReturned, r.ReturnedAt))
                    .ToList()))
            .ToList();

        return new ProductHistoryDto(productId, sales);
    }

    public async Task<IEnumerable<ProductResponseDto>> GetLowStockProductsAsync()
    {
        var products = await _context.Products
            .Where(p => p.IsActive && p.RemainingQuantity <= p.MinStockThreshold)
            .OrderBy(p => p.RemainingQuantity)
            .ToListAsync();

        return products.Select(MapToDto);
    }

    public async Task<IReadOnlyList<string>> GenerateBarcodesAsync(int count)
    {
        if (count <= 0 || count > 500)
            throw new Exception("عدد الباركودات يجب أن يكون بين 1 و 500");

        return await GenerateUniqueBarcodesAsync(count);
    }

    public async Task<ChangeBarcodeResultDto> ChangeBarcodeAsync(int productId, ChangeBarcodeDto dto, int? changedByUserId)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId)
            ?? throw new Exception("المنتج غير موجود");

        var mode = (dto.Mode ?? string.Empty).Trim().ToLowerInvariant();
        string newBarcode;

        if (mode == "generate")
        {
            newBarcode = (await GenerateUniqueBarcodesAsync(1))[0];
        }
        else if (mode == "scan")
        {
            newBarcode = (dto.Barcode ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(newBarcode))
                throw new Exception("الباركود الجديد مطلوب");

            if (string.Equals(newBarcode, product.Barcode, StringComparison.Ordinal))
                throw new Exception("الباركود الجديد مطابق للحالي");

            var owner = await _context.Products
                .Where(p => p.Id != productId && p.Barcode == newBarcode)
                .Select(p => new { p.Id, p.Name })
                .FirstOrDefaultAsync();

            if (owner is not null)
                throw new Exception($"الباركود مستخدم للمنتج «{owner.Name}»");
        }
        else
        {
            throw new Exception("طريقة التعديل غير صحيحة. استخدم scan أو generate");
        }

        var oldBarcode = product.Barcode;
        product.Barcode = newBarcode;

        _context.BarcodeChangeLogs.Add(new BarcodeChangeLog
        {
            ProductId = product.Id,
            OldBarcode = oldBarcode,
            NewBarcode = newBarcode,
            ChangedByUserId = changedByUserId,
            ChangedAt = DateTime.UtcNow
        });

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            throw new Exception("الباركود مستخدم لمنتج آخر");
        }

        return new ChangeBarcodeResultDto(product.Id, oldBarcode, newBarcode);
    }

    public async Task<IReadOnlyList<BarcodeChangeLogDto>> GetBarcodeChangesAsync(int productId)
    {
        var exists = await _context.Products.AnyAsync(p => p.Id == productId);
        if (!exists)
            throw new Exception("المنتج غير موجود");

        return await _context.BarcodeChangeLogs
            .Where(l => l.ProductId == productId)
            .OrderByDescending(l => l.ChangedAt)
            .Select(l => new BarcodeChangeLogDto(
                l.Id,
                l.OldBarcode,
                l.NewBarcode,
                l.ChangedByUser != null ? l.ChangedByUser.Username : null,
                l.ChangedAt
            ))
            .ToListAsync();
    }

    private async Task<List<string>> GenerateUniqueBarcodesAsync(int count)
    {
        var existing = await _context.Products.Select(p => p.Barcode).ToListAsync();
        var taken = existing.ToHashSet(StringComparer.Ordinal);

        long next = 100001;
        foreach (var code in existing)
        {
            if (code.Length == 6 && long.TryParse(code, out var n) && n >= next)
                next = n + 1;
        }

        var generated = new List<string>(count);
        while (generated.Count < count)
        {
            var code = next < 1000000 ? next.ToString("D6") : next.ToString();
            next++;
            if (taken.Add(code))
                generated.Add(code);
        }

        return generated;
    }

    public decimal SuggestSellingPrice(decimal wholesalePrice, decimal percent)
    {
        if (percent < 25 || percent > 100)
            throw new Exception("النسبة يجب أن تكون بين 25 و 100");

        if (wholesalePrice <= 0)
            throw new Exception("سعر الجملة يجب أن يكون أكبر من صفر");

        // سعر البيع = الجملة + (الجملة × النسبة)
        return Math.Round(wholesalePrice + (wholesalePrice * percent / 100m), 2);
    }

    private static ProductResponseDto MapToDto(Product p) => new(
        p.Id,
        p.Name,
        p.Barcode,
        p.WholesalePrice,
        p.SellingPrice,
        p.ProfitMarginPercent,
        p.TotalReceivedQuantity,
        p.RemainingQuantity,
        p.MinStockThreshold,
        p.RemainingQuantity <= p.MinStockThreshold,
        p.CreatedAt
    );
}
