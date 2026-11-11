namespace SmartStart.Application.DTOs;

public record CreateInvoiceItemDto(int ProductId, int Quantity);

public record CreateInvoiceDto(int CashierId, List<CreateInvoiceItemDto> Items);

public record InvoiceItemResponseDto(
    int Id,
    int ProductId,
    string ProductName,
    string Barcode,
    int Quantity,
    decimal UnitWholesalePrice,
    decimal UnitSellingPrice,
    decimal LineTotal,
    decimal LineProfit,
    int AvailableToReturn
);

public record InvoiceResponseDto(
    int Id,
    string InvoiceNumber,
    string QrCode,
    int CashierId,
    decimal TotalAmount,
    decimal TotalProfit,
    DateTime CreatedAt,
    List<InvoiceItemResponseDto> Items
);
