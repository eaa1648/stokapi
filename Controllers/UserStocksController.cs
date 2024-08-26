using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Collections.Generic;
using System.Security.Principal;
using System.Threading.Tasks;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "admin")]
public class UserStocksController : ControllerBase
{
    private readonly string _connectionString;

    public UserStocksController(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("PostgreSql");
    }

    [HttpGet("kullanicistokkontrol")]
    public async Task<IActionResult> GetUserStocks(string username)
    {

        
        var userStocks = new List<UserStockDto>();

        using (var conn = new NpgsqlConnection(_connectionString))
        {
            await conn.OpenAsync();
            using (var cmd = new NpgsqlCommand(@"
                SELECT us.id, us.stockname, us.quantity, us.purchaseprice, us.purchasedate
                FROM userstocks us
                WHERE us.username = @username", conn))
            {
                cmd.Parameters.AddWithValue("username", username);
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        userStocks.Add(new UserStockDto
                        {
                            Id = reader.GetInt32(0), // `id` kolonu
                            StockName = reader.GetString(1),
                            Quantity = reader.GetInt32(2),
                            PurchasePrice = reader.GetDecimal(3),
                            PurchaseDate = reader.GetDateTime(4)
                        });
                    }
                }
            }
        }

        if (userStocks.Count == 0)
        {
            return NotFound("No stocks found for the user.");
        }

        return Ok(userStocks);
    }
}
