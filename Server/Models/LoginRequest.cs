namespace Poker.Server.Models
{
    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;

        public string? DisplayName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        
    }
}