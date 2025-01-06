using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using test.Data;
using test.Models;

namespace test.Controllers
{
   [Authorize(Roles = "Admin")]
   public class DiscountController : Controller
   {
       private readonly DiscountDAL _discountDAL;
       private readonly BookDAL _bookDAL;

       public DiscountController(DiscountDAL discountDAL, BookDAL bookDAL)
       {
           _discountDAL = discountDAL;
           _bookDAL = bookDAL;
       }

       [HttpGet]
       public async Task<IActionResult> Create(int? id)
       {
           if (!id.HasValue)
               return RedirectToAction("AdminBooks", "Books"); 

           var book = await _bookDAL.GetBookByIdAsync(id.Value);
           if (book == null)
           {
               TempData["Error"] = "Book not found";
               return RedirectToAction("AdminBooks", "Books");
           }

           var hasDiscount = await _discountDAL.HasActiveDiscountAsync(id.Value);
           if (hasDiscount)
           {
               TempData["Error"] = "Book already has an active discount";
               return RedirectToAction("AdminBooks", "Books");
           }

           ViewData["BookTitle"] = book.Title;
           ViewBag.BookId = id;
           return View();
       }

       [HttpPost]
       [ValidateAntiForgeryToken]
       public async Task<IActionResult> Create(int BookId, decimal DiscountAmount, DateTime StartDate, DateTime EndDate)
       {
           try
           {
               var book = await _bookDAL.GetBookByIdAsync(BookId);
               if (book == null)
               {
                   ModelState.AddModelError("", "Book not found");
                   return View();
               }

               ViewData["BookTitle"] = book.Title;
               ViewBag.BookId = BookId;

               if ((EndDate - StartDate).TotalDays > 7)
               {
                   ModelState.AddModelError("", "Discount period cannot exceed 7 days");
                   return View();
               }

               var discount = new DiscountModel
               {
                   BookId = BookId,
                   DiscountAmount = DiscountAmount,
                   StartDate = DateTime.SpecifyKind(StartDate, DateTimeKind.Utc),
                   EndDate = DateTime.SpecifyKind(EndDate, DateTimeKind.Utc),
                   IsActive = true
               };

               await _discountDAL.CreateDiscountAsync(discount);
               return RedirectToAction("AdminBooks", "Books");
           }
           catch (Exception ex)
           {
               ModelState.AddModelError("", $"Error creating discount: {ex.Message}");
               return View();
           }
       }
       [HttpGet]
       public async Task<IActionResult> Edit(int? id)
       {
           if (!id.HasValue)
               return NotFound();

           var discount = await _discountDAL.GetDiscountAsync(id.Value);
           if (discount == null)
               return NotFound();

           ViewData["BookTitle"] = discount.Book?.Title;
           return View(discount);
       }

       [HttpPost]
       [ValidateAntiForgeryToken]
       public async Task<IActionResult> Edit(int id, DiscountModel discount)
       {
           if (id != discount.Id)
               return NotFound();

           if (!ModelState.IsValid)
               return View(discount);

           try
           {
               if ((discount.EndDate - discount.StartDate).TotalDays > 7)
               {
                   ModelState.AddModelError("", "Discount period cannot exceed 7 days");
                   return View(discount);
               }

               var overlappingDiscount = await _discountDAL.GetDiscountsByBookAsync(discount.BookId);
               if (overlappingDiscount.Any(d => d.Id != id && d.IsActive))
               {
                   ModelState.AddModelError("", "Another active discount exists for this book");
                   return View(discount);
               }

               await _discountDAL.UpdateDiscountAsync(discount);
               return RedirectToAction("AdminBooks", "Books");
           }
           catch (Exception ex)
           {
               ModelState.AddModelError("", $"Error updating discount: {ex.Message}");
               return View(discount);
           }
       }

       [HttpPost]
       [ValidateAntiForgeryToken]
       public async Task<IActionResult> Delete(int id)
       {
           await _discountDAL.DeleteDiscountAsync(id);
           return RedirectToAction("AdminBooks", "Books");
       }
   }
}