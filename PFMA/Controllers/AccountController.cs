using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PFMA.Data;
using PFMA.Models;
using PFMA.Helpers;

namespace PFMA.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            // If already logged in, redirect to respective dashboard
            var role = HttpContext.Session.GetString("UserRole");
            if (!string.IsNullOrEmpty(role))
            {
                return RedirectToDashboard(role);
            }
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Email and Password are required.";
                return View();
            }

            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            if (user != null && PasswordHelper.VerifyPassword(password, user.PasswordHash))
            {
                // Set session variables
                HttpContext.Session.SetInt32("UserId", user.UserId);
                HttpContext.Session.SetString("UserName", user.Name);
                HttpContext.Session.SetString("UserEmail", user.Email);
                HttpContext.Session.SetString("UserRole", user.Role);
                HttpContext.Session.SetString("AccountType", user.AccountType ?? "Standard");
                HttpContext.Session.SetString("ProfilePicture", user.ProfilePictureUrl ?? "/images/default-avatar.png");

                return RedirectToDashboard(user.Role);
            }

            ViewBag.Error = "Invalid email or password.";
            return View();
        }

        // GET: Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (!string.IsNullOrEmpty(role))
            {
                return RedirectToDashboard(role);
            }
            return View();
        }

        // POST: Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(string name, string email, string password, string confirmPassword)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "All fields are required.";
                return View();
            }

            if (password != confirmPassword)
            {
                ViewBag.Error = "Passwords do not match.";
                return View();
            }

            var existingUser = _context.Users.FirstOrDefault(u => u.Email == email);
            if (existingUser != null)
            {
                ViewBag.Error = "An account with this email already exists.";
                return View();
            }

            // Create new standard user
            var newUser = new User
            {
                Name = name,
                Email = email,
                PasswordHash = PasswordHelper.HashPassword(password),
                Role = "User",
                AccountType = "Standard",
                ProfilePictureUrl = "/images/avatars/default.png"
            };

            _context.Users.Add(newUser);
            _context.SaveChanges();

            // Set session variables and log in directly
            HttpContext.Session.SetInt32("UserId", newUser.UserId);
            HttpContext.Session.SetString("UserName", newUser.Name);
            HttpContext.Session.SetString("UserEmail", newUser.Email);
            HttpContext.Session.SetString("UserRole", newUser.Role);
            HttpContext.Session.SetString("AccountType", newUser.AccountType);
            HttpContext.Session.SetString("ProfilePicture", newUser.ProfilePictureUrl);

            return RedirectToAction("Index", "Home");
        }

        // GET: Account/Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        private IActionResult RedirectToDashboard(string role)
        {
            if (role == "Admin")
                return RedirectToAction("Index", "Admin");
            else if (role == "Advisor")
                return RedirectToAction("Index", "Advisor");
            else
                return RedirectToAction("Index", "Home");
        }
    }
}
