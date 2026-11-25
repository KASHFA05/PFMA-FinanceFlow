using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PFMA.Data;
using PFMA.Models;
using PFMA.Helpers;

namespace PFMA.Controllers
{
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProfileController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Profile
        public IActionResult Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var user = _context.Users.Find(userId.Value);
            return View(user);
        }

        // POST: Profile/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangePassword(string currentPassword, string newPassword)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var user = _context.Users.Find(userId.Value);
            if (user != null)
            {
                if (PasswordHelper.VerifyPassword(currentPassword, user.PasswordHash))
                {
                    user.PasswordHash = PasswordHelper.HashPassword(newPassword);
                    _context.SaveChanges();
                    ViewBag.Message = "Password updated successfully.";
                }
                else
                {
                    ViewBag.Error = "Current password is incorrect.";
                }
            }

            return View("Index", user);
        }

        // POST: Profile/UpdateSecurity
        [HttpPost]
        public IActionResult UpdateSecurity(bool twoFactor, bool emailNotif, bool loginAlerts)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Json(new { success = false });
            }

            var user = _context.Users.Find(userId.Value);
            if (user != null)
            {
                user.TwoFactorEnabled = twoFactor;
                user.EmailNotificationsEnabled = emailNotif;
                user.LoginAlertsEnabled = loginAlerts;
                
                _context.SaveChanges();

                // Sync session
                HttpContext.Session.SetString("TwoFactorEnabled", twoFactor.ToString());
            }

            return Json(new { success = true });
        }

        // POST: Profile/SetCurrency - choose the currency shown across the app
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SetCurrency(string currency)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var code = CurrencyHelper.Normalize(currency);
            Response.Cookies.Append(CurrencyHelper.CookieName(HttpContext), code, new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax
            });

            TempData["CurrencyMessage"] = "Currency changed to " + code + ". All amounts now use this currency symbol.";
            return RedirectToAction("Index");
        }

        // POST: Profile/RequestDeletion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RequestDeletion()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var user = _context.Users.Find(userId.Value);
            if (user != null)
            {
                // Delete user's transactions, goals, bill reminders
                var transactions = _context.Transactions.Where(t => t.UserId == userId.Value);
                _context.Transactions.RemoveRange(transactions);

                var goals = _context.SavingsGoals.Where(g => g.UserId == userId.Value);
                _context.SavingsGoals.RemoveRange(goals);

                var reminders = _context.BillReminders.Where(r => r.UserId == userId.Value);
                _context.BillReminders.RemoveRange(reminders);

                _context.Users.Remove(user);
                _context.SaveChanges();

                HttpContext.Session.Clear();
                return RedirectToAction("Register", "Account");
            }

            return RedirectToAction("Index");
        }

        // POST: Profile/UploadAvatar
        [HttpPost]
        public IActionResult UploadAvatar(IFormFile avatar)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Json(new { success = false, message = "Unauthorized access." });
            }

            if (avatar == null || avatar.Length == 0)
            {
                return Json(new { success = false, message = "No file selected." });
            }

            // Validate file extension
            var extension = Path.GetExtension(avatar.FileName).ToLowerInvariant();
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            if (!allowedExtensions.Contains(extension))
            {
                return Json(new { success = false, message = "Invalid file type. Only JPG, JPEG, PNG, and GIF are allowed." });
            }

            // Validate file size (2 MB limit)
            if (avatar.Length > 2 * 1024 * 1024)
            {
                return Json(new { success = false, message = "File is too large. Maximum size allowed is 2 MB." });
            }

            var user = _context.Users.Find(userId.Value);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found." });
            }

            // Create directories if not existing
            var uploadsDir = Path.Combine(_webHostEnvironment.WebRootPath, "images", "avatars");
            if (!Directory.Exists(uploadsDir))
            {
                Directory.CreateDirectory(uploadsDir);
            }

            // Generate unique filename
            var fileName = $"avatar_{userId.Value}_{DateTime.UtcNow.Ticks}{extension}";
            var filePath = Path.Combine(uploadsDir, fileName);

            // Save file to disk
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                avatar.CopyTo(stream);
            }

            // Delete old avatar if it's not the default avatar and exists
            if (!string.IsNullOrEmpty(user.ProfilePictureUrl) &&
                !user.ProfilePictureUrl.Contains("default.png") &&
                !user.ProfilePictureUrl.Contains("admin.png") &&
                !user.ProfilePictureUrl.Contains("advisor.png") &&
                !user.ProfilePictureUrl.Contains("alex.png") &&
                !user.ProfilePictureUrl.Contains("jane.png"))
            {
                var oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, user.ProfilePictureUrl.TrimStart('/'));
                if (System.IO.File.Exists(oldFilePath))
                {
                    try
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                    catch
                    {
                        // Ignore delete failure of old image
                    }
                }
            }

            // Update database and save
            user.ProfilePictureUrl = $"/images/avatars/{fileName}";
            _context.SaveChanges();

            // Sync with session
            HttpContext.Session.SetString("ProfilePicture", user.ProfilePictureUrl);

            return Json(new { success = true, url = user.ProfilePictureUrl });
        }
    }
}
