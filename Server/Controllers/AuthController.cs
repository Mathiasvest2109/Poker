using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;
using Poker.Server.Models;
using Poker.Server.DAL;


namespace Server.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AuthController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
          if (request == null ||
        string.IsNullOrWhiteSpace(request.Username) ||
        string.IsNullOrWhiteSpace(request.Password))
        return BadRequest("Invalid login request.");

    // Look up the user by username
    var user = _context.Users
                       .FirstOrDefault(u => u.Username == request.Username);

    // Verify hash (returns true if password matches)
    if (user != null && BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
    {
        return Ok(new 
        {
            Username = user.Username,
            DisplayName = user.Displayname,
            UserId = user.Id, // assuming User model has an Id field
             Message = "Login successful" 
        }); 
    }

    return Unauthorized(new { message = "Invalid username or password" }); 



        }


        [HttpPost("createUser")]
        public IActionResult CreateUser([FromBody] LoginRequest request){
             if (request == null ||
        string.IsNullOrWhiteSpace(request.Username) ||
        string.IsNullOrWhiteSpace(request.Password))
        return BadRequest("Invalid user creation request.");

    if (_context.Users.Any(u => u.Username == request.Username))
        return Conflict(new { message = "Username already exists." });

    var user = new User
    {
        Username     = request.Username,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),// ← hash here
        Displayname = request.DisplayName, 
    };

    _context.Users.Add(user);
    _context.SaveChanges();

      return Ok(new 
    { 
        Username = user.Username,
        DisplayName = user.Displayname,
        UserId = user.Id,
        Message = "Ok" 
    });
          } 
    }
}


