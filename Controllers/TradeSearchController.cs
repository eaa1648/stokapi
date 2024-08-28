using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System;
using System.Collections.Generic;

[Route("api/[controller]")]
[ApiController]
public class TradeSearchController : ControllerBase
{
    private readonly string _connectionString;

    public TradeSearchController(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("PostgreSql");
    }

    // GET: api/tradesearch/byusername?username={username}
    [HttpGet("byusername")]
    public IActionResult GetTradesByUsername(string username)
    {
        var trades = new List<object>();

        using (var conn = new NpgsqlConnection(_connectionString))
        {
            conn.Open();
            using (var cmd = new NpgsqlCommand("SELECT * FROM transactions WHERE username = @username", conn))
            {
                cmd.Parameters.AddWithValue("username", username);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        trades.Add(new
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("id")),
                            Username = reader.GetString(reader.GetOrdinal("username")),
                            StockName = reader.GetString(reader.GetOrdinal("stockname")),
                            Quantity = reader.GetInt32(reader.GetOrdinal("quantity")),
                            Price = reader.GetDecimal(reader.GetOrdinal("price")),
                            TransactionType = reader.GetString(reader.GetOrdinal("transaction_type")),
                            TransactionDate = reader.GetDateTime(reader.GetOrdinal("transaction_date")) // Ensure column name matches
                        });
                    }
                }
            }
        }

        if (trades.Count == 0)
        {
            return NotFound("No trades found for the specified username.");
        }

        return Ok(trades);
    }

    // GET: api/tradesearch/bydate?date={date}
    [HttpGet("bydate")]
    public IActionResult GetTradesByDate(DateTime date)
    {
        var trades = new List<object>();

        using (var conn = new NpgsqlConnection(_connectionString))
        {
            conn.Open();
            using (var cmd = new NpgsqlCommand("SELECT * FROM transactions WHERE transaction_date::date = @date", conn))
            {
                cmd.Parameters.AddWithValue("date", date.Date); // Ensure date parameter is DateTime

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        trades.Add(new
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("id")),
                            Username = reader.GetString(reader.GetOrdinal("username")),
                            StockName = reader.GetString(reader.GetOrdinal("stockname")),
                            Quantity = reader.GetInt32(reader.GetOrdinal("quantity")),
                            Price = reader.GetDecimal(reader.GetOrdinal("price")),
                            TransactionType = reader.GetString(reader.GetOrdinal("transaction_type")),
                            TransactionDate = reader.GetDateTime(reader.GetOrdinal("transaction_date")) // Ensure column name matches
                        });
                    }
                }
            }
        }

        if (trades.Count == 0)
        {
            return NotFound("No trades found for the specified date.");
        }

        return Ok(trades);
    }
}
