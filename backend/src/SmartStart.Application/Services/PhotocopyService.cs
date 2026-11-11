using Microsoft.EntityFrameworkCore;
using SmartStart.Application.DTOs;
using SmartStart.Application.Interfaces;
using SmartStart.Domain.Entities;

namespace SmartStart.Application.Services;

public class PhotocopyService : IPhotocopyService
{
    private const string UnprofitableMessage = "هذا السعر لن يحقق ربحاً";
    private const string AskAdmin = "راجعي الأدمن";

    private readonly IApplicationDbContext _context;

    public PhotocopyService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PhotocopySettingsDto> GetSettingsAsync()
    {
        var settings = await EnsureSettingsAsync();
        var papers = await _context.PhotocopyPaperTypes
            .OrderBy(p => p.Name)
            .ToListAsync();
        var ink = (await _context.PhotocopyInkLevels.ToListAsync())
            .OrderBy(i => i.Level)
            .ToList();
        var cashiers = await _context.Users
            .Where(u => u.Role == UserRole.Cashier)
            .OrderBy(u => u.Username)
            .ToListAsync();

        return new PhotocopySettingsDto(
            settings.MaintenanceCost,
            settings.ElectricityCost,
            papers.Select(MapPaper).ToList(),
            ink.Select(MapInk).ToList(),
            cashiers.Select(u => new PhotocopyCashierAccessDto(
                u.Id, u.Username, u.IsActive, u.CanAccessPhotocopy)).ToList()
        );
    }

    public async Task<PhotocopySettingsDto> UpdateOverheadAsync(UpdatePhotocopyOverheadDto dto)
    {
        if (dto.MaintenanceCost < 0 || dto.ElectricityCost < 0)
            throw new Exception("قيمة الصيانة والكهرباء لا يمكن أن تكون سالبة");

        var settings = await EnsureSettingsAsync();
        settings.MaintenanceCost = dto.MaintenanceCost;
        settings.ElectricityCost = dto.ElectricityCost;
        await _context.SaveChangesAsync();
        return await GetSettingsAsync();
    }

