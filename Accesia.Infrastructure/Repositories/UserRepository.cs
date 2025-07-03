using Accesia.Application.Common.Interfaces;
using Accesia.Domain.Entities;
using Accesia.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Accesia.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Email.Value == email);
    }

    public async Task<User?> GetUserByIdAsync(string id)
    {
        if (!Guid.TryParse(id, out var guid))
            return null;
        return await _context.Users.FirstOrDefaultAsync(u => u.Id == guid);
    }

    public async Task<User> CreateUserAsync(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<User> UpdateUserAsync(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();  
        return user;
    }

    public async Task<User?> DeleteUserAsync(string id)
    {
        var user = await GetUserByIdAsync(id);
        if (user == null)
            return null;
        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return user;
    }
}