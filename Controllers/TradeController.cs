using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Globalization;

[Route("api/[controller]")]
[ApiController]
public class TradeController : ControllerBase
{
    private readonly string _connectionString;

    public TradeController(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("PostgreSql");
    }

    [HttpPost("buy")]
    public IActionResult BuyStock([FromBody] TradeModel trade)
    {
        using (var conn = new NpgsqlConnection(_connectionString))
        {
            conn.Open();
            using (var transaction = conn.BeginTransaction())
            {
                try
                {
                    // Kullanıcının bakiyesini kontrol et
                    var userBalanceCmd = new NpgsqlCommand("SELECT balance FROM users WHERE username = @username", conn);
                    userBalanceCmd.Parameters.AddWithValue("username", trade.Username);
                    var userBalance = (decimal)userBalanceCmd.ExecuteScalar();

                    // Hisse senedinin fiyatını al
                    var stockPriceCmd = new NpgsqlCommand("SELECT son FROM data WHERE id = @id", conn);
                    stockPriceCmd.Parameters.AddWithValue("id", trade.StockId);
                    var stockPriceStr = stockPriceCmd.ExecuteScalar().ToString();
                    var stockPrice = decimal.Parse(stockPriceStr, CultureInfo.InvariantCulture);

                    var totalCost = stockPrice * trade.Quantity;

                    if (userBalance < totalCost)
                    {
                        return BadRequest("Yetersiz bakiye.");
                    }

                    // Kullanıcının bakiyesini güncelle
                    var updateBalanceCmd = new NpgsqlCommand("UPDATE users SET balance = balance - @amount WHERE username = @username", conn);
                    updateBalanceCmd.Parameters.AddWithValue("amount", totalCost);
                    updateBalanceCmd.Parameters.AddWithValue("username", trade.Username);
                    updateBalanceCmd.ExecuteNonQuery();

                    // Hisse senedi ismini al
                    var stockNameCmd = new NpgsqlCommand("SELECT isim FROM data WHERE id = @id", conn);
                    stockNameCmd.Parameters.AddWithValue("id", trade.StockId);
                    var stockName = stockNameCmd.ExecuteScalar().ToString();

                    // Kullanıcının hisse senedi miktarını güncelle veya ekle
                    var updateUserStockCmd = new NpgsqlCommand(@"
                        INSERT INTO userstocks (username, stockname, quantity, purchaseprice, purchasedate) 
                        VALUES (@username, @stockname, @quantity, @purchaseprice, CURRENT_TIMESTAMP) 
                        ON CONFLICT (username, stockname) 
                        DO UPDATE SET quantity = userstocks.quantity + @quantity, purchaseprice = EXCLUDED.purchaseprice", conn);
                    updateUserStockCmd.Parameters.AddWithValue("username", trade.Username);
                    updateUserStockCmd.Parameters.AddWithValue("stockname", stockName);
                    updateUserStockCmd.Parameters.AddWithValue("quantity", trade.Quantity);
                    updateUserStockCmd.Parameters.AddWithValue("purchaseprice", stockPrice);
                    updateUserStockCmd.ExecuteNonQuery();

                    transaction.Commit();
                    return Ok("Hisse senedi başarıyla satın alındı.");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return StatusCode(500, $"İşlem sırasında bir hata oluştu: {ex.Message}");
                }
            }
        }
    }

    [HttpPost("sell")]
    public IActionResult SellStock([FromBody] TradeModel trade)
    {
        using (var conn = new NpgsqlConnection(_connectionString))
        {
            conn.Open();
            using (var transaction = conn.BeginTransaction())
            {
                try
                {
                    // Hisse senedi ismini al
                    var stockNameCmd = new NpgsqlCommand("SELECT isim FROM data WHERE id = @id", conn);
                    stockNameCmd.Parameters.AddWithValue("id", trade.StockId);
                    var stockName = stockNameCmd.ExecuteScalar().ToString();

                    // Kullanıcının sahip olduğu hisse senedinin miktarını kontrol et
                    var userStockCmd = new NpgsqlCommand("SELECT quantity FROM userstocks WHERE username = @username AND stockname = @stockname", conn);
                    userStockCmd.Parameters.AddWithValue("username", trade.Username);
                    userStockCmd.Parameters.AddWithValue("stockname", stockName);
                    var userStockQuantity = (int?)userStockCmd.ExecuteScalar();

                    if (userStockQuantity == null || userStockQuantity < trade.Quantity)
                    {
                        return BadRequest("Yetersiz hisse senedi miktarı.");
                    }

                    // Hisse senedinin fiyatını al
                    var stockPriceCmd = new NpgsqlCommand("SELECT son FROM data WHERE id = @id", conn);
                    stockPriceCmd.Parameters.AddWithValue("id", trade.StockId);
                    var stockPriceStr = stockPriceCmd.ExecuteScalar().ToString();
                    var stockPrice = decimal.Parse(stockPriceStr, CultureInfo.InvariantCulture);

                    var totalRevenue = stockPrice * trade.Quantity;

                    // Kullanıcının bakiyesini güncelle
                    var updateBalanceCmd = new NpgsqlCommand("UPDATE users SET balance = balance + @amount WHERE username = @username", conn);
                    updateBalanceCmd.Parameters.AddWithValue("amount", totalRevenue);
                    updateBalanceCmd.Parameters.AddWithValue("username", trade.Username);
                    updateBalanceCmd.ExecuteNonQuery();

                    // Kullanıcının hisse senedi miktarını güncelle
                    var updateUserStockCmd = new NpgsqlCommand("UPDATE userstocks SET quantity = quantity - @quantity WHERE username = @username AND stockname = @stockname", conn);
                    updateUserStockCmd.Parameters.AddWithValue("username", trade.Username);
                    updateUserStockCmd.Parameters.AddWithValue("stockname", stockName);
                    updateUserStockCmd.Parameters.AddWithValue("quantity", trade.Quantity);
                    updateUserStockCmd.ExecuteNonQuery();

                    // Hisse senedi miktarı sıfır veya altına düşerse kaydı sil
                    if (userStockQuantity == trade.Quantity)
                    {
                        var deleteUserStockCmd = new NpgsqlCommand("DELETE FROM userstocks WHERE username = @username AND stockname = @stockname", conn);
                        deleteUserStockCmd.Parameters.AddWithValue("username", trade.Username);
                        deleteUserStockCmd.Parameters.AddWithValue("stockname", stockName);
                        deleteUserStockCmd.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    return Ok("Hisse senedi başarıyla satıldı.");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return StatusCode(500, $"İşlem sırasında bir hata oluştu: {ex.Message}");
                }
            }
        }
    }

    [HttpGet("stocks/{username}")]
    public IActionResult GetUserStocks(string username)
    {
        using (var conn = new NpgsqlConnection(_connectionString))
        {
            conn.Open();
            var getUserStocksCmd = new NpgsqlCommand("SELECT stockname, quantity, purchaseprice, purchasedate FROM userstocks WHERE username = @username", conn);
            getUserStocksCmd.Parameters.AddWithValue("username", username);

            using (var reader = getUserStocksCmd.ExecuteReader())
            {
                var userStocks = new List<UserStockDto>();
                while (reader.Read())
                {
                    userStocks.Add(new UserStockDto
                    {
                        StockName = reader.GetString(0),
                        Quantity = reader.GetInt32(1),
                        PurchasePrice = reader.GetDecimal(2),
                        PurchaseDate = reader.GetDateTime(3)
                    });
                }

                return Ok(userStocks);
            }
        }
    }
}