    public async Task<PhotocopyPaperDto> CreatePaperAsync(CreatePhotocopyPaperDto dto)
    {
        ValidateMoney(dto.WholesalePrice, dto.SellingPrice, dto.ConfirmUnprofitable);
        if (dto.Quantity <= 0)
            throw new Exception("الكمية يجب أن تكون أكبر من صفر");

        var name = RequireName(dto.Name);
        if (await _context.PhotocopyPaperTypes.AnyAsync(p => p.Name.ToLower() == name.ToLower()))
            throw new Exception("نوع الورق موجود مسبقاً");

        var paper = new PhotocopyPaperType
        {
            Name = name,
            WholesalePrice = dto.WholesalePrice,
            SellingPrice = dto.SellingPrice,
            TotalReceivedQuantity = dto.Quantity,
            RemainingQuantity = dto.Quantity,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.PhotocopyPaperTypes.Add(paper);
        await _context.SaveChangesAsync();
        return MapPaper(paper);
    }

    public async Task<PhotocopyPaperDto> UpdatePaperAsync(int id, UpdatePhotocopyPaperDto dto)
    {
        ValidateMoney(dto.WholesalePrice, dto.SellingPrice, dto.ConfirmUnprofitable);
        var paper = await _context.PhotocopyPaperTypes.FindAsync(id)
            ?? throw new Exception("نوع الورق غير موجود");

        var name = RequireName(dto.Name);
        var taken = await _context.PhotocopyPaperTypes
            .AnyAsync(p => p.Id != id && p.Name.ToLower() == name.ToLower());
        if (taken)
            throw new Exception("نوع الورق موجود مسبقاً");

        paper.Name = name;
        paper.WholesalePrice = dto.WholesalePrice;
        paper.SellingPrice = dto.SellingPrice;
        paper.IsActive = dto.IsActive;
        await _context.SaveChangesAsync();
        return MapPaper(paper);
    }

    public async Task<PhotocopyPaperDto> RestockPaperAsync(int id, RestockPhotocopyPaperDto dto)
    {
        if (dto.Quantity <= 0)
            throw new Exception("الكمية يجب أن تكون أكبر من صفر");

        var paper = await _context.PhotocopyPaperTypes.FindAsync(id)
            ?? throw new Exception("نوع الورق غير موجود");

        paper.TotalReceivedQuantity += dto.Quantity;
        paper.RemainingQuantity += dto.Quantity;
        await _context.SaveChangesAsync();
        return MapPaper(paper);
    }

    public async Task<PhotocopySettingsDto> UpdateInkAsync(UpdatePhotocopyInkDto dto)
    {
        if (dto.Levels == null || dto.Levels.Count != 3)
            throw new Exception("يجب إدخال المستويات الثلاثة للحبر");

        var rows = await _context.PhotocopyInkLevels.ToListAsync();
        foreach (var item in dto.Levels)
        {
            var level = ParseInk(item.Level);
            ValidateMoney(item.WholesalePerPage, item.SellingPerPage, item.ConfirmUnprofitable);
            if (item.RemainingPages < 0)
                throw new Exception("رصيد صفحات الحبر لا يمكن أن يكون سالباً");

            var row = rows.FirstOrDefault(r => r.Level == level)
                ?? throw new Exception("مستوى الحبر غير موجود");
            row.WholesalePerPage = item.WholesalePerPage;
            row.SellingPerPage = item.SellingPerPage;
            row.RemainingPages = item.RemainingPages;
        }

        await _context.SaveChangesAsync();
        return await GetSettingsAsync();
    }

    public async Task SetCashierAccessAsync(int cashierId, bool canAccess)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == cashierId && u.Role == UserRole.Cashier)
            ?? throw new Exception("الكاشير غير موجود");
        user.CanAccessPhotocopy = canAccess;
        await _context.SaveChangesAsync();
    }

    public async Task<PhotocopyCatalogDto> GetCatalogAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId)
            ?? throw new Exception("المستخدم غير موجود");

        var canAccess = user.Role == UserRole.Admin || (user.Role == UserRole.Cashier && user.CanAccessPhotocopy);
        var settings = await EnsureSettingsAsync();
        var papers = await _context.PhotocopyPaperTypes
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync();
        // Level is persisted as text, so sorting must happen in memory to follow Low → Medium → High
        var ink = (await _context.PhotocopyInkLevels.ToListAsync())
            .OrderBy(i => i.Level)
            .ToList();

        var inkReady = ink.Count == 3 && ink.All(i => i.WholesalePerPage > 0 && i.SellingPerPage > 0);
        string? message = null;
        if (!canAccess)
            message = $"حسابك غير مفعّل لصفحة التصوير. {AskAdmin}";
        else if (papers.Count == 0)
            message = $"لا يوجد نوع ورق مفعّل. {AskAdmin}";
        else if (!inkReady)
            message = $"إعدادات الحبر غير مكتملة. {AskAdmin}";

        return new PhotocopyCatalogDto(
            canAccess,
            canAccess && papers.Count > 0 && inkReady,
            message,
            settings.MaintenanceCost,
            settings.ElectricityCost,
            papers.Select(MapPaper).ToList(),
            ink.Select(MapInk).ToList()
        );
    }

    public async Task<PhotocopySaleDto> CreateSaleAsync(int cashierId, CreatePhotocopySaleDto dto)
    {
        if (dto.PageCount <= 0)
            throw new Exception("عدد الصفحات يجب أن يكون أكبر من صفر");

        var cashier = await _context.Users.FindAsync(cashierId)
            ?? throw new Exception("الكاشير غير موجود");
        if (!cashier.IsActive)
            throw new Exception("حساب الكاشير غير نشط");
        if (cashier.Role != UserRole.Cashier || !cashier.CanAccessPhotocopy)
            throw new Exception($"حسابك غير مفعّل لصفحة التصوير. {AskAdmin}");

        var paper = await _context.PhotocopyPaperTypes.FirstOrDefaultAsync(p => p.Id == dto.PaperTypeId && p.IsActive)
            ?? throw new Exception($"لا يوجد نوع ورق مفعّل. {AskAdmin}");

        if (paper.RemainingQuantity < dto.PageCount)
            throw new Exception($"رصيد الورق المتبقي أقل من المطلوب ({paper.RemainingQuantity} ورقة)");

        var inkLevel = ParseInk(dto.InkLevel);
        var ink = await _context.PhotocopyInkLevels.FirstOrDefaultAsync(i => i.Level == inkLevel)
            ?? throw new Exception($"إعدادات الحبر غير مكتملة. {AskAdmin}");
        if (ink.WholesalePerPage <= 0 || ink.SellingPerPage <= 0)
            throw new Exception($"إعدادات الحبر غير مكتملة. {AskAdmin}");

        var settings = await EnsureSettingsAsync();
        var pages = dto.PageCount;
        var total = pages * paper.SellingPrice + pages * ink.SellingPerPage
                    + settings.MaintenanceCost + settings.ElectricityCost;
        var cost = pages * paper.WholesalePrice + pages * ink.WholesalePerPage
                   + settings.MaintenanceCost + settings.ElectricityCost;
        var profit = total - cost;
        var percent = cost == 0 ? 0 : Math.Round(profit / cost * 100m, 2);

        paper.RemainingQuantity -= pages;
        if (ink.RemainingPages > 0)
            ink.RemainingPages = Math.Max(0, ink.RemainingPages - pages);

        var sale = new PhotocopySale
        {
            SaleNumber = $"CPY-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(100, 999)}",
            CashierId = cashier.Id,
            PaperTypeId = paper.Id,
            PaperName = paper.Name,
            InkLevel = inkLevel,
            PageCount = pages,
            PaperWholesalePrice = paper.WholesalePrice,
            PaperSellingPrice = paper.SellingPrice,
            InkWholesalePerPage = ink.WholesalePerPage,
            InkSellingPerPage = ink.SellingPerPage,
            MaintenanceCost = settings.MaintenanceCost,
            ElectricityCost = settings.ElectricityCost,
            TotalAmount = Math.Round(total, 2),
            CostAmount = Math.Round(cost, 2),
            ProfitAmount = Math.Round(profit, 2),
            ProfitPercent = percent,
            CreatedAt = DateTime.UtcNow
        };

        _context.PhotocopySales.Add(sale);
        await _context.SaveChangesAsync();
        return MapSale(sale);
    }

    private async Task<PhotocopySettings> EnsureSettingsAsync()
    {
        var settings = await _context.PhotocopySettings.OrderBy(s => s.Id).FirstOrDefaultAsync();
        if (settings != null)
            return settings;

        settings = new PhotocopySettings { MaintenanceCost = 10m, ElectricityCost = 20m };
        _context.PhotocopySettings.Add(settings);
        await SeedInkIfMissingAsync();
        await _context.SaveChangesAsync();
        return settings;
    }

    private async Task SeedInkIfMissingAsync()
    {
        if (await _context.PhotocopyInkLevels.AnyAsync())
            return;
        _context.PhotocopyInkLevels.AddRange(
            new PhotocopyInkLevel { Level = InkCoverageLevel.Low },
            new PhotocopyInkLevel { Level = InkCoverageLevel.Medium },
            new PhotocopyInkLevel { Level = InkCoverageLevel.High }
        );
    }

    private static void ValidateMoney(decimal wholesale, decimal selling, bool confirmUnprofitable)
    {
        if (wholesale <= 0)
            throw new Exception("سعر الجملة يجب أن يكون أكبر من صفر");
        if (selling <= 0)
            throw new Exception("سعر البيع يجب أن يكون أكبر من صفر");
        if (selling <= wholesale && !confirmUnprofitable)
            throw new Exception(UnprofitableMessage);
    }

    private static string RequireName(string? name)
    {
        var trimmed = (name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            throw new Exception("الاسم مطلوب");
        return trimmed;
    }

    private static InkCoverageLevel ParseInk(string? value)
    {
        if (Enum.TryParse<InkCoverageLevel>(value, true, out var level)
            && Enum.IsDefined(level))
            return level;
        throw new Exception("مستوى الحبر غير صالح");
    }

    private static PhotocopyPaperDto MapPaper(PhotocopyPaperType p) =>
        new(p.Id, p.Name, p.WholesalePrice, p.SellingPrice, p.TotalReceivedQuantity, p.RemainingQuantity, p.IsActive, p.CreatedAt);

    private static PhotocopyInkDto MapInk(PhotocopyInkLevel i) =>
        new(i.Level.ToString(), i.WholesalePerPage, i.SellingPerPage, i.RemainingPages);

    private static PhotocopySaleDto MapSale(PhotocopySale s) =>
        new(
            s.Id,
            s.SaleNumber,
            s.CashierId,
            s.PaperTypeId,
            s.PaperName,
            s.InkLevel.ToString(),
            s.PageCount,
            s.PaperSellingPrice,
            s.InkSellingPerPage,
            s.MaintenanceCost,
            s.ElectricityCost,
            s.TotalAmount,
            s.CostAmount,
            s.ProfitAmount,
            s.ProfitPercent,
            s.CreatedAt
        );
}
