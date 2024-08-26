using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ScrapingController : ControllerBase
{
    private readonly ScrapingService _scrapingService;

    public ScrapingController(ScrapingService scrapingService)
    {
        _scrapingService = scrapingService;
    }

   [HttpGet("scrape")]
public IActionResult ScrapeData()
{
    try
    {
        var data = _scrapingService.ScrapeAndInsertData();
        return Ok(data); // Scraped data'yi döndür
    }
    catch (Exception ex)
    {
        return BadRequest($"Bir hata oluştu: {ex.Message}");
    }
}

}
