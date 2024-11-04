using System.ComponentModel.DataAnnotations.Schema;

namespace DotNetCoreWebJWTAuthAPI.Entities
{
    public class RefreshToken
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public required string Token { get; set; }      // Der JWT-Token-String
        [ForeignKey("User")]
        public required string Email { get; set; }      // E-Mail des zugehörigen Benutzers
        public DateTime ExpiryDate { get; set; }        // Ablaufdatum des Tokens
        public bool IsRevoked { get; set; }             // Flag, um deaktivierte Tokens zu markieren
        public virtual User? User { get; set; }         // Beziehung zu User
    }
}