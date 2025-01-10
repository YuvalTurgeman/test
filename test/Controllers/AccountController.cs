using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using test.Data;
using test.Models;
using test.Enums;
using test.Services;
using test.ViewModels;
    
namespace test.Controllers
{
    public class AccountController : BaseController
    {
        private readonly UserDAL _userDAL;
        private readonly EmailService _emailService;
        
        public AccountController(UserDAL userDAL, EmailService emailService)
        {
            _userDAL = userDAL;
            _emailService = emailService; 
        }


        // GET: Account/Login
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, string returnUrl = null)
        {
            var user = await _userDAL.GetUserByEmailAsync(email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.Password))
            {
                ViewData["LoginError"] = "Invalid email or password.";
                return View();
            }

            // Create claims for authentication
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Permission.ToString())
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            // Explicitly store UserPermission in session
            HttpContext.Session.SetString("UserPermission", user.Permission.ToString());
            HttpContext.Session.SetInt32("UserId", user.Id);

            // Debugging: Ensure UserPermission is set
            Console.WriteLine($"UserPermission in Session: {HttpContext.Session.GetString("UserPermission")}");

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("UserHomePage", "Books");
        }


        // GET: Account/Register
        [HttpGet]
        // [Authorize(Roles = "Guest")]
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
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage);
                    ViewData["ErrorMessage"] = string.Join(" ", errors);
                    return View(user);
                }

                if (string.IsNullOrWhiteSpace(user.Username))
                {
                    ViewData["ErrorMessage"] = "Username is required.";
                    return View(user);
                }

                // Password validation
                var passwordRegex = new Regex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[#$^+=!*()@%&]).{8,}$");
                if (!passwordRegex.IsMatch(user.Password))
                {
                    ViewData["ErrorMessage"] = "Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character (#$^+=!*()@%&).";
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
                user.Permission = test.Enums.UserPermission.Customer;
                
                var createdUser = await _userDAL.CreateUserAsync(user);

                // Create claims and sign in
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, createdUser.Id.ToString()),
                    new Claim(ClaimTypes.Name, createdUser.Username),
                    new Claim(ClaimTypes.Email, createdUser.Email),
                    new Claim(ClaimTypes.Role, createdUser.Permission.ToString())
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                HttpContext.Session.SetInt32("UserId", createdUser.Id);
                
                return RedirectToAction("Login", "Account");
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
        
        
        public async Task<IActionResult> Logout()
        {
            // Clear authentication cookie
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Clear session
            HttpContext.Session.Clear();

            // Debugging: Ensure session is cleared
            Console.WriteLine("Session cleared on logout.");

            return RedirectToAction("Login", "Account");
        }
        
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }
        
        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            var user = await _userDAL.GetUserByEmailAsync(email);
            if (user == null)
            {
                // Don't disclose that the email doesn't exist
                TempData["Message"] = "If the email exists in our system, a reset link has been sent.";
                return RedirectToAction("ForgotPassword");
            }

            // Generate and save the reset token
            var token = Guid.NewGuid().ToString();
            user.ResetToken = token;
            user.ResetTokenExpires = DateTime.UtcNow.AddHours(1);
            await _userDAL.UpdateUserAsync(user);

            // Generate reset link
            var resetLink = Url.Action(
                "ResetPassword",
                "Account",
                new { token = token, email = user.Email },
                Request.Scheme);

            // Send email
            var emailBody = $@"
        <h1>Password Reset Request</h1>
        <p>Click the link below to reset your password:</p>
        <a href='{resetLink}'>Reset Password</a>
        <p>If you did not request this, please ignore this email.</p>";
            await _emailService.SendEmailAsync(user.Email, "Reset Your Password", emailBody);

            TempData["Message"] = "If the email exists in our system, a reset link has been sent.";
            return RedirectToAction("ForgotPassword");
        }
        
        
        [HttpGet]
        public IActionResult ResetPassword(string token, string email)
        {
            // Check if token or email is missing
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                TempData["Error"] = "Invalid password reset request.";
                return RedirectToAction("ForgotPassword");
            }

            // Pass the token and email to the view
            var model = new ResetPassword
            {
                Token = token,
                Email = email
            };

            return View(model); // Render the ResetPassword view
        }
        
        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPassword model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Invalid input. Please check the form and try again.";
                return View(model);
            }

            var user = await _userDAL.GetUserByEmailAsync(model.Email);
            if (user == null || user.ResetToken != model.Token || user.ResetTokenExpires < DateTime.UtcNow)
            {
                TempData["Error"] = "Invalid or expired reset token.";
                return RedirectToAction("ResetPassword", new { token = model.Token, email = model.Email });
            }

            // Update the user's password
            user.Password = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            user.ResetToken = null;
            user.ResetTokenExpires = null;
            await _userDAL.UpdateUserAsync(user);

            TempData["Success"] = "Your password has been reset successfully!";
            return RedirectToAction("Login");
        }


        
        // Admin Actions
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminUserManagement()
        {
            var users = await _userDAL.GetAllUsersAsync();
            return View(users);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult AdminCreateUser()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            if (await _userDAL.DeleteUserAsync(id))
            {
                return RedirectToAction("AdminUserManagement");
            }
            return NotFound();
        }
        
        // GET: Account/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied()
        {
            var user = User.Identity?.Name ?? "Anonymous";
            Console.WriteLine($"Access denied for user: {user}");
            return View();
        }
        
    }
}