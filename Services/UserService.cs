using Npgsql;
using BCrypt.Net;
using System.Collections.Generic;

public class UserService
{
    private readonly string _connectionString;

    public UserService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public string HashPassword(string password)
    {
        // Hash the password
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool VerifyPassword(string enteredPassword, string storedHash)
    {
        // Verify the hashed password
        return BCrypt.Net.BCrypt.Verify(enteredPassword, storedHash);
    }

    public void SaveUser(string username, string password, string email, string role = "user")
    {
        string hashedPassword = HashPassword(password);

        // Save the hashed password and email to the database
        using (var conn = new NpgsqlConnection(_connectionString))
        {
            conn.Open();
            using (var command = new NpgsqlCommand("INSERT INTO users (Username, Password, Email, Role) VALUES (@username, @password, @e-mail, @role)", conn))
            {
                command.Parameters.AddWithValue("username", username);
                command.Parameters.AddWithValue("password", hashedPassword);
                command.Parameters.AddWithValue("email", email);
                command.Parameters.AddWithValue("role", role);
                command.ExecuteNonQuery();
            }
        }
    }

    public bool AuthenticateUser(string username, string enteredPassword, out string role)
    {
        string storedHash = "";
        role = "";

        // Retrieve the hashed password and role from the database
        using (var conn = new NpgsqlConnection(_connectionString))
        {
            conn.Open();
            using (var command = new NpgsqlCommand("SELECT Password, Role FROM users WHERE Username = @username", conn))
            {
                command.Parameters.AddWithValue("username", username);
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        storedHash = reader.GetString(0);
                        role = reader.GetString(1);
                    }
                    else
                    {
                        return false; // User not found
                    }
                }
            }
        }

        // Verify the password
        return VerifyPassword(enteredPassword, storedHash);
    }

    public bool UserExists(string username)
    {
        using (var conn = new NpgsqlConnection(_connectionString))
        {
            conn.Open();
            using (var command = new NpgsqlCommand("SELECT COUNT(1) FROM users WHERE Username = @username", conn))
            {
                command.Parameters.AddWithValue("username", username);
                var result = (long)command.ExecuteScalar();
                return result > 0;
            }
        }
    }

    public void DeleteUser(string username)
    {
        using (var conn = new NpgsqlConnection(_connectionString))
        {
            conn.Open();
            using (var command = new NpgsqlCommand("DELETE FROM users WHERE Username = @username", conn))
            {
                command.Parameters.AddWithValue("username", username);
                command.ExecuteNonQuery();
            }
        }
    }

    public void UpdateUserRole(string username, string newRole)
    {
        using (var conn = new NpgsqlConnection(_connectionString))
        {
            conn.Open();
            using (var command = new NpgsqlCommand("UPDATE users SET Role = @role WHERE Username = @username", conn))
            {
                command.Parameters.AddWithValue("username", username);
                command.Parameters.AddWithValue("role", newRole);
                command.ExecuteNonQuery();
            }
        }
    }

    public List<User> GetAllUsers()
    {
        var users = new List<User>();

        using (var conn = new NpgsqlConnection(_connectionString))
        {
            conn.Open();
            using (var command = new NpgsqlCommand("SELECT Id, Username, Email, Role FROM users", conn))
            {
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        users.Add(new User
                        {
                            Id = reader.GetInt32(0),
                            Username = reader.GetString(1),
                            Email = reader.GetString(2),
                            Role = reader.GetString(3)
                        });
                    }
                }
            }
        }

        return users;
    }
}

// Define a User class to hold user details
public class User
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; } // Added Email field
    public string Role { get; set; }
}
