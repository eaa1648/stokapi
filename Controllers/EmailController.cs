using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "admin")]
public class EmailController : ControllerBase
{
    private readonly EmailService _emailService;

    public EmailController(EmailService emailService)
    {
        _emailService = emailService;
    }

    [HttpPost("Eposta-gonderme")]
    public async Task<IActionResult> SendEmail([FromForm] string to, [FromForm] string subject, [FromForm] string body, [FromForm] IEnumerable<IFormFile> attachments)
    {
        if (string.IsNullOrEmpty(to) || string.IsNullOrEmpty(subject) || string.IsNullOrEmpty(body))
        {
            return BadRequest("Invalid email request.");
        }

        try
        {
            await _emailService.SendEmailAsync(to, subject, body, attachments);
            return Ok("Email sent successfully.");
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
}
