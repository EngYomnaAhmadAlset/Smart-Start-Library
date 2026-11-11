using Microsoft.EntityFrameworkCore;
using SmartStart.Application.DTOs;
using SmartStart.Application.Interfaces;
using SmartStart.Domain.Entities;

namespace SmartStart.Application.Services;

public class AuthService : IAuthService
{
    private readonly IApplicationDbContext _context;

    public AuthService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto dto)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == dto.Username);

        // Unified message — do not reveal whether username exists
        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            throw new Exception("اسم المستخدم أو كلمة المرور غير صحيحة");

        if (!user.IsActive)
            throw new Exception("الحساب معطل، راجعي الأدمن");

        return new LoginResponseDto(
            UserId: user.Id,
            Username: user.Username,
            Role: user.Role.ToString(),
            CanAccessPhotocopy: user.CanAccessPhotocopy
        );
    }

    public async Task ChangePasswordAsync(int userId, ChangePasswordDto dto)
    {
        var user = await _context.Users.FindAsync(userId)
            ?? throw new Exception("المستخدم غير موجود");

        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            throw new Exception("كلمة المرور الحالية غير صحيحة");

        if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 6)
            throw new Exception("كلمة المرور الجديدة يجب أن تكون 6 أحرف على الأقل");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        await _context.SaveChangesAsync();
    }

    public async Task<LoginResponseDto> ChangeUsernameAsync(int userId, ChangeUsernameDto dto)
    {
        var user = await _context.Users.FindAsync(userId)
            ?? throw new Exception("المستخدم غير موجود");

        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            throw new Exception("كلمة المرور الحالية غير صحيحة");

        var username = (dto.NewUsername ?? string.Empty).Trim();

        if (username.Length < 3)
            throw new Exception("اسم المستخدم يجب أن يكون 3 أحرف على الأقل");

        if (username.Any(char.IsWhiteSpace))
            throw new Exception("اسم المستخدم لا يمكن أن يحتوي مسافات");

        var taken = await _context.Users
            .AnyAsync(u => u.Id != userId && u.Username.ToLower() == username.ToLower());
        if (taken)
            throw new Exception("اسم المستخدم مستخدم مسبقاً، اختاري اسماً آخر");

        user.Username = username;
        await _context.SaveChangesAsync();

        return new LoginResponseDto(
            UserId: user.Id,
            Username: user.Username,
            Role: user.Role.ToString(),
            CanAccessPhotocopy: user.CanAccessPhotocopy
        );
    }
}
