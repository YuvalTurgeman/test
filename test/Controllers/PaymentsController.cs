using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;
using test.Services;
using test.Data;
using System.Security.Claims;

namespace test.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly PaymentService _paymentService;
    private readonly ShoppingCartDAL _shoppingCartDAL;
    private readonly CartItemDAL _cartItemDAL;
    private readonly BookDAL _bookDAL;

    public PaymentsController(
        PaymentService paymentService, 
        ShoppingCartDAL shoppingCartDAL,
        CartItemDAL cartItemDAL,
        BookDAL bookDAL)
    {
        _paymentService = paymentService;
        _shoppingCartDAL = shoppingCartDAL;
        _cartItemDAL = cartItemDAL;
        _bookDAL = bookDAL;
    }

    [HttpPost("create-checkout-session")]
    public async Task<IActionResult> CreateCheckoutSession(int? bookId = null, bool? isBorrow = null)
    {
        try
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var lineItems = new List<SessionLineItemOptions>();
            string successUrl;
            string cancelUrl;

            // Handle direct borrow payment
            if (bookId.HasValue && isBorrow.HasValue)
            {
                var book = await _bookDAL.GetBookByIdAsync(bookId.Value);
                if (book == null)
                    return BadRequest(new { message = "Book not found" });

                var price = isBorrow.Value ? book.BorrowPrice : book.PurchasePrice;
                if (!price.HasValue)
                    return BadRequest(new { message = "Invalid price" });

                lineItems.Add(new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "usd",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = "Digireads eBooks"
                        },
                        UnitAmount = Convert.ToInt64(price.Value * 100)
                    },
                    Quantity = 1
                });

                // Store borrow intent in session
                if (isBorrow.Value)
                {
                    HttpContext.Session.SetString("BorrowIntent", $"{bookId}:{userId}");
                }

                successUrl = isBorrow.Value ? 
                    "https://localhost:7151/Borrow/Success" : 
                    "https://localhost:7151/ShoppingCart/Success";
                cancelUrl = isBorrow.Value ? 
                    "https://localhost:7151/Borrow/Cancel" : 
                    "https://localhost:7151/ShoppingCart/Cancel";
            }
            // Handle shopping cart payment
            else
            {
                var cart = await _shoppingCartDAL.GetByUserIdAsync(userId);
                if (cart == null || !cart.CartItems.Any())
                {
                    return BadRequest(new { message = "Shopping cart is empty" });
                }

                decimal totalAmount = cart.CartItems.Sum(item => 
                    (item.FinalPrice ?? 
                     (item.IsBorrow ? item.Book.BorrowPrice : item.Book.PurchasePrice) ?? 0) * item.Quantity);

                lineItems.Add(new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "usd",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = "Digireads eBooks"
                        },
                        UnitAmount = Convert.ToInt64(totalAmount * 100)
                    },
                    Quantity = 1
                });

                successUrl = "https://localhost:7151/ShoppingCart/Success";
                cancelUrl = "https://localhost:7151/ShoppingCart/Cancel";
            }

            var sessionUrl = await _paymentService.CreateCheckoutSessionAsync(lineItems, successUrl, cancelUrl);
            return Ok(new { url = sessionUrl });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while processing your request" });
        }
    }
}