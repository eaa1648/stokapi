using System.Text.Json.Serialization;

public class TradeModel
{
    public string Username { get; set; }
    public int StockId { get; set; }
    public int Quantity { get; set; }

    [JsonIgnore]
    public decimal PurchasePrice { get; set; } // Optional: Use if you need this property but don't want it in Swagger
}
