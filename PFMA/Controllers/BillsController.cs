using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PFMA.Data;
using PFMA.Models;

namespace PFMA.Controllers
{
    public class BillsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BillsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Bills/Index
        public IActionResult Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var reminders = _context.BillReminders
                .Where(r => r.UserId == userId.Value)
                .OrderBy(r => r.DueDate)
                .ToList();

            // Calculate card metrics
            decimal totalUpcoming = reminders.Where(r => r.Status == "Upcoming").Sum(r => r.Amount);
            decimal overdueAmount = reminders.Where(r => r.Status == "Overdue").Sum(r => r.Amount);
            decimal paidThisMonth = reminders.Where(r => r.Status == "Paid" && r.DueDate.Month == DateTime.Now.Month).Sum(r => r.Amount);

            ViewBag.TotalUpcoming = totalUpcoming;
            ViewBag.OverdueAmount = overdueAmount;
            ViewBag.PaidThisMonth = paidThisMonth;

            return View(reminders);
        }

        // POST: Bills/AddReminder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddReminder(BillReminder model)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (ModelState.IsValid)
            {
                model.UserId = userId.Value;
                _context.BillReminders.Add(model);
                _context.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        // POST: Bills/ToggleStatus
        [HttpPost]
        public IActionResult ToggleStatus(int id, string status)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Unauthorized();
            }

            var reminder = _context.BillReminders.FirstOrDefault(r => r.ReminderId == id && r.UserId == userId.Value);
            if (reminder != null)
            {
                reminder.Status = status;
                _context.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        // POST: Bills/DeleteReminder
        [HttpPost]
        public IActionResult DeleteReminder(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Unauthorized();
            }

            var reminder = _context.BillReminders.FirstOrDefault(r => r.ReminderId == id && r.UserId == userId.Value);
            if (reminder != null)
            {
                _context.BillReminders.Remove(reminder);
                _context.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        // POST: Bills/EditReminder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditReminder(int reminderId, string title, string category, string provider, decimal amount, DateTime dueDate)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var reminder = _context.BillReminders.FirstOrDefault(r => r.ReminderId == reminderId && r.UserId == userId.Value);
            if (reminder != null && !string.IsNullOrWhiteSpace(title) && amount > 0)
            {
                reminder.Title = title.Trim();
                reminder.Category = string.IsNullOrWhiteSpace(category) ? reminder.Category : category;
                reminder.Provider = string.IsNullOrWhiteSpace(provider) ? reminder.Provider : provider.Trim();
                reminder.Amount = amount;
                reminder.DueDate = dueDate;
                _context.SaveChanges();
            }

            return RedirectToAction("Index");
        }
    }
}
