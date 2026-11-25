using Microsoft.AspNetCore.Mvc;
using PFMA.Data;
using PFMA.Models;

namespace PFMA.Controllers
{
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Categories
        public IActionResult Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // First visit: create the default categories for this user
            if (!_context.Categories.Any(c => c.UserId == userId.Value))
            {
                var defaults = new[]
                {
                    ("Food", "Expense", "fa-mug-hot"),
                    ("Transport", "Expense", "fa-car"),
                    ("Bills", "Expense", "fa-bolt"),
                    ("Shopping", "Expense", "fa-cart-shopping"),
                    ("Housing", "Expense", "fa-house"),
                    ("Entertainment", "Expense", "fa-film"),
                    ("Health", "Expense", "fa-heart-pulse"),
                    ("Education", "Expense", "fa-book"),
                    ("Salary", "Income", "fa-money-bill-wave"),
                    ("Freelance", "Income", "fa-laptop-code")
                };

                foreach (var d in defaults)
                {
                    _context.Categories.Add(new Category { UserId = userId.Value, Name = d.Item1, Type = d.Item2, Icon = d.Item3 });
                }
                _context.SaveChanges();
            }

            var list = _context.Categories
                .Where(c => c.UserId == userId.Value)
                .OrderBy(c => c.Type)
                .ThenBy(c => c.Name)
                .ToList();

            return View(list);
        }

        // POST: Categories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(string name, string type, string icon)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (!string.IsNullOrWhiteSpace(name))
            {
                name = name.Trim();
                bool exists = _context.Categories.Any(c => c.UserId == userId.Value && c.Name == name);
                if (!exists)
                {
                    _context.Categories.Add(new Category
                    {
                        UserId = userId.Value,
                        Name = name.Length > 50 ? name.Substring(0, 50) : name,
                        Type = type == "Income" ? "Income" : "Expense",
                        Icon = string.IsNullOrWhiteSpace(icon) ? "fa-tag" : icon
                    });
                    _context.SaveChanges();
                }
            }

            return RedirectToAction("Index");
        }

        // POST: Categories/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, string name, string type, string icon)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var category = _context.Categories.FirstOrDefault(c => c.CategoryId == id && c.UserId == userId.Value);
            if (category != null && !string.IsNullOrWhiteSpace(name))
            {
                name = name.Trim();
                category.Name = name.Length > 50 ? name.Substring(0, 50) : name;
                category.Type = type == "Income" ? "Income" : "Expense";
                category.Icon = string.IsNullOrWhiteSpace(icon) ? category.Icon : icon;
                _context.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        // POST: Categories/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var category = _context.Categories.FirstOrDefault(c => c.CategoryId == id && c.UserId == userId.Value);
            if (category != null)
            {
                _context.Categories.Remove(category);
                _context.SaveChanges();
            }

            return RedirectToAction("Index");
        }
    }
}
