using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;
using Npgsql;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using BCrypt.Net;
using System.Collections.Generic;

[Route("api/[controller]")]
[ApiController]
public class AuthenticationController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly string _connectionString;

    public AuthenticationController(IConfiguration configuration)
    {
        _configuration = configuration;
        _connectionString = _configuration.GetConnectionString("PostgreSql");
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginModel login)
    {
        if (AuthenticateUser(login.Username, login.Password, out string role))
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["JwtSettings:SecretKey"]);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Name, login.Username),
                    new Claim(ClaimTypes.Role, role) // Role ekleyin
                }),
                Expires = DateTime.UtcNow.AddHours(1),
                Issuer = _configuration["JwtSettings:Issuer"],
                Audience = _configuration["JwtSettings:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);
            return Ok(new { Token = tokenString });
        }
        return Unauthorized();
    }

   [HttpPost("register")]
public IActionResult Register([FromBody] RegisterModel registration)
{
    if (string.IsNullOrWhiteSpace(registration.Username) || string.IsNullOrWhiteSpace(registration.Password) || string.IsNullOrWhiteSpace(registration.Email))
    {
        return BadRequest("Username, password, and email are required.");
    }

    if (UserExists(registration.Username))
    {
        return BadRequest("Username already exists.");
    }

    var hashedPassword = BCrypt.Net.BCrypt.HashPassword(registration.Password);

    using (var conn = new NpgsqlConnection(_connectionString))
    {
        conn.Open();
        using (var cmd = new NpgsqlCommand("INSERT INTO Users (Username, Password, Email, Role) VALUES (@username, @password, @email, @role)", conn))
        {
            cmd.Parameters.AddWithValue("username", registration.Username);
            cmd.Parameters.AddWithValue("password", hashedPassword);
            cmd.Parameters.AddWithValue("email", registration.Email); // Include email
            cmd.Parameters.AddWithValue("role", registration.Role ?? "user"); // Default role to "user" if null
            cmd.ExecuteNonQuery();
        }
    }

    return Ok("User registered successfully.");
}



    [HttpGet("users")]
    [Authorize(Roles = "admin")] // Sadece adminler erişebilir
    public IActionResult GetAllUsers()
    {
        var users = new List<User>();

        using (var conn = new NpgsqlConnection(_connectionString))
        {
            conn.Open();
            using (var cmd = new NpgsqlCommand("SELECT Id, Username, Role FROM Users", conn))
            {
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        users.Add(new User
                        {
                            Id = reader.GetInt32(0),
                            Username = reader.GetString(1),
                            Role = reader.GetString(2)
                        });
                    }
                }
            }
        }

        return Ok(users);
    }

    private bool AuthenticateUser(string username, string password, out string role)
    {
        string storedHash = "";
        role = "";

        using (var conn = new NpgsqlConnection(_connectionString))
        {
            conn.Open();
            using (var cmd = new NpgsqlCommand("SELECT Password, Role FROM Users WHERE Username = @username", conn))
            {
                cmd.Parameters.AddWithValue("username", username);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        storedHash = reader.GetString(0);
                        role = reader.GetString(1);
                    }
                    else
                    {
                        return false; // Kullanıcı bulunamadı
                    }
                }
            }
        }

        return BCrypt.Net.BCrypt.Verify(password, storedHash);
    }

    private bool UserExists(string username)
    {
        using (var conn = new NpgsqlConnection(_connectionString))
        {
            conn.Open();
            using (var cmd = new NpgsqlCommand("SELECT COUNT(1) FROM Users WHERE Username = @username", conn))
            {
                cmd.Parameters.AddWithValue("username", username);
                var result = (long)cmd.ExecuteScalar();
                return result > 0;
            }
        }
    }
}

public class LoginModel
{
    public string Username { get; set; }
    public string Password { get; set; }
}

public class RegisterModel : LoginModel
{
    public string Role { get; set; } // Rolü ekleyin

    public string Email { get; set; }
}


