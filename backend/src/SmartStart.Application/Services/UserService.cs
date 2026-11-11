using Microsoft.EntityFrameworkCore;
using SmartStart.Application.DTOs;
using SmartStart.Application.Interfaces;
using SmartStart.Domain.Entities;

namespace SmartStart.Application.Services;

public class UserService : IUserService
{
    private readonly IApplicationDbContext _context;

    public UserService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<UserResponseDto>> GetCashiersAsync()
    {
        var cashiers = await _context.Users
            .Where(u => u.Role == UserRole.Cashier)
            .OrderBy(u => u.Username)
            .ToListAsync();

        return cashiers.Select(Map);
    }

    public async Task<UserResponseDto> CreateCashierAsync(CreateCashierDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password))
            throw new Exception("اسم المستخدم وكلمة المرور مطلوبان");

        if (dto.Password.Length < 6)
            throw new Exception("كلمة المرور يجب أن تكون 6 أحرف على الأقل");

        var exists = await _context.Users.AnyAsync(u => u.Username == dto.Username);
        if (exists)
            throw new Exception("اسم المستخدم مستخدم مسبقاً");

        var user = new User
        {
            Username = dto.Username.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = UserRole.Cashier,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return Map(user);
    }

    public async Task<UserResponseDto> UpdateCashierAsync(int id, UpdateCashierDto dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.Role == UserRole.Cashier)
            ?? throw new Exception("الكاشير غير موجود");

        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            if (dto.Password.Length < 6)
                throw new Exception("كلمة المرور يجب أن تكون 6 أحرف على الأقل");
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        }

        if (dto.IsActive.HasValue)
            user.IsActive = dto.IsActive.Value;

        if (dto.CanAccessPhotocopy.HasValue)
            user.CanAccessPhotocopy = dto.CanAccessPhotocopy.Value;

        await _context.SaveChangesAsync();
        return Map(user);
    }

    public async Task<UserResponseDto?> GetByIdAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        return user == null ? null : Map(user);
    }

    private static UserResponseDto Map(User u) =>
        new(u.Id, u.Username, u.Role.ToString(), u.IsActive, u.CanAccessPhotocopy, u.CreatedAt);
}
