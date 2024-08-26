public class UserStockDto
{
    public int Id { get; set; } // `Id` özelliği eklendi
    public string StockName { get; set; }
    public int Quantity { get; set; }
    public decimal PurchasePrice { get; set; }
    public DateTime PurchaseDate { get; set; }
}
