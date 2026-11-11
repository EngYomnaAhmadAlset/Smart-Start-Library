namespace SmartStart.Application.DTOs;

public record DashboardLowStockDto(
    int Id,
    string Name,
    string Barcode,
    int RemainingQuantity,
    int MinStockThreshold
);

public record DashboardInvoiceDto(
    int Id,
    string InvoiceNumber,
    string CashierName,
    decimal TotalAmount,
    DateTime CreatedAt
);

public record DashboardTopProductDto(
    int ProductId,
    string ProductName,
    string Barcode,
    int QuantitySold,
    decimal Revenue
);

public record DashboardDto(
    int ProductsCount,
    int LowStockCount,
    int RemainingUnits,
    decimal InventoryWholesaleValue,
    decimal InventorySellingValue,
    int InvoicesCount,
    int InvoicesTodayCount,
    decimal SalesToday,
    decimal ProfitToday,
    decimal SalesThisMonth,
    decimal ProfitThisMonth,
    int ReturnsCount,
    int CashiersCount,
    int ActiveCashiersCount,
    int RestockEventsCount,
    IReadOnlyList<DashboardLowStockDto> LowStock,
    IReadOnlyList<DashboardInvoiceDto> RecentInvoices,
    IReadOnlyList<DashboardTopProductDto> TopSelling
);
