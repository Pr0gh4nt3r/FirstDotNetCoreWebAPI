using DotNetCoreWebJWTAuthAPI.Data;
using DotNetCoreWebJWTAuthAPI.Entities;
using Microsoft.EntityFrameworkCore;

namespace DotNetCoreWebJWTAuthAPI.Services
{
    public interface IUserService
    {
        Task<User?> ValidateUserAsync(string email, string password);
    }

    public class UserService(DataContext dataContext) : IUserService
    {
        private readonly DataContext _dataContext = dataContext;

        public async Task<User?> ValidateUserAsync(string email, string password)
        {
            // Hier solltest du die Logik zur Überprüfung der Benutzerdaten implementieren
            User? user = await _dataContext.Users.SingleOrDefaultAsync(u => u.Email == email);

            // Überprüfe, ob der Benutzer existiert und das Passwort korrekt ist
            if (user != null && VerifyPassword(password, user.Password))
            {
                return user; // Benutzer ist authentifiziert
            }

            return null; // Ungültige Anmeldedaten
        }

        private static bool VerifyPassword(string password, string passwordHash)
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
    }
}
