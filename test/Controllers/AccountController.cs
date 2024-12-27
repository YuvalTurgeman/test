using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using test.Data;
using test.Models;

namespace test.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserDAL _userDAL;
        
        public AccountController(UserDAL userDAL)
        {
            _userDAL = userDAL;
        }

        // GET: Account/Login
        public IActionResult Login()
        {
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password)
        {
            var user = await _userDAL.GetUserByEmailAsync(email);
            if (user == null)
            {
                ViewData["LoginError"] = "Invalid email or password.";
                return View();
            }

            if (!BCrypt.Net.BCrypt.Verify(password, user.Password))
            {
                ViewData["LoginError"] = "Invalid email or password.";
                return View();
            }

            HttpContext.Session.SetInt32("UserId", user.Id);
            return RedirectToAction("ShowUser", new { id = user.Id });
        }

        // GET: Account/Register
        public IActionResult Register()
        {
            return View();
        }
        
        //POST: Account/Register
        [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(User user, string confirmPassword)
    {
        try
        {
            
            if (string.IsNullOrWhiteSpace(user.Username))
            {
                ViewData["ErrorMessage"] = "Username is required.";
                return View(user);
            }
            

            if (user.Password != confirmPassword)
            {
                ViewData["ErrorMessage"] = "Passwords do not match.";
                return View(user);
            }

            var isEmailUnique = await _userDAL.IsEmailUniqueAsync(user.Email);

            if (!isEmailUnique)
            {
                ViewData["ErrorMessage"] = "This email is already registered.";
                return View(user);
            }

            // Hash password and set default permission
            user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
            user.Permission = UserPermission.Customer;
            

            // Create the user
            var createdUser = await _userDAL.CreateUserAsync(user);
            

            // Set session
            HttpContext.Session.SetInt32("UserId", createdUser.Id);
            HttpContext.Session.SetString("UserRole", createdUser.Permission.ToString());

            return RedirectToAction("UserHomePage", "Books");
        }
        catch (Exception ex)
        {
            ViewData["ErrorMessage"] = "An error occurred during registration. Please try again.";
            return View(user);
        }
    }

        // GET: Account/ShowUser
        public async Task<IActionResult> ShowUser(int? id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            id = id ?? userId;

            if (!id.HasValue)
            {
                return RedirectToAction("Login");
            }

            var user = await _userDAL.GetUserByIdAsync(id.Value);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: Account/EditUsername
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUsername(int id, string newUsername)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue || userId.Value != id)
            {
                return RedirectToAction("Login");
            }

            var user = await _userDAL.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(newUsername) || newUsername.Length > 50)
            {
                TempData["Error"] = "Username must be between 1 and 50 characters.";
                return RedirectToAction("ShowUser", new { id });
            }

            user.Username = newUsername;
            await _userDAL.UpdateUserAsync(user);

            TempData["Success"] = "Username updated successfully.";
            return RedirectToAction("ShowUser", new { id });
        }

        // POST: Account/EditEmail
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEmail(int id, string newEmail)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue || userId.Value != id)
            {
                return RedirectToAction("Login");
            }

            if (string.IsNullOrWhiteSpace(newEmail) || !new EmailAddressAttribute().IsValid(newEmail))
            {
                TempData["Error"] = "Please enter a valid email address.";
                return RedirectToAction("ShowUser", new { id });
            }

            if (!await _userDAL.IsEmailUniqueAsync(newEmail))
            {
                TempData["Error"] = "This email is already in use.";
                return RedirectToAction("ShowUser", new { id });
            }

            var user = await _userDAL.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            user.Email = newEmail;
            await _userDAL.UpdateUserAsync(user);

            TempData["Success"] = "Email updated successfully.";
            return RedirectToAction("ShowUser", new { id });
        }

        // POST: Account/EditPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPassword(int id, string newPassword, string confirmPassword)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue || userId.Value != id)
            {
                return RedirectToAction("Login");
            }

            var user = await _userDAL.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            {
                TempData["Error"] = "Password must be at least 6 characters long.";
                return RedirectToAction("ShowUser", new { id });
            }

            if (newPassword != confirmPassword)
            {
                TempData["Error"] = "Passwords do not match.";
                return RedirectToAction("ShowUser", new { id });
            }

            user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _userDAL.UpdateUserAsync(user);

            TempData["Success"] = "Password updated successfully.";
            return RedirectToAction("ShowUser", new { id });
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // Admin Actions
        [HttpGet]
        public async Task<IActionResult> AdminUserManagement()
        {
            var users = await _userDAL.GetAllUsersAsync();
            return View(users);
        }

        [HttpGet]
        public IActionResult AdminCreateUser()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminCreateUser(User user)
        {
            if (ModelState.IsValid)
            {
                if (!await _userDAL.IsEmailUniqueAsync(user.Email))
                {
                    ModelState.AddModelError("Email", "This email is already in use.");
                    return View(user);
                }

                user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
                await _userDAL.CreateUserAsync(user);
                return RedirectToAction("AdminUserManagement");
            }
            return View(user);
        }

        [HttpGet]
        public async Task<IActionResult> EditUser(int id)
        {
            var user = await _userDAL.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

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
                var user = await _userDAL.GetUserByIdAsync(id);
                if (user == null)
                {
                    return NotFound();
                }

                user.Username = updatedUser.Username;
                user.Email = updatedUser.Email;
                user.Permission = updatedUser.Permission;

                await _userDAL.UpdateUserAsync(user);
                return RedirectToAction("AdminUserManagement");
            }
            return View(updatedUser);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int id)
        {
            if (await _userDAL.DeleteUserAsync(id))
            {
                return RedirectToAction("AdminUserManagement");
            }
            return NotFound();
        }
    }
}