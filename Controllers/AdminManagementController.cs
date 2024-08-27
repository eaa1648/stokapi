using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

[ApiController]
[Route("api/[controller]")]
public class AdminManagementController : ControllerBase
{
    private readonly UserStockEmailService _userStockEmailService;

    public AdminManagementController(UserStockEmailService userStockEmailService)
    {
        _userStockEmailService = userStockEmailService;
    }

    [HttpPost("trigger-email-report")]
    
    public async Task<IActionResult> TriggerEmailReport()
    {
        await _userStockEmailService.SendUserStocksEmail();
        return Ok("Emails have been sent.");
    }
}
