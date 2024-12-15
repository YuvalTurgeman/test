using Microsoft.AspNetCore.Mvc;
using test.Data;
using test.Models;

[ApiController]
[Route("api/[controller]")]
public class TransactionsApiController : ControllerBase
{
    private readonly TransactionDAL _transactionDal;

    public TransactionsApiController(TransactionDAL transactionDal)
    {
        _transactionDal = transactionDal;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllTransactions()
    {
        var transactions = await _transactionDal.GetAllTransactionsAsync();
        return Ok(transactions);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTransactionById(int id)
    {
        var transaction = await _transactionDal.GetTransactionByIdAsync(id);
        if (transaction == null) return NotFound();
        return Ok(transaction);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTransaction([FromBody] TransactionModel transaction)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        await _transactionDal.AddTransactionAsync(transaction);
        return CreatedAtAction(nameof(GetTransactionById), new { id = transaction.Id }, transaction);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTransaction(int id, [FromBody] TransactionModel transaction)
    {
        if (id != transaction.Id) return BadRequest("Transaction ID mismatch");
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var existingTransaction = await _transactionDal.GetTransactionByIdAsync(id);
        if (existingTransaction == null) return NotFound();

        await _transactionDal.UpdateTransactionAsync(transaction);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTransaction(int id)
    {
        var existingTransaction = await _transactionDal.GetTransactionByIdAsync(id);
        if (existingTransaction == null) return NotFound();

        await _transactionDal.DeleteTransactionAsync(id);
        return NoContent();
    }
}