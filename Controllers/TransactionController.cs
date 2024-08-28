using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

[Route("api/[controller]")]
[ApiController]
public class TransactionController : ControllerBase
{
    private readonly TransactionService _transactionService;

    public TransactionController(TransactionService transactionService)
    {
        _transactionService = transactionService;
    }

    [HttpGet("list")]
    public ActionResult<List<Transaction>> GetTransactions()
    {
        var transactions = _transactionService.GetTransactions();
        return Ok(transactions);
    }
}
