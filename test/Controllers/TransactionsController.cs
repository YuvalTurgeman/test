using Microsoft.AspNetCore.Mvc;
using test.Data;

[Route("Transactions")]
public class TransactionsController : Controller
{
    private readonly TransactionDAL _transactionDal;

    public TransactionsController(TransactionDAL transactionDal)
    {
        _transactionDal = transactionDal;
    }

    [HttpGet("AdminTransactions")]
    public async Task<IActionResult> AdminTransactions()
    {
        var transactions = await _transactionDal.GetAllTransactionsAsync();
        return View(transactions); // Render Razor view
    }
}