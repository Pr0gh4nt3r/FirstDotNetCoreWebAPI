namespace FirstDotNetCoreWebAPI.DTOs
{
    public class UserUpdateDTO
    {
        public string? Email { get; set; }
        public string? Password { get; set; } = null;
        public bool? IsEmailConfirmed { get; set; }
    }
}
