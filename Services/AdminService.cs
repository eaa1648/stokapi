using Npgsql;
using System.Collections.Generic;

public class AdminService
{
    private readonly string _connectionString;

    public AdminService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("PostgreSql");
    }

    public IEnumerable<string> GetAllUsers()
    {
        var users = new List<string>();

        using (var conn = new NpgsqlConnection(_connectionString))
        {
            conn.Open();
            using (var cmd = new NpgsqlCommand("SELECT Username FROM Users", conn))
            {
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        users.Add(reader.GetString(0));
                    }
                }
            }
        }

        return users;
    }

    public void AddUser(string username, string password, string role = "user")
    {
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

        using (var conn = new NpgsqlConnection(_connectionString))
        {
            conn.Open();
            using (var cmd = new NpgsqlCommand("INSERT INTO Users (Username, Password, Role) VALUES (@username, @password, @role)", conn))
            {
                cmd.Parameters.AddWithValue("username", username);
                cmd.Parameters.AddWithValue("password", hashedPassword);
                cmd.Parameters.AddWithValue("role", role);
                cmd.ExecuteNonQuery();
            }
        }
    }

    public void DeleteUser(string username)
    {
        using (var conn = new NpgsqlConnection(_connectionString))
        {
            conn.Open();
            using (var cmd = new NpgsqlCommand("DELETE FROM Users WHERE Username = @username", conn))
            {
                cmd.Parameters.AddWithValue("username", username);
                cmd.ExecuteNonQuery();
            }
        }
    }

    public UserDetailsModel GetUserDetails(string username)
    {
        var userDetails = new UserDetailsModel();

        using (var conn = new NpgsqlConnection(_connectionString))
        {
            conn.Open();

            // Get user balance
            using (var cmd = new NpgsqlCommand("SELECT Balance FROM users WHERE id = (SELECT id FROM Users WHERE Username = @username)", conn))
            {
                cmd.Parameters.AddWithValue("username", username);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        userDetails.Username = username;
                        userDetails.Balance = reader.GetDecimal(0);
                    }
                }
            }

            // Get user stocks
            using (var cmd = new NpgsqlCommand("SELECT StockName, Quantity FROM UserStocks WHERE id = (SELECT id FROM Users WHERE Username = @username)", conn))
            {
                cmd.Parameters.AddWithValue("username", username);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        userDetails.Stocks.Add(new UserStockModel
                        {
                            StockName = reader.GetString(0),
                            Quantity = reader.GetInt32(1)
                        });
                    }
                }
            }
        }

        return userDetails;
    }

    public void UpdateUserBalance(string username, decimal amount)
    {
        using (var conn = new NpgsqlConnection(_connectionString))
        {
            conn.Open();
            using (var cmd = new NpgsqlCommand("UPDATE users SET Balance = Balance + @amount WHERE id = (SELECT id FROM Users WHERE Username = @username)", conn))
            {
                cmd.Parameters.AddWithValue("amount", amount);
                cmd.Parameters.AddWithValue("username", username);
                cmd.ExecuteNonQuery();
            }
        }
    }

    public void UpdateUserStock(string username, string stockName, int quantity)
    {
        using (var conn = new NpgsqlConnection(_connectionString))
        {
            conn.Open();
            using (var cmd = new NpgsqlCommand(@"INSERT INTO UserStocks (id, StockName, Quantity)
                                                 VALUES ((SELECT id FROM Users WHERE Username = @username), @stockName, @quantity)
                                                 ON CONFLICT (id, StockName) DO UPDATE
                                                 SET Quantity = UserStocks.Quantity + @quantity", conn))
            {
                cmd.Parameters.AddWithValue("username", username);
                cmd.Parameters.AddWithValue("stockName", stockName);
                cmd.Parameters.AddWithValue("quantity", quantity);
                cmd.ExecuteNonQuery();
            }
        }
    }
}

public class UserDetailsModel
{
    
    public string Username { get; set; }
    public decimal Balance { get; set; }
    public List<UserStockModel> Stocks { get; set; } = new List<UserStockModel>();
}

public class UserStockModel
{
    public string StockName { get; set; }
    public int Quantity { get; set; }
}
