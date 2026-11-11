namespace SmartStart.Application.DTOs;

public record CreateProductDto(
    string Name,
    string? Barcode,
    decimal WholesalePrice,
    decimal SellingPrice,
    int InitialQuantity,
    int MinStockThreshold
);

public record UpdateProductDto(
    string Name,
    string Barcode,
    decimal WholesalePrice,
    decimal SellingPrice,
    int MinStockThreshold,
    int TotalReceivedQuantity,
    int RemainingQuantity
);

public record RestockProductDto(
    int ProductId,
    int QuantityAdded,
    decimal NewWholesalePrice,
    int? RestockedByUserId
);

public record ImportProductRowDto(
    string Name,
    int OriginalQuantity,
    int RemainingQuantity,
    decimal WholesalePrice,
    decimal SellingPrice
);

public record ChangeBarcodeDto(
    string Mode,
    string? Barcode
);

public record ChangeBarcodeResultDto(
    int ProductId,
    string OldBarcode,
    string NewBarcode
);

public record BarcodeChangeLogDto(
    int Id,
    string OldBarcode,
    string NewBarcode,
    string? ChangedByName,
    DateTime ChangedAt
);

public record ImportProductsResultDto(
    int ImportedCount,
    int CreatedCount,
    int UpdatedCount,
    IReadOnlyList<ProductResponseDto> Products,
    IReadOnlyList<ProductResponseDto> CreatedProducts
);

public record GenerateBarcodesDto(int Count);

public record SuggestPriceDto(decimal WholesalePrice, decimal Percent);

public record ProductResponseDto(
    int Id,
    string Name,
    string Barcode,
    decimal WholesalePrice,
    decimal SellingPrice,
    decimal ProfitMarginPercent,
    int TotalReceivedQuantity,
    int RemainingQuantity,
    int MinStockThreshold,
    bool IsLowStock,
    DateTime CreatedAt
);

public record ProductReturnHistoryDto(
    int QuantityReturned,
    DateTime ReturnedAt
);

public record ProductSaleHistoryDto(
    int InvoiceItemId,
    string InvoiceNumber,
    int Quantity,
    decimal UnitSellingPrice,
    DateTime SoldAt,
    string CashierName,
    IReadOnlyList<ProductReturnHistoryDto> Returns
);

public record ProductHistoryDto(
    int ProductId,
    IReadOnlyList<ProductSaleHistoryDto> Sales
);
