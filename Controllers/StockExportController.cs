using Microsoft.AspNetCore.Mvc;
using MiniExcelLibs;
using Npgsql;
using System.Data;
using System.IO;
using System.Threading.Tasks;

[Route("api/[controller]")]
[ApiController]
public class StockExportController : ControllerBase
{
    private readonly string _connectionString;

    public StockExportController(Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("PostgreSql");

        if (string.IsNullOrEmpty(_connectionString))
        {
            throw new InvalidOperationException("The ConnectionString property has not been initialized.");
        }
    }

    [HttpGet("export/{username}")]
    public async Task<IActionResult> ExportUserStocks(string username)
    {
        try
        {
            var userStocks = GetUserStocksFromDatabase(username);

            if (userStocks.Rows.Count == 0)
            {
                return NotFound("No stocks found for the specified user.");
            }

            var fileName = $"{username}_stocks.xlsx";
            using var memoryStream = new MemoryStream();

            // MiniExcelLibs ile veriyi kaydet
            await memoryStream.SaveAsAsync(userStocks);

            memoryStream.Position = 0;
            return File(memoryStream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    private DataTable GetUserStocksFromDatabase(string username)
    {
        var query = @"SELECT stockname, quantity, purchaseprice, purchasedate 
                      FROM userstocks 
                      WHERE username = @username";

        var dataTable = new DataTable();

        using (var connection = new NpgsqlConnection(_connectionString))
        {
            connection.Open();
            using (var command = new NpgsqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@username", username);

                using (var reader = command.ExecuteReader())
                {
                    dataTable.Load(reader);
                }
            }
        }

        return dataTable;
    }
}
