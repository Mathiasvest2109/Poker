namespace Poker.Server.Models 
{
    public class User
    {
        public int Id { get; set; } // Primary key

        public string Username { get; set; } = string.Empty;

        public string Displayname {get; set;} = string.Empty;

        public string PasswordHash { get; set; } = string.Empty; 
    }
}