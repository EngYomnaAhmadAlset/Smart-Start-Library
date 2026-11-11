using SmartStart.Application.DTOs;

namespace SmartStart.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginRequestDto dto);
    Task ChangePasswordAsync(int userId, ChangePasswordDto dto);
    Task<LoginResponseDto> ChangeUsernameAsync(int userId, ChangeUsernameDto dto);
}

public interface IUserService
{
    Task<IEnumerable<UserResponseDto>> GetCashiersAsync();
    Task<UserResponseDto> CreateCashierAsync(CreateCashierDto dto);
    Task<UserResponseDto> UpdateCashierAsync(int id, UpdateCashierDto dto);
    Task<UserResponseDto?> GetByIdAsync(int id);
}

public interface IReturnService
{
    Task<ReturnDetailsDto> ProcessReturnAsync(CreateReturnDto dto);
    Task<ReturnDetailsDto> ProcessByQueryAsync(CreateReturnByQueryDto dto);
    Task<ReturnablePreviewDto?> PreviewAsync(string query);
    Task<IReadOnlyList<ReturnDetailsDto>> GetSinceAsync(DateTime sinceUtc);
}

public interface IProductService
{
    Task<ProductResponseDto> CreateProductAsync(CreateProductDto dto);
    Task<ProductResponseDto> UpdateProductAsync(int id, UpdateProductDto dto);
    Task DeleteProductAsync(int id);
    Task<ProductResponseDto> RestockProductAsync(RestockProductDto dto);
    Task<ProductResponseDto?> GetByBarcodeAsync(string barcode);
    Task<ProductResponseDto?> GetByIdAsync(int id);
    Task<IEnumerable<ProductResponseDto>> SearchProductsAsync(string? searchTerm);
    Task<IEnumerable<ProductResponseDto>> GetLowStockProductsAsync();
    Task<ProductHistoryDto?> GetHistoryAsync(int productId);
    Task<IReadOnlyList<string>> GenerateBarcodesAsync(int count);
    Task<ImportProductsResultDto> ImportProductsAsync(IReadOnlyList<ImportProductRowDto> rows);
    Task<ChangeBarcodeResultDto> ChangeBarcodeAsync(int productId, ChangeBarcodeDto dto, int? changedByUserId);
    Task<IReadOnlyList<BarcodeChangeLogDto>> GetBarcodeChangesAsync(int productId);
    decimal SuggestSellingPrice(decimal wholesalePrice, decimal percent);
}

public interface IInvoiceService
{
    Task<InvoiceResponseDto> CreateInvoiceAsync(CreateInvoiceDto dto);
    Task<InvoiceResponseDto?> GetInvoiceByIdAsync(int id);
    Task<InvoiceResponseDto> GetInvoiceByScanAsync(string code);
    Task<IEnumerable<InvoiceResponseDto>> GetRecentInvoicesAsync(int count = 20);
    Task<IEnumerable<InvoiceResponseDto>> GetInvoicesSinceAsync(DateTime sinceUtc, int? cashierId = null);
}

public interface IReportService
{
    Task<ProfitReportDto> GetProfitReportAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<TopSellingProductDto>> GetTopSellingProductsAsync(int count = 10, DateTime? startDate = null, DateTime? endDate = null);
}

public interface IDashboardService
{
    Task<DashboardDto> GetAsync();
}

public interface IPhotocopyService
{
    Task<PhotocopySettingsDto> GetSettingsAsync();
    Task<PhotocopySettingsDto> UpdateOverheadAsync(UpdatePhotocopyOverheadDto dto);
    Task<PhotocopyPaperDto> CreatePaperAsync(CreatePhotocopyPaperDto dto);
    Task<PhotocopyPaperDto> UpdatePaperAsync(int id, UpdatePhotocopyPaperDto dto);
    Task<PhotocopyPaperDto> RestockPaperAsync(int id, RestockPhotocopyPaperDto dto);
    Task<PhotocopySettingsDto> UpdateInkAsync(UpdatePhotocopyInkDto dto);
    Task SetCashierAccessAsync(int cashierId, bool canAccess);
    Task<PhotocopyCatalogDto> GetCatalogAsync(int userId);
    Task<PhotocopySaleDto> CreateSaleAsync(int cashierId, CreatePhotocopySaleDto dto);
}
