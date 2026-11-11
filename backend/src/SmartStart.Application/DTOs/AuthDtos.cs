namespace SmartStart.Application.DTOs;

public record LoginRequestDto(string Username, string Password);

public record LoginResponseDto(
    int UserId,
    string Username,
    string Role,
    bool CanAccessPhotocopy
);

public record ChangePasswordDto(string CurrentPassword, string NewPassword);

public record ChangeUsernameDto(string CurrentPassword, string NewUsername);

public record CreateCashierDto(string Username, string Password);

public record UpdateCashierDto(string? Password, bool? IsActive, bool? CanAccessPhotocopy);

public record UserResponseDto(
    int Id,
    string Username,
    string Role,
    bool IsActive,
    bool CanAccessPhotocopy,
    DateTime CreatedAt
);
