namespace Poker.Server.Models
{
    public class LoginResponse
    {
        public bool Success { get; set; } = false;
        public string? Username { get; set; } = string.Empty;

        public string? Displayname {get; set;} = string.Empty;


      
    }
}



