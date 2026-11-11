namespace SmartStart.Application.DTOs;

public record CreateReturnDto(
    int InvoiceItemId,
    int QuantityReturned,
    int? ProcessedByUserId
);

public record CreateReturnByQueryDto(
    string Query,
    int QuantityReturned,
    int? ProcessedByUserId
);

public record ReturnablePreviewDto(
    int InvoiceItemId,
    string InvoiceNumber,
    int ProductId,
    string ProductName,
    string Barcode,
    int AvailableQuantity,
    decimal UnitSellingPrice,
    DateTime SoldAt
);

public record ReturnDetailsDto(
    int Id,
    int InvoiceItemId,
    string InvoiceNumber,
    int ProductId,
    string ProductName,
    string Barcode,
    int QuantityReturned,
    decimal UnitSellingPrice,
    decimal RefundAmount,
    string? ProcessedByName,
    DateTime ReturnedAt,
    DateTime SoldAt,
    string SoldByName
);
