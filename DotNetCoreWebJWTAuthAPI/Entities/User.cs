using System.ComponentModel.DataAnnotations;

namespace DotNetCoreWebJWTAuthAPI.Entities
{
    public class User
    {
        [Key]
        public required string Email { get; set; }
        public required string Password { get; set; }
        public bool IsEmailConfirmed { get; set; }
        public virtual ICollection<RefreshToken>? RefreshTokens { get; set; }
    }
}
