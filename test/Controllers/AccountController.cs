using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using test.Data;
using test.Models;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;

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

        /*// GET: Account/ShowUser
        public IActionResult ShowUser(User user)
        {

            return View(user);
        }*/

        // GET: Account/ShowUser
        public IActionResult ShowUser(int? id)
        {
            // Get the logged-in user's ID from session
            var userId = HttpContext.Session.GetInt32("UserId");
            
            // If no ID provided, use the logged-in user's ID
            id = id ?? userId;

            // If no session and no ID provided, redirect to login
            if (!id.HasValue)
            {
                return RedirectToAction("Login");
            }

            var user = _context.users.Find(id.Value);
            if (user == null)
            {
                return NotFound();
            }

            // Console.WriteLine(userId);
            // Console.WriteLine(user.Id);
            
            // Only allow viewing if it's the logged-in user
            // if (userId != user.Id)
            // {
            //     return Unauthorized();
            // }

            return View(user);
        }

        // POST: Account/EditUsername
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditUsername(int id, string newUsername)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue || userId.Value != id)
            {
                return RedirectToAction("Login");
            }

            var user = _context.users.Find(id);
            if (user == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(newUsername) || newUsername.Length > 50)
            {
                TempData["Error"] = "Username must be between 1 and 50 characters.";
                return RedirectToAction("ShowUser", new { id = user.Id });
            }

            user.Username = newUsername;
            _context.SaveChanges();

            TempData["Success"] = "Username updated successfully.";
            return RedirectToAction("ShowUser", new { id = user.Id });
        }

        // POST: Account/EditEmail
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditEmail(int id, string newEmail)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue || userId.Value != id)
            {
                return RedirectToAction("Login");
            }

            var user = _context.users.Find(id);
            if (user == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(newEmail) || !new EmailAddressAttribute().IsValid(newEmail))
            {
                TempData["Error"] = "Please enter a valid email address.";
                return RedirectToAction("ShowUser", new { id = user.Id });
            }

            if (_context.users.Any(u => u.Email == newEmail && u.Id != id))
            {
                TempData["Error"] = "This email is already in use.";
                return RedirectToAction("ShowUser", new { id = user.Id });
            }

            user.Email = newEmail;
            _context.SaveChanges();

            TempData["Success"] = "Email updated successfully.";
            return RedirectToAction("ShowUser", new { id = user.Id });
        }

        // POST: Account/EditPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditPassword(int id, string newPassword, string confirmPassword)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue || userId.Value != id)
            {
                return RedirectToAction("Login");
            }

            var user = _context.users.Find(id);
            if (user == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            {
                TempData["Error"] = "Password must be at least 6 characters long.";
                return RedirectToAction("ShowUser", new { id = user.Id });
            }

            if (newPassword != confirmPassword)
            {
                TempData["Error"] = "Passwords do not match.";
                return RedirectToAction("ShowUser", new { id = user.Id });
            }

            user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
            _context.SaveChanges();

            TempData["Success"] = "Password updated successfully.";
            return RedirectToAction("ShowUser", new { id = user.Id });
        }

        // Add a Logout action
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
        
        // GET: Account/AdminUserManagement
        [HttpGet]
        public async Task<IActionResult> AdminUserManagement()
        {
            var users = await _context.users.ToListAsync(); // Fetch all users from the database
            return View(users); // Pass the users to the view
        }
        
        // GET: Account/CreateUser
        [HttpGet]
        public IActionResult AdminCreateUser()
        {
            return View(); // Render the CreateUser.cshtml view
        }

// POST: Account/CreateUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminCreateUser(User user)
        {
            if (ModelState.IsValid)
            {
                if (_context.users.Any(u => u.Email == user.Email))
                {
                    ModelState.AddModelError("Email", "This email is already in use.");
                    return View(user);
                }

                user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password); // Hash the password
                _context.users.Add(user); // Add user to database
                await _context.SaveChangesAsync();
                return RedirectToAction("AdminUserManagement"); // Redirect to AdminUserManagement
            }

            return View(user); // Return the form with validation errors
        }
        
        // GET: Account/EditUser
        [HttpGet]
        public async Task<IActionResult> EditUser(int id)
        {
            var user = await _context.users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

// POST: Account/AdminEditUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(int id, User updatedUser)
        {
            if (id != updatedUser.Id)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                var user = await _context.users.FindAsync(id);
                if (user == null)
                {
                    return NotFound();
                }

                user.Username = updatedUser.Username;
                user.Email = updatedUser.Email;
                user.Permission = updatedUser.Permission;

                await _context.SaveChangesAsync();
                return RedirectToAction("AdminUserManagement");
            }

            return View(updatedUser);
        }

        // POST: Account/DeleteUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            _context.users.Remove(user);
            await _context.SaveChangesAsync();

            return RedirectToAction("AdminUserManagement");
        }



    }
}