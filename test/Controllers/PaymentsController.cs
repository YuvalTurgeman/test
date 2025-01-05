using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;
using test.Services;
using test.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace test.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly PaymentService _paymentService;
    private readonly ShoppingCartDAL _shoppingCartDAL;
    private readonly CartItemDAL _cartItemDAL;
    private readonly BookDAL _bookDAL;
    private readonly BorrowDAL _borrowDAL;

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
       // _borrowDAL = borrowDal;
    }

    /*[HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCheckoutSession()
    {
        try
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            // Changed from _cartItemDAL to _cartDAL
            var cart = await _cartDAL.GetByUserIdAsync(userId);

            if (cart == null || !cart.CartItems.Any())
            {
                return BadRequest(new { message = "Cart is empty." });
            }

            // Validate borrow limit
            if (cart.CartItems.Count(i => i.IsBorrow) > 0)
            {
                var currentBorrows = await _borrowDAL.GetActiveUserBorrowsAsync(userId);
                if (currentBorrows.Count + cart.CartItems.Count(i => i.IsBorrow) > 3)
                {
                    return BadRequest(new { message = "You can only borrow up to 3 books at a time." });
                }
            }

            var lineItems = new List<SessionLineItemOptions>();
            foreach (var item in cart.CartItems)
            {
                lineItems.Add(new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "usd",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = $"{item.Book.Title} ({(item.IsBorrow ? "Borrow" : "Purchase")})",
                        },
                        UnitAmount = Convert.ToInt64(item.FinalPrice * 100)
                    },
                    Quantity = item.Quantity
                });
            }

            var sessionUrl = await _paymentService.CreateCheckoutSessionAsync(
                lineItems,
                "https://localhost:7151/ShoppingCart/Success",
                "https://localhost:7151/ShoppingCart/Cancel"
            );

            return Ok(new { url = sessionUrl });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Failed to create checkout session." });
        }
    }*/
}