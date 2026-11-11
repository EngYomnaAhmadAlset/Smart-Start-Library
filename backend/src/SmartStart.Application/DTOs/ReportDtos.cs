namespace SmartStart.Application.DTOs;

public record ProductProfitLineDto(
    int ProductId,
    string ProductName,
    string Barcode,
    int QuantitySold,
    decimal TotalRevenue,
    decimal TotalProfit
);

public record ProfitReportDto(
    decimal TotalSales,
    decimal TotalCost,
    decimal NetProfit,
    int TotalInvoicesCount,
    DateTime StartDate,
    DateTime EndDate,
    IReadOnlyList<ProductProfitLineDto> Products
);

public record TopSellingProductDto(
    int ProductId,
    string ProductName,
    string Barcode,
    int TotalQuantitySold,
    decimal TotalRevenue
);
