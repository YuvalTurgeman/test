using Microsoft.AspNetCore.Mvc;
using test.Data;
using test.Models;
using BCrypt.Net;

namespace test.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }
        
        // GET: Account/Login
        public IActionResult Login()
        {
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(string email, string password)
        {
            // Check if the user exists in the database
            var user = _context.users.SingleOrDefault(u => u.Email == email);
            if (user == null)
            {
                ViewData["LoginError"] = "Invalid email or password.";
                return View();
            }

            // Verify the password
            if (!BCrypt.Net.BCrypt.Verify(password, user.Password))
            {
                ViewData["LoginError"] = "Invalid email or password.";
                return View();
            }

            // Log the user in by setting a session or cookie
            // For example, setting a session:
            HttpContext.Session.SetInt32("UserId", user.Id);

            // Redirect to the user's profile or dashboard
            return RedirectToAction("ShowUser", user);
        }
        
        // GET: Account/Register
        public IActionResult Register()
        {
            return View();
        }

        // POST: Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(User user)
        {
            if (ModelState.IsValid)
            {
                // Check if the email is already registered
                if (_context.users.Any(u => u.Email == user.Email))
                {
                    ModelState.AddModelError("Email", "Email is already in use.");
                    return View(user);
                }

                // Hash the user's password
                user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
                user.Permission = UserPermission.Customer;
                
                // Add the user to the database
                _context.users.Add(user);
                _context.SaveChanges();

                // Redirect to the ShowUser action to display the newly registered user
                return RedirectToAction("ShowUser", user);
            }

            // If validation fails, return the form with errors
            return View(user);
        }

        // GET: Account/ShowUser
        public IActionResult ShowUser(User user)
        {
            
            return View(user);
        }
    }
}