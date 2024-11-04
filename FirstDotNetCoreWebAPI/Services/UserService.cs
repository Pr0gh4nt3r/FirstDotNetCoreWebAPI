using FirstDotNetCoreWebAPI.Data;
using FirstDotNetCoreWebAPI.Entities;
using Microsoft.EntityFrameworkCore;

namespace FirstDotNetCoreWebAPI.Services
{
    public interface IUserService
    {
        Task<User?> RegisterUserAsync(string email, string password);
        Task<User?> ValidateUserAsync(string email, string password);
        Task<bool> UserExistsAsync(string email);
    }

    public class UserService(DataContext dataContext) : IUserService
    {
        private readonly DataContext _dataContext = dataContext;

        public async Task<bool> UserExistsAsync(string email)
        {
            return await _dataContext.Users.AnyAsync(u => u.Email == email);
        }

        public async Task<User?> RegisterUserAsync(string email, string password)
        {
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

            User user = new()
            {
                Email = email,
                Password = passwordHash
            };

            await _dataContext.Users.AddAsync(user);

            try
            {
                await _dataContext.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return null;
            }

            return user;
        }

        public async Task<User?> ValidateUserAsync(string email, string password)
        {
            var user = await _dataContext.Users.SingleOrDefaultAsync(u => u.Email == email);

            if (user != null && BCrypt.Net.BCrypt.Verify(password, user.Password))
                return user; // Benutzer ist authentifiziert

            return null; // Ungültige Anmeldedaten
        }
    }
}
