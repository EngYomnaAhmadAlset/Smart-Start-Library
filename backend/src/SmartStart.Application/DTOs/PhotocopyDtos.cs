namespace SmartStart.Application.DTOs;

public record PhotocopyPaperDto(
    int Id,
    string Name,
    decimal WholesalePrice,
    decimal SellingPrice,
    int TotalReceivedQuantity,
    int RemainingQuantity,
    bool IsActive,
    DateTime CreatedAt
);

public record PhotocopyInkDto(
    string Level,
    decimal WholesalePerPage,
    decimal SellingPerPage,
    int RemainingPages
);

public record PhotocopySettingsDto(
    decimal MaintenanceCost,
    decimal ElectricityCost,
    IReadOnlyList<PhotocopyPaperDto> Papers,
    IReadOnlyList<PhotocopyInkDto> InkLevels,
    IReadOnlyList<PhotocopyCashierAccessDto> Cashiers
);

public record PhotocopyCashierAccessDto(
    int Id,
    string Username,
    bool IsActive,
    bool CanAccessPhotocopy
);

public record UpdatePhotocopyOverheadDto(decimal MaintenanceCost, decimal ElectricityCost);

public record CreatePhotocopyPaperDto(
    string Name,
    decimal WholesalePrice,
    decimal SellingPrice,
    int Quantity,
    bool ConfirmUnprofitable
);

public record UpdatePhotocopyPaperDto(
    string Name,
    decimal WholesalePrice,
    decimal SellingPrice,
    bool IsActive,
    bool ConfirmUnprofitable
);

public record RestockPhotocopyPaperDto(int Quantity);

public record UpdatePhotocopyInkLevelDto(
    string Level,
    decimal WholesalePerPage,
    decimal SellingPerPage,
    int RemainingPages,
    bool ConfirmUnprofitable
);

public record UpdatePhotocopyInkDto(IReadOnlyList<UpdatePhotocopyInkLevelDto> Levels);

public record SetPhotocopyAccessDto(bool CanAccessPhotocopy);

public record PhotocopyCatalogDto(
    bool CanAccess,
    bool IsReady,
    string? Message,
    decimal MaintenanceCost,
    decimal ElectricityCost,
    IReadOnlyList<PhotocopyPaperDto> Papers,
    IReadOnlyList<PhotocopyInkDto> InkLevels
);

public record CreatePhotocopySaleDto(
    int PaperTypeId,
    string InkLevel,
    int PageCount
);

public record PhotocopySaleDto(
    int Id,
    string SaleNumber,
    int CashierId,
    int PaperTypeId,
    string PaperName,
    string InkLevel,
    int PageCount,
    decimal PaperSellingPrice,
    decimal InkSellingPerPage,
    decimal MaintenanceCost,
    decimal ElectricityCost,
    decimal TotalAmount,
    decimal CostAmount,
    decimal ProfitAmount,
    decimal ProfitPercent,
    DateTime CreatedAt
);
