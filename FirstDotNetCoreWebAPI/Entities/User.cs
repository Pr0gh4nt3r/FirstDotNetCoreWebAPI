using System.ComponentModel.DataAnnotations;

namespace FirstDotNetCoreWebAPI.Entities
{
    public class User
    {
        [Key]
        public required string Email { get; set; }
        public required string Password { get; set; }
        public bool IsEmailConfirmed { get; set; }
    }
}
