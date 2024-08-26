using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

[ApiController]
[Route("[controller]")]
public class StockController : ControllerBase
{
    private readonly ScrapingService _scrapingService;

    public StockController(ScrapingService scrapingService)
    {
        _scrapingService = scrapingService;
    }

    [HttpGet]
    public IActionResult GetStockData()
    {
        var data = _scrapingService.ScrapeAndInsertData();
        return Ok(data);
    }
}
