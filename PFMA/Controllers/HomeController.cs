using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PFMA.Data;
using PFMA.Models;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using PFMA.Helpers;

namespace PFMA.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("~/Views/Shared/Error.cshtml", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        // GET: Dashboard
        public IActionResult Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Fetch user info
            var user = _context.Users.Find(userId.Value);
            if (user == null)
            {
                return RedirectToAction("Logout", "Account");
            }

            // Calculations
            var userTransactions = _context.Transactions.Where(t => t.UserId == userId.Value).ToList();
            
            decimal totalIncome = userTransactions.Where(t => t.Type == "Income").Sum(t => t.Amount);
            decimal totalExpense = userTransactions.Where(t => t.Type == "Expense").Sum(t => t.Amount);
            decimal totalBalance = totalIncome - totalExpense;

            // Monthly stats (for the latest month with data, e.g., October 2023 in seeded data)
            // If no transactions exist, use current month
            int targetMonth = DateTime.Now.Month;
            int targetYear = DateTime.Now.Year;
            if (userTransactions.Any())
            {
                var latestTx = userTransactions.OrderByDescending(t => t.Date).First();
                targetMonth = latestTx.Date.Month;
                targetYear = latestTx.Date.Year;
            }

            decimal monthlyIncome = userTransactions
                .Where(t => t.Type == "Income" && t.Date.Month == targetMonth && t.Date.Year == targetYear)
                .Sum(t => t.Amount);

            decimal monthlyExpense = userTransactions
                .Where(t => t.Type == "Expense" && t.Date.Month == targetMonth && t.Date.Year == targetYear)
                .Sum(t => t.Amount);

            // Savings Goal Progress
            var goals = _context.SavingsGoals.Where(g => g.UserId == userId.Value).ToList();
            decimal totalSavings = goals.Sum(g => g.CurrentAmount);
            decimal targetSavings = goals.Sum(g => g.TargetAmount);
            double savingsPercentage = targetSavings > 0 ? (double)(totalSavings / targetSavings) * 100 : 0;

            // Recent Transactions (top 4 as in screen)
            var recentTransactions = userTransactions
                .OrderByDescending(t => t.Date)
                .Take(4)
                .ToList();

            // Spending by Category (Expense Breakdown)
            var categoryBreakdown = userTransactions
                .Where(t => t.Type == "Expense" && t.Date.Month == targetMonth && t.Date.Year == targetYear)
                .GroupBy(t => t.Category)
                .Select(g => new {
                    Category = g.Key,
                    Amount = g.Sum(t => t.Amount)
                })
                .ToList();

            decimal totalMonthlyExpense = categoryBreakdown.Sum(c => c.Amount);
            
            // Format category data for frontend chart
            var categories = categoryBreakdown.Select(c => c.Category).ToArray();
            var categoryAmounts = categoryBreakdown.Select(c => (double)c.Amount).ToArray();
            var categoryPercentages = categoryBreakdown.Select(c => totalMonthlyExpense > 0 ? (double)Math.Round((c.Amount / totalMonthlyExpense) * 100, 1) : 0.0).ToArray();

            // Spending Trends (last 6 months)
            var trends = userTransactions
                .Where(t => t.Type == "Expense")
                .GroupBy(t => new { t.Date.Year, t.Date.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .Take(6)
                .Select(g => new {
                    Label = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM"),
                    Amount = g.Sum(t => t.Amount)
                })
                .ToList();

            // Neon trend chart: income and expenses of the last 12 months
            // (ending with the latest month that has transactions, empty months are shown as 0)
            DateTime lastTrendMonth = userTransactions.Any()
                ? new DateTime(userTransactions.Max(t => t.Date).Year, userTransactions.Max(t => t.Date).Month, 1)
                : new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

            var trendLabels12 = new List<string>();
            var trendIncome12 = new List<double>();
            var trendExpense12 = new List<double>();
            for (int i = 11; i >= 0; i--)
            {
                var m = lastTrendMonth.AddMonths(-i);
                trendLabels12.Add(m.ToString("MMM"));
                trendIncome12.Add((double)userTransactions
                    .Where(t => t.Type == "Income" && t.Date.Year == m.Year && t.Date.Month == m.Month)
                    .Sum(t => t.Amount));
                trendExpense12.Add((double)userTransactions
                    .Where(t => t.Type == "Expense" && t.Date.Year == m.Year && t.Date.Month == m.Month)
                    .Sum(t => t.Amount));
            }
            ViewBag.TrendLabels12 = trendLabels12.ToArray();
            ViewBag.TrendIncome12 = trendIncome12.ToArray();
            ViewBag.TrendExpense12 = trendExpense12.ToArray();

            // Daily flow (last 30 days) and weekly flow (last 12 weeks), ending at the latest transaction
            DateTime flowEnd = userTransactions.Any() ? userTransactions.Max(t => t.Date).Date : DateTime.Today;

            var flowDayLabels = new List<string>();
            var flowDayIncome = new List<double>();
            var flowDayExpense = new List<double>();
            for (int i = 29; i >= 0; i--)
            {
                DateTime day = flowEnd.AddDays(-i);
                flowDayLabels.Add(day.ToString("dd MMM"));
                flowDayIncome.Add((double)userTransactions.Where(t => t.Type == "Income" && t.Date.Date == day).Sum(t => t.Amount));
                flowDayExpense.Add((double)userTransactions.Where(t => t.Type == "Expense" && t.Date.Date == day).Sum(t => t.Amount));
            }

            int flowDaysSinceMonday = ((int)flowEnd.DayOfWeek + 6) % 7;
            DateTime flowLastWeekStart = flowEnd.AddDays(-flowDaysSinceMonday);
            var flowWeekLabels = new List<string>();
            var flowWeekIncome = new List<double>();
            var flowWeekExpense = new List<double>();
            for (int i = 11; i >= 0; i--)
            {
                DateTime weekStart = flowLastWeekStart.AddDays(-7 * i);
                DateTime weekEnd = weekStart.AddDays(7);
                flowWeekLabels.Add(weekStart.ToString("dd MMM"));
                flowWeekIncome.Add((double)userTransactions.Where(t => t.Type == "Income" && t.Date.Date >= weekStart && t.Date.Date < weekEnd).Sum(t => t.Amount));
                flowWeekExpense.Add((double)userTransactions.Where(t => t.Type == "Expense" && t.Date.Date >= weekStart && t.Date.Date < weekEnd).Sum(t => t.Amount));
            }

            ViewBag.FlowDayLabels = flowDayLabels.ToArray();
            ViewBag.FlowDayIncome = flowDayIncome.ToArray();
            ViewBag.FlowDayExpense = flowDayExpense.ToArray();
            ViewBag.FlowWeekLabels = flowWeekLabels.ToArray();
            ViewBag.FlowWeekIncome = flowWeekIncome.ToArray();
            ViewBag.FlowWeekExpense = flowWeekExpense.ToArray();

            // View Models / View Data
            ViewBag.TotalBalance = totalBalance;
            ViewBag.MonthlyIncome = monthlyIncome;
            ViewBag.MonthlyExpense = monthlyExpense;
            ViewBag.TotalSavings = totalSavings;
            ViewBag.TargetSavings = targetSavings;
            ViewBag.SavingsPercentage = Math.Round(savingsPercentage, 0);
            
            ViewBag.RecentTransactions = recentTransactions;
            
            ViewBag.ChartCategories = categories;
            ViewBag.ChartCategoryAmounts = categoryAmounts;
            ViewBag.ChartCategoryPercentages = categoryPercentages;

            ViewBag.TrendLabels = trends.Select(t => t.Label).ToArray();
            ViewBag.TrendAmounts = trends.Select(t => (double)t.Amount).ToArray();

            // Get categories list for the Add Transaction modal
            ViewBag.AllCategories = new string[] { "Food", "Transport", "Bills", "Housing", "Salary", "Shopping", "Entertainment", "Misc" };

            // Notifications/Alerts setup
            ViewBag.Recommendations = _context.AdvisorRecommendations
                .Where(r => r.ClientId == userId.Value)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefault();

            return View();
        }

        // GET: Home/Transactions (Full list page)
        public IActionResult Transactions(string searchQuery, string categoryFilter, string typeFilter)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var query = _context.Transactions.Where(t => t.UserId == userId.Value);

            if (!string.IsNullOrEmpty(searchQuery))
            {
                query = query.Where(t => t.Description.Contains(searchQuery) || (t.Notes != null && t.Notes.Contains(searchQuery)));
            }

            if (!string.IsNullOrEmpty(categoryFilter) && categoryFilter != "All")
            {
                query = query.Where(t => t.Category == categoryFilter);
            }

            if (!string.IsNullOrEmpty(typeFilter) && typeFilter != "All")
            {
                query = query.Where(t => t.Type == typeFilter);
            }

            var transactionsList = query.OrderByDescending(t => t.Date).ToList();

            ViewBag.SearchQuery = searchQuery;
            ViewBag.CategoryFilter = categoryFilter;
            ViewBag.TypeFilter = typeFilter;
            ViewBag.Categories = new string[] { "Food", "Transport", "Bills", "Housing", "Salary", "Shopping", "Entertainment", "Misc" };

            return View(transactionsList);
        }

        // POST: Home/AddTransaction
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddTransaction(Transaction model)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (ModelState.IsValid)
            {
                model.UserId = userId.Value;
                try
                {
                    _context.Transactions.Add(model);
                    _context.SaveChanges();
                }
                catch (Exception ex)
                {
                    return Content("ERROR: " + (ex.InnerException?.Message ?? ex.Message));
                }
                return RedirectToAction("Index");
            }

            return RedirectToAction("Index");
        }

        // POST: Home/EditTransaction
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditTransaction(int transactionId, string type, string category, string description, decimal amount, DateTime date, string? notes)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var tx = _context.Transactions.FirstOrDefault(t => t.TransactionId == transactionId && t.UserId == userId.Value);
            if (tx != null && amount > 0 && !string.IsNullOrWhiteSpace(category))
            {
                tx.Type = type == "Income" ? "Income" : "Expense";
                tx.Category = category.Trim();
                tx.Description = string.IsNullOrWhiteSpace(description) ? category.Trim() : description.Trim();
                tx.Amount = amount;
                tx.Date = date;
                tx.Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
                _context.SaveChanges();
            }

            return RedirectToAction("Transactions");
        }

        // POST: Home/DeleteTransaction
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteTransaction(int transactionId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var tx = _context.Transactions.FirstOrDefault(t => t.TransactionId == transactionId && t.UserId == userId.Value);
            if (tx != null)
            {
                _context.Transactions.Remove(tx);
                _context.SaveChanges();
            }

            return RedirectToAction("Transactions");
        }

        // GET: Home/Reports (Financial Analytics)
        public IActionResult Reports()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var transactions = _context.Transactions.Where(t => t.UserId == userId.Value).ToList();
            var data = ReportData.Build(transactions);

            ViewBag.TotalIncome = data.TotalIncome;
            ViewBag.TotalExpense = data.TotalExpense;
            ViewBag.NetSavings = data.NetSavings;
            ViewBag.SavingsRate = data.SavingsRate;
            ViewBag.TopCategory = data.TopCategory;
            ViewBag.TopCategoryPct = Math.Round(data.TopCategoryPercent, 0);
            ViewBag.Insights = data.Insights;

            ViewBag.ChartLabels = data.Months.Select(m => m.Label).ToArray();
            ViewBag.ChartIncome = data.Months.Select(m => (double)m.Income).ToArray();
            ViewBag.ChartExpense = data.Months.Select(m => (double)m.Expense).ToArray();

            ViewBag.TotalSaved = _context.SavingsGoals.Where(g => g.UserId == userId.Value).Sum(g => g.CurrentAmount);

            return View();
        }

        // GET: Home/ExportPdf - server-side PDF (no internet / JavaScript library needed)
        public IActionResult ExportPdf()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var userName = HttpContext.Session.GetString("UserName") ?? "User";
            var transactions = _context.Transactions
                .Where(t => t.UserId == userId.Value)
                .OrderByDescending(t => t.Date)
                .ThenByDescending(t => t.TransactionId)
                .ToList();

            CurrencyHelper.PdfSymbol.Value = CurrencyHelper.AsciiSymbol(HttpContext);
            var data = ReportData.Build(transactions);
            var bytes = PdfReportBuilder.Build(userName, data, transactions);

            return File(bytes, "application/pdf", $"FinanceFlow_Report_{DateTime.Now:yyyy-MM-dd}.pdf");
        }

        // GET: Home/ExportExcel - CSV file that opens in Excel and can be imported again
        public IActionResult ExportExcel()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var userName = HttpContext.Session.GetString("UserName") ?? "User";
            var transactions = _context.Transactions
                .Where(t => t.UserId == userId.Value)
                .OrderByDescending(t => t.Date)
                .ThenByDescending(t => t.TransactionId)
                .ToList();

            decimal totalIncome = transactions.Where(t => t.Type == "Income").Sum(t => t.Amount);
            decimal totalExpense = transactions.Where(t => t.Type == "Expense").Sum(t => t.Amount);

            var inv = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            sb.AppendLine("FinanceFlow - Financial Report");
            sb.AppendLine("Generated for," + CsvUtil.Escape(userName));
            sb.AppendLine("Generated on," + DateTime.Now.ToString("dd MMM yyyy HH:mm", inv));
            sb.AppendLine();
            sb.AppendLine("--- SUMMARY ---");
            sb.AppendLine("Total Income," + totalIncome.ToString("F2", inv));
            sb.AppendLine("Total Expenses," + totalExpense.ToString("F2", inv));
            sb.AppendLine("Net Savings," + (totalIncome - totalExpense).ToString("F2", inv));
            sb.AppendLine();
            sb.AppendLine("--- TRANSACTIONS ---");
            sb.AppendLine("Date,Type,Category,Description,Amount,Notes");
            foreach (var tx in transactions)
            {
                sb.AppendLine(string.Join(",",
                    tx.Date.ToString("yyyy-MM-dd", inv),
                    CsvUtil.Escape(tx.Type),
                    CsvUtil.Escape(tx.Category),
                    CsvUtil.Escape(tx.Description),
                    tx.Amount.ToString("F2", inv),
                    CsvUtil.Escape(tx.Notes)));
            }

            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            return File(bytes, "text/csv", $"FinanceFlow_Transactions_{DateTime.Now:yyyy-MM-dd}.csv");
        }

        // GET: Home/DownloadImportTemplate - sample CSV showing the expected format
        public IActionResult DownloadImportTemplate()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Date,Type,Category,Description,Amount,Notes");
            sb.AppendLine("2026-10-01,Income,Salary,Monthly Salary,50000.00,Main job");
            sb.AppendLine("2026-10-02,Expense,Food,Grocery Store,1250.50,Weekly groceries");
            sb.AppendLine("2026-10-03,Expense,Transport,Fuel,3000.00,");
            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            return File(bytes, "text/csv", "FinanceFlow_Import_Template.csv");
        }

        // POST: Home/ImportTransactions - imports a CSV file (also accepts the file created by Export to Excel)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportTransactions(IFormFile? file)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (file == null || file.Length == 0)
            {
                TempData["ImportError"] = "Please choose a CSV file first.";
                return RedirectToAction("Transactions");
            }

            if (file.Length > 2 * 1024 * 1024)
            {
                TempData["ImportError"] = "The file is too large (maximum 2 MB).";
                return RedirectToAction("Transactions");
            }

            if (!Path.GetExtension(file.FileName).Equals(".csv", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ImportError"] = "Only .csv files are supported. In Excel use File > Save As > CSV (Comma delimited).";
                return RedirectToAction("Transactions");
            }

            string text;
            using (var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8, true))
            {
                text = await reader.ReadToEndAsync();
            }
            text = text.TrimStart('\uFEFF');

            char delimiter = CsvUtil.DetectDelimiter(text);
            var rows = CsvUtil.Parse(text, delimiter);

            // Accepts our own template AND typical bank / JazzCash / EasyPaisa statements
            // (columns such as Date, Description/Narration/Details, Debit + Credit or Amount).
            int headerIndex = rows.FindIndex(r =>
            {
                var h = r.Select(cell => cell.Trim().ToLowerInvariant()).ToList();
                bool hasDate = FindCol(h, "date", "transaction date", "value date", "posting date") >= 0;
                bool hasAmount = FindCol(h, "amount") >= 0 ||
                    (FindCol(h, "debit", "withdrawal", "paid out", "money out") >= 0 &&
                     FindCol(h, "credit", "deposit", "paid in", "money in") >= 0);
                return hasDate && hasAmount;
            });

            if (headerIndex < 0)
            {
                TempData["ImportError"] = "Could not find the header row. The file needs a Date column and either an Amount column or Debit + Credit columns (Type, Category, Description and Notes are optional). Use Download Template.";
                return RedirectToAction("Transactions");
            }

            var header = rows[headerIndex].Select(cell => cell.Trim().ToLowerInvariant()).ToList();
            int iDate = FindCol(header, "date", "transaction date", "value date", "posting date");
            int iType = FindCol(header, "type", "transaction type");
            int iCategory = FindCol(header, "category");
            int iDescription = FindCol(header, "description", "narration", "details", "particulars", "remarks", "reference");
            int iAmount = FindCol(header, "amount");
            int iDebit = FindCol(header, "debit", "withdrawal", "paid out", "money out");
            int iCredit = FindCol(header, "credit", "deposit", "paid in", "money in");
            int iNotes = FindCol(header, "notes");

            var inv = CultureInfo.InvariantCulture;
            var existingKeys = _context.Transactions
                .Where(t => t.UserId == userId.Value)
                .AsEnumerable()
                .Select(t => TransactionKey(t.Date, t.Type, t.Category, t.Description, t.Amount))
                .ToHashSet();

            int imported = 0, duplicates = 0, invalid = 0;

            foreach (var row in rows.Skip(headerIndex + 1))
            {
                if (row.All(c => string.IsNullOrWhiteSpace(c))) continue;
                if (row.Count > 0 && row[0].TrimStart().StartsWith("---")) continue;

                string dateText = CellAt(row, iDate);
                string typeText = CellAt(row, iType).ToLowerInvariant();

                decimal amount = 0;
                bool amountOk = false;
                if (iAmount >= 0)
                {
                    amountOk = TryParseAmount(CellAt(row, iAmount), delimiter, out amount);
                }
                else
                {
                    // Bank / wallet statement with separate Debit and Credit columns
                    if (iDebit >= 0 && TryParseAmount(CellAt(row, iDebit), delimiter, out var debitValue) && debitValue != 0)
                    {
                        amount = debitValue;
                        typeText = "expense";
                        amountOk = true;
                    }
                    else if (iCredit >= 0 && TryParseAmount(CellAt(row, iCredit), delimiter, out var creditValue) && creditValue != 0)
                    {
                        amount = creditValue;
                        typeText = "income";
                        amountOk = true;
                    }
                }

                if (!TryParseDate(dateText, out var date) || !amountOk || amount == 0)
                {
                    invalid++;
                    continue;
                }

                string type;
                if (typeText == "income" || typeText == "credit" || typeText == "deposit" || typeText == "in") type = "Income";
                else if (typeText == "expense" || typeText == "debit" || typeText == "withdrawal" || typeText == "out") type = "Expense";
                else if (typeText == "") type = amount < 0 ? "Expense" : "Income";
                else
                {
                    invalid++;
                    continue;
                }

                amount = Math.Abs(amount);

                string category = CellAt(row, iCategory);
                if (category.Length == 0) category = "Misc";
                if (category.Length > 100) category = category.Substring(0, 100);

                string description = CellAt(row, iDescription);
                if (description.Length == 0) description = category;
                if (description.Length > 200) description = description.Substring(0, 200);

                string notes = CellAt(row, iNotes);
                if (notes.Length > 500) notes = notes.Substring(0, 500);

                var key = TransactionKey(date, type, category, description, amount);
                if (!existingKeys.Add(key))
                {
                    duplicates++;
                    continue;
                }

                _context.Transactions.Add(new Transaction
                {
                    UserId = userId.Value,
                    Date = date,
                    Type = type,
                    Category = category,
                    Description = description,
                    Amount = amount,
                    Notes = notes.Length == 0 ? null : notes
                });
                imported++;
            }

            if (imported > 0)
            {
                await _context.SaveChangesAsync();
                TempData["ImportSuccess"] = $"Imported {imported} transaction(s). Skipped {duplicates} duplicate(s) and {invalid} invalid row(s).";
            }
            else
            {
                TempData["ImportError"] = $"No transactions were imported. Duplicates: {duplicates}, invalid rows: {invalid}. Check the file format (Download Template).";
            }

            return RedirectToAction("Transactions");
        }

        // Finds a column by exact name first, then by "contains" (e.g. "Transaction Date")
        private static int FindCol(List<string> header, params string[] names)
        {
            foreach (var name in names)
            {
                int exact = header.IndexOf(name);
                if (exact >= 0) return exact;
            }
            foreach (var name in names)
            {
                int partial = header.FindIndex(h => h.Contains(name));
                if (partial >= 0) return partial;
            }
            return -1;
        }

        private static string CellAt(List<string> row, int index) =>
            index >= 0 && index < row.Count ? row[index].Trim() : "";

        private static string TransactionKey(DateTime date, string type, string category, string description, decimal amount) =>
            string.Join("|",
                date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                type,
                category.ToLowerInvariant(),
                description.ToLowerInvariant(),
                amount.ToString("F2", CultureInfo.InvariantCulture));

        private static bool TryParseDate(string text, out DateTime date)
        {
            var formats = new[]
            {
                "yyyy-MM-dd", "yyyy/MM/dd", "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "d-M-yyyy",
                "MM/dd/yyyy", "M/d/yyyy", "dd MMM yyyy", "d MMM yyyy", "yyyy-MM-dd HH:mm:ss", "yyyy-MM-ddTHH:mm:ss"
            };

            if (DateTime.TryParseExact(text, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            {
                return true;
            }
            return DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
        }

        private static bool TryParseAmount(string text, char delimiter, out decimal amount)
        {
            text = text.Replace("$", "").Replace("Rs.", "").Replace("Rs", "").Replace("PKR", "").Replace("USD", "")
                       .Replace("EUR", "").Replace("GBP", "").Replace("SAR", "").Replace("AED", "").Replace("INR", "")
                       .Replace("€", "").Replace("£", "").Replace("₹", "").Replace(" ", "");
            if (text.Length == 0) { amount = 0; return false; }

            // "1250,50" (comma as decimal separator) vs "1,250.50" (comma as thousands separator)
            if (text.Contains(',') && !text.Contains('.') && delimiter != ',')
            {
                text = text.Replace(',', '.');
            }
            else
            {
                text = text.Replace(",", "");
            }

            return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out amount);
        }
    }
}
