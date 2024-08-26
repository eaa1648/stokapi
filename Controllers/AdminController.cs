using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "admin")]
public class AdminController : ControllerBase
{
    private readonly AdminService _adminService;

    public AdminController(AdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet("list-users")]
    public IActionResult GetAllUsers()
    {
        var users = _adminService.GetAllUsers();
        return Ok(users);
    }

    [HttpPost("add-user")]
    public IActionResult AddUser([FromBody] UserModel user)
    {
        if (_adminService.GetAllUsers().Contains(user.Username))
        {
            return BadRequest("Username already exists.");
        }

        _adminService.AddUser(user.Username, user.Password, user.Role);
        return Ok("User added successfully.");
    }

    [HttpDelete("delete-user")]
    public IActionResult DeleteUser(string username)
    {
        var users = _adminService.GetAllUsers();
        if (!users.Contains(username))
        {
            return NotFound("User not found.");
        }

        _adminService.DeleteUser(username);
        return Ok("User deleted successfully.");
    }

    // Endpoint to get user details including balance and stocks
    [HttpGet("user-details")]
    public IActionResult GetUserDetails(string username)
    {
        var userDetails = _adminService.GetUserDetails(username);
        if (userDetails == null || string.IsNullOrEmpty(userDetails.Username))
        {
            return NotFound("User not found.");
        }
        return Ok(userDetails);
    }

    // Endpoint to update user balance
    [HttpPost("user-balance")]
    public IActionResult UpdateUserBalance([FromBody] UpdateBalanceModel model)
    {
        try
        {
            _adminService.UpdateUserBalance(model.Username, model.Amount);
            return Ok("User balance updated successfully.");
        }
        catch (Exception)
        {
            return NotFound("User not found.");
        }
    }

    // Endpoint to update user stock
    [HttpPost("userstock-update")]
    public IActionResult UpdateUserStock([FromBody] UpdateStockModel model)
    {
        try
        {
            _adminService.UpdateUserStock(model.Username, model.StockName, model.Quantity);
            return Ok("User stock updated successfully.");
        }
        catch (Exception)
        {
            return NotFound("User not found.");
        }
    }
}

public class UserModel
{
    public string Username { get; set; }
    public string Password { get; set; }
    public string Role { get; set; } = "user";
}

public class UpdateBalanceModel
{
    public string Username { get; set; }
    public decimal Amount { get; set; } // Positive to add, negative to reduce
}

public class UpdateStockModel
{
    public string Username { get; set; }
    public string StockName { get; set; }
    public int Quantity { get; set; } // Positive to add, negative to reduce
}
