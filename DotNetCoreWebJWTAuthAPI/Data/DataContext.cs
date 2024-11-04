using DotNetCoreWebJWTAuthAPI.Entities;
using Microsoft.EntityFrameworkCore;

namespace DotNetCoreWebJWTAuthAPI.Data
{
    public class DataContext(DbContextOptions<DataContext> options) : DbContext(options)
    {
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RefreshToken>()
                .HasOne(rt => rt.User)                      // Beziehung zu User
                .WithMany(u => u.RefreshTokens)             // Beziehung zu RefreshTokens
                .HasForeignKey(rt => rt.Email);             // Festlegen des Fremdschlüssels
        }
    }
}
