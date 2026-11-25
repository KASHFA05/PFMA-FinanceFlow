using Microsoft.AspNetCore.Mvc;
using PFMA.Data;
using PFMA.Models;

namespace PFMA.Controllers
{
    public class DebtsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DebtsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Debts
        public IActionResult Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var debts = _context.Debts
                .Where(d => d.UserId == userId.Value)
                .OrderBy(d => d.DueDate)
                .ToList();

            ViewBag.TotalToPay = debts.Where(d => d.Type == "To Pay").Sum(d => d.Remaining);
            ViewBag.TotalToReceive = debts.Where(d => d.Type == "To Receive").Sum(d => d.Remaining);

            return View(debts);
        }

        // POST: Debts/Add
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Add(string title, string type, decimal totalAmount, decimal clearedAmount, DateTime dueDate)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (!string.IsNullOrWhiteSpace(title) && totalAmount > 0)
            {
                _context.Debts.Add(new Debt
                {
                    UserId = userId.Value,
                    Title = title.Trim(),
                    Type = type == "To Receive" ? "To Receive" : "To Pay",
                    TotalAmount = totalAmount,
                    ClearedAmount = Math.Min(Math.Max(clearedAmount, 0), totalAmount),
                    DueDate = dueDate
                });
                _context.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        // POST: Debts/Update  (edit details and the cleared amount)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Update(int id, string title, string type, decimal totalAmount, decimal clearedAmount, DateTime dueDate)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var debt = _context.Debts.FirstOrDefault(d => d.DebtId == id && d.UserId == userId.Value);
            if (debt != null && !string.IsNullOrWhiteSpace(title) && totalAmount > 0)
            {
                debt.Title = title.Trim();
                debt.Type = type == "To Receive" ? "To Receive" : "To Pay";
                debt.TotalAmount = totalAmount;
                debt.ClearedAmount = Math.Min(Math.Max(clearedAmount, 0), totalAmount);
                debt.DueDate = dueDate;
                _context.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        // POST: Debts/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var debt = _context.Debts.FirstOrDefault(d => d.DebtId == id && d.UserId == userId.Value);
            if (debt != null)
            {
                _context.Debts.Remove(debt);
                _context.SaveChanges();
            }

            return RedirectToAction("Index");
        }
    }
}
