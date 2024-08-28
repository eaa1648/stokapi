using Npgsql;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

public class TransactionService
{
    private readonly string _connectionString;

    public TransactionService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("PostgreSql");
    }

    public void AddTransaction(string username, string stockname, string transactionType, int quantity, decimal price)
    {
        using (var conn = new NpgsqlConnection(_connectionString))
        {
            conn.Open();
            var query = @"
                INSERT INTO transactions (username, stockname, transaction_type, quantity, price)
                VALUES (@username, @stockname, @transaction_type, @quantity, @price)";

            using (var cmd = new NpgsqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("username", username);
                cmd.Parameters.AddWithValue("stockname", stockname);
                cmd.Parameters.AddWithValue("transaction_type", transactionType);
                cmd.Parameters.AddWithValue("quantity", quantity);
                cmd.Parameters.AddWithValue("price", price);
                cmd.ExecuteNonQuery();
            }
        }
    }

    public List<Transaction> GetTransactions()
    {
        var transactions = new List<Transaction>();

        using (var conn = new NpgsqlConnection(_connectionString))
        {
            conn.Open();
            var query = "SELECT * FROM transactions ORDER BY transaction_date DESC";

            using (var cmd = new NpgsqlCommand(query, conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    transactions.Add(new Transaction
                    {
                        Id = reader.GetInt32(0),
                        TransactionDate = reader.GetDateTime(1),
                        Username = reader.GetString(2),
                        Stockname = reader.GetString(3),
                        TransactionType = reader.GetString(4),
                        Quantity = reader.GetInt32(5),
                        Price = reader.GetDecimal(6)
                    });
                }
            }
        }

        return transactions;
    }
}

public class Transaction
{
    public int Id { get; set; }
    public DateTime TransactionDate { get; set; }
    public string Username { get; set; }
    public string Stockname { get; set; }
    public string TransactionType { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}
