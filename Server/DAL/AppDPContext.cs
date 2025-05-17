using Microsoft.EntityFrameworkCore;
using Poker.Server.Models; // Assuming 'User' is defined in this namespace



namespace Poker.Server.DAL
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}

        public DbSet<User> Users { get; set; }
    }
}
