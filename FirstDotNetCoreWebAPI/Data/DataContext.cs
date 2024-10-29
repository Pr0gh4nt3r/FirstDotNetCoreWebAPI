using FirstDotNetCoreWebAPI.Entities;
using Microsoft.EntityFrameworkCore;

namespace FirstDotNetCoreWebAPI.Data
{
    public class DataContext(DbContextOptions<DataContext> options) : DbContext(options)
    {
        public DbSet<User> Users { get; set; }
    }
}