using System.Globalization;
using PFMA.Models;

namespace PFMA.Helpers
{
    public class ReportInsight
    {
        public string Text { get; set; } = string.Empty;
        // "good" (green), "warn" (red) or "info" (blue)
        public string Kind { get; set; } = "info";
    }

    public class CategoryTotal
    {
        public string Category { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public double Percent { get; set; }
    }

    public class MonthTotal
    {
        public DateTime Month { get; set; }
        public decimal Income { get; set; }
        public decimal Expense { get; set; }
        public decimal Net => Income - Expense;
        public string Label => Month.ToString("MMM yyyy", CultureInfo.InvariantCulture);
    }

    /// <summary>Calculates all numbers shown on the Reports page and in the PDF/CSV exports.</summary>
    public class ReportData
    {
        public decimal TotalIncome { get; set; }
        public decimal TotalExpense { get; set; }
        public decimal NetSavings => TotalIncome - TotalExpense;
        public double SavingsRate { get; set; }
        public string TopCategory { get; set; } = "None";
        public decimal TopCategoryAmount { get; set; }
        public double TopCategoryPercent { get; set; }
        public List<CategoryTotal> Categories { get; set; } = new();
        public List<MonthTotal> Months { get; set; } = new();          // last 6 months that have data
        public List<ReportInsight> Insights { get; set; } = new();

        public static string Money(decimal v) =>
            (v < 0 ? "-" : "") + (CurrencyHelper.PdfSymbol.Value ?? "Rs.") + " " + Math.Abs(v).ToString("N2", CultureInfo.InvariantCulture);

        public static ReportData Build(IList<Transaction> transactions)
        {
            var d = new ReportData();

            d.TotalIncome = transactions.Where(t => t.Type == "Income").Sum(t => t.Amount);
            d.TotalExpense = transactions.Where(t => t.Type == "Expense").Sum(t => t.Amount);
            d.SavingsRate = d.TotalIncome > 0
                ? Math.Round((double)(d.NetSavings / d.TotalIncome) * 100, 1)
                : 0;

            // Expense per category
            d.Categories = transactions
                .Where(t => t.Type == "Expense")
                .GroupBy(t => t.Category)
                .Select(g => new CategoryTotal
                {
                    Category = g.Key,
                    Amount = g.Sum(t => t.Amount),
                    Percent = d.TotalExpense > 0 ? Math.Round((double)(g.Sum(t => t.Amount) / d.TotalExpense) * 100, 1) : 0
                })
                .OrderByDescending(c => c.Amount)
                .ToList();

            if (d.Categories.Count > 0)
            {
                d.TopCategory = d.Categories[0].Category;
                d.TopCategoryAmount = d.Categories[0].Amount;
                d.TopCategoryPercent = d.Categories[0].Percent;
            }

            // Monthly totals
            var allMonths = transactions
                .GroupBy(t => new DateTime(t.Date.Year, t.Date.Month, 1))
                .OrderBy(g => g.Key)
                .Select(g => new MonthTotal
                {
                    Month = g.Key,
                    Income = g.Where(t => t.Type == "Income").Sum(t => t.Amount),
                    Expense = g.Where(t => t.Type == "Expense").Sum(t => t.Amount)
                })
                .ToList();

            d.Months = allMonths.TakeLast(6).ToList();

            // ----- Insights -----
            if (transactions.Count == 0)
            {
                d.Insights.Add(new ReportInsight { Text = "No transactions yet. Add or import some transactions to see insights.", Kind = "info" });
                return d;
            }

            if (d.Categories.Count > 0)
            {
                d.Insights.Add(new ReportInsight
                {
                    Text = $"Top spending category: {d.TopCategory} ({d.TopCategoryPercent.ToString("0.#", CultureInfo.InvariantCulture)}% of all expenses).",
                    Kind = "info"
                });
            }

            if (allMonths.Count >= 2)
            {
                var latest = allMonths[allMonths.Count - 1];
                var previous = allMonths.Take(allMonths.Count - 1).TakeLast(6).ToList();
                decimal avgPrevious = previous.Average(m => m.Net);
                decimal diff = latest.Net - avgPrevious;
                bool better = diff >= 0;
                d.Insights.Add(new ReportInsight
                {
                    Text = $"In {latest.Label} you saved {Money(Math.Abs(diff))} {(better ? "more" : "less")} than your {previous.Count}-month average.",
                    Kind = better ? "good" : "warn"
                });

                // Biggest category change compared with the previous month
                var prevMonth = allMonths[allMonths.Count - 2];
                var curByCat = ExpensesByCategory(transactions, latest.Month);
                var prevByCat = ExpensesByCategory(transactions, prevMonth.Month);

                string? bestCat = null;
                double bestPct = 0;
                foreach (var kv in curByCat)
                {
                    if (prevByCat.TryGetValue(kv.Key, out var prevAmount) && prevAmount > 0)
                    {
                        double pct = (double)((kv.Value - prevAmount) / prevAmount) * 100;
                        if (bestCat == null || Math.Abs(pct) > Math.Abs(bestPct))
                        {
                            bestCat = kv.Key;
                            bestPct = pct;
                        }
                    }
                }

                if (bestCat != null)
                {
                    bool up = bestPct >= 0;
                    d.Insights.Add(new ReportInsight
                    {
                        Text = $"{bestCat} spending {(up ? "increased" : "decreased")} by {Math.Abs(bestPct).ToString("0", CultureInfo.InvariantCulture)}% compared to {prevMonth.Label}.",
                        Kind = up ? "warn" : "good"
                    });
                }
            }
            else if (allMonths.Count == 1)
            {
                var only = allMonths[0];
                d.Insights.Add(new ReportInsight
                {
                    Text = $"Net savings in {only.Label}: {Money(only.Net)}.",
                    Kind = only.Net >= 0 ? "good" : "warn"
                });
            }

            return d;
        }

        private static Dictionary<string, decimal> ExpensesByCategory(IList<Transaction> tx, DateTime monthStart)
        {
            return tx.Where(t => t.Type == "Expense" && t.Date.Year == monthStart.Year && t.Date.Month == monthStart.Month)
                     .GroupBy(t => t.Category)
                     .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));
        }
    }
}
