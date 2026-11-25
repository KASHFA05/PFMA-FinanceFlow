using Microsoft.AspNetCore.Mvc;
using PFMA.Data;
using PFMA.Models;

namespace PFMA.Controllers
{
    public class BudgetsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BudgetsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Budgets
        public IActionResult Index(string? month)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (string.IsNullOrWhiteSpace(month))
            {
                month = DateTime.Now.ToString("yyyy-MM");
            }

            var budgets = _context.Budgets
                .Where(b => b.UserId == userId.Value && b.Month == month)
                .OrderBy(b => b.Category)
                .ToList();

            // Spent amount per category for the selected month (from the user's expense transactions)
            var expenses = _context.Transactions
                .Where(t => t.UserId == userId.Value && t.Type == "Expense")
                .ToList()
                .Where(t => t.Date.ToString("yyyy-MM") == month)
                .GroupBy(t => t.Category)
                .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));

            ViewBag.Month = month;
            ViewBag.Spent = expenses;
            ViewBag.TotalBudget = budgets.Sum(b => b.Amount);
            ViewBag.TotalSpent = budgets.Sum(b => expenses.ContainsKey(b.Category) ? expenses[b.Category] : 0m);

            return View(budgets);
        }

        // POST: Budgets/Add
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Add(string category, decimal amount, string month)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (string.IsNullOrWhiteSpace(month))
            {
                month = DateTime.Now.ToString("yyyy-MM");
            }

            if (!string.IsNullOrWhiteSpace(category) && amount > 0)
            {
                var existing = _context.Budgets.FirstOrDefault(b =>
                    b.UserId == userId.Value && b.Category == category && b.Month == month);

                if (existing != null)
                {
                    existing.Amount = amount;
                }
                else
                {
                    _context.Budgets.Add(new Budget
                    {
                        UserId = userId.Value,
                        Category = category.Trim(),
                        Amount = amount,
                        Month = month
                    });
                }
                _context.SaveChanges();
            }

            return RedirectToAction("Index", new { month });
        }

        // POST: Budgets/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id, string? month)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var budget = _context.Budgets.FirstOrDefault(b => b.BudgetId == id && b.UserId == userId.Value);
            if (budget != null)
            {
                _context.Budgets.Remove(budget);
                _context.SaveChanges();
            }

            return RedirectToAction("Index", new { month });
        }
    }
}
