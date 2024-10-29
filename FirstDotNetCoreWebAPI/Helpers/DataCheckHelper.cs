using FirstDotNetCoreWebAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace FirstDotNetCoreWebAPI.Helpers
{
    internal class DataCheckHelper
    {
        internal static async Task<bool> UserExistsAsync(DataContext dataContext, string email)
        {
            return await dataContext.Users.AnyAsync(u => u.Email == email);
        }
    }
}
