using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PFMA.Data;
using PFMA.Models;

namespace PFMA.Controllers
{
    public class AdvisorController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdvisorController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index(int? clientId)
        {
            var advisorId = HttpContext.Session.GetInt32("UserId");
            var role = HttpContext.Session.GetString("UserRole");

            if (role != "Advisor" || advisorId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Get clients of this advisor
            var clients = _context.AdvisorClients
                .Where(ac => ac.AdvisorId == advisorId.Value)
                .Select(ac => ac.Client)
                .ToList();

            if (clients.Count == 0)
            {
                ViewBag.Error = "You do not have any assigned clients.";
                return View();
            }

            // Select default client if not specified
            int selectedClientId = clientId ?? clients.First()!.UserId;
            var selectedClient = _context.Users.Find(selectedClientId);

            if (selectedClient == null || !clients.Any(c => c != null && c.UserId == selectedClientId))
            {
                selectedClient = clients.First()!;
                selectedClientId = selectedClient.UserId;
            }

            // Fetch selected client statistics
            var clientTransactions = _context.Transactions.Where(t => t.UserId == selectedClientId).ToList();
            
            // Latest month calculation
            int targetMonth = DateTime.Now.Month;
            int targetYear = DateTime.Now.Year;
            if (clientTransactions.Any())
            {
                var latestTx = clientTransactions.OrderByDescending(t => t.Date).First();
                targetMonth = latestTx.Date.Month;
                targetYear = latestTx.Date.Year;
            }

            // Spending by Category (Expense Breakdown)
            var categoryBreakdown = clientTransactions
                .Where(t => t.Type == "Expense" && t.Date.Month == targetMonth && t.Date.Year == targetYear)
                .GroupBy(t => t.Category)
                .Select(g => new {
                    Category = g.Key,
                    Amount = g.Sum(t => t.Amount)
                })
                .ToList();

            decimal totalExpense = categoryBreakdown.Sum(c => c.Amount);

            // Savings Goals Progress
            var goals = _context.SavingsGoals.Where(g => g.UserId == selectedClientId).ToList();

            // Spending trends (last 6 months)
            var trends = clientTransactions
                .Where(t => t.Type == "Expense")
                .GroupBy(t => new { t.Date.Year, t.Date.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .Take(6)
                .Select(g => new {
                    Label = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM"),
                    Amount = g.Sum(t => t.Amount)
                })
                .ToList();

            // Weekly spending trends (last 8 weeks, a week starts on Monday)
            DateTime endDate = clientTransactions.Any() ? clientTransactions.Max(t => t.Date).Date : DateTime.Today;
            int daysSinceMonday = ((int)endDate.DayOfWeek + 6) % 7;
            DateTime lastWeekStart = endDate.AddDays(-daysSinceMonday);
            var weekLabels = new List<string>();
            var weekAmounts = new List<decimal>();
            for (int i = 7; i >= 0; i--)
            {
                DateTime weekStart = lastWeekStart.AddDays(-7 * i);
                DateTime weekEnd = weekStart.AddDays(7);
                weekLabels.Add(weekStart.ToString("dd MMM"));
                weekAmounts.Add(clientTransactions
                    .Where(t => t.Type == "Expense" && t.Date.Date >= weekStart && t.Date.Date < weekEnd)
                    .Sum(t => t.Amount));
            }
            ViewBag.WeekLabels = weekLabels.ToArray();
            ViewBag.WeekAmounts = weekAmounts.ToArray();

            // Current recommendations
            var recommendations = _context.AdvisorRecommendations
                .Where(r => r.AdvisorId == advisorId.Value && r.ClientId == selectedClientId)
                .OrderByDescending(r => r.CreatedAt)
                .ToList();

            // Populate View Bags
            ViewBag.Clients = clients;
            ViewBag.SelectedClient = selectedClient;
            ViewBag.CategoryBreakdown = categoryBreakdown.Select(cb => new {
                cb.Category,
                cb.Amount,
                Percentage = totalExpense > 0 ? (double)(cb.Amount / totalExpense) * 100 : 0
            }).ToList();
            
            ViewBag.Goals = goals;
            
            ViewBag.TrendLabels = trends.Select(t => t.Label).ToArray();
            ViewBag.TrendAmounts = trends.Select(t => t.Amount).ToArray();
            ViewBag.AverageSpend = trends.Any() ? Math.Round(trends.Average(t => t.Amount), 2) : 0;
            ViewBag.Recommendations = recommendations;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PostRecommendation(int clientId, string text)
        {
            var advisorId = HttpContext.Session.GetInt32("UserId");
            if (advisorId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (!string.IsNullOrEmpty(text))
            {
                var recommendation = new AdvisorRecommendation
                {
                    AdvisorId = advisorId.Value,
                    ClientId = clientId,
                    Text = text,
                    CreatedAt = DateTime.Now
                };

                _context.AdvisorRecommendations.Add(recommendation);
                _context.SaveChanges();
            }

            return RedirectToAction("Index", new { clientId = clientId });
        }
    }
}
