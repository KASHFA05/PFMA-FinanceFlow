using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PFMA.Data;
using PFMA.Models;

namespace PFMA.Controllers
{
    public class GoalsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public GoalsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Goals
        public IActionResult Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var goals = _context.SavingsGoals
                .Where(g => g.UserId == userId.Value)
                .ToList();

            decimal totalSaved = goals.Sum(g => g.CurrentAmount);
            ViewBag.TotalSaved = totalSaved;
            ViewBag.GoalsCount = goals.Count;

            return View(goals);
        }

        // POST: Goals/AddMoney
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddMoney(int goalId, decimal amount)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var goal = _context.SavingsGoals.FirstOrDefault(g => g.GoalId == goalId && g.UserId == userId.Value);
            if (goal != null && amount > 0)
            {
                goal.CurrentAmount += amount;

                // Add a mock transaction showing transfer to savings
                var tx = new Transaction
                {
                    UserId = userId.Value,
                    Amount = amount,
                    Type = "Expense",
                    Category = "Bills",
                    Description = $"Savings Transfer - {goal.GoalName}",
                    Date = DateTime.Now,
                    Notes = $"Deposited fund to {goal.GoalName} goal."
                };

                _context.Transactions.Add(tx);
                _context.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        // POST: Goals/AddGoal
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddGoal(string goalName, decimal targetAmount, decimal currentAmount, DateTime? targetDate)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (!string.IsNullOrEmpty(goalName) && targetAmount > 0)
            {
                var goal = new SavingsGoal
                {
                    UserId = userId.Value,
                    GoalName = goalName,
                    TargetAmount = targetAmount,
                    CurrentAmount = currentAmount >= 0 ? currentAmount : 0,
                    TargetDate = targetDate
                };

                _context.SavingsGoals.Add(goal);
                _context.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        // POST: Goals/EditGoal
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditGoal(int goalId, string goalName, decimal targetAmount, decimal currentAmount, DateTime? targetDate)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var goal = _context.SavingsGoals.FirstOrDefault(g => g.GoalId == goalId && g.UserId == userId.Value);
            if (goal != null && !string.IsNullOrWhiteSpace(goalName) && targetAmount > 0)
            {
                goal.GoalName = goalName.Trim();
                goal.TargetAmount = targetAmount;
                goal.CurrentAmount = currentAmount >= 0 ? currentAmount : 0;
                goal.TargetDate = targetDate;
                _context.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        // POST: Goals/DeleteGoal
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteGoal(int goalId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var goal = _context.SavingsGoals.FirstOrDefault(g => g.GoalId == goalId && g.UserId == userId.Value);
            if (goal != null)
            {
                _context.SavingsGoals.Remove(goal);
                _context.SaveChanges();
            }

            return RedirectToAction("Index");
        }
    }
}
