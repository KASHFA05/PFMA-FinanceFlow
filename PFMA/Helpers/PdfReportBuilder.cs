using System.Globalization;
using PFMA.Models;

namespace PFMA.Helpers
{
    /// <summary>Builds the "Financial Analytics Report" PDF (same look as the original FinanceFlow report).</summary>
    public static class PdfReportBuilder
    {
        private const double Left = 15;
        private const double Right = 195;
        private const double BottomLimit = 272;
        private const int MaxTransactionRows = 500;

        private static readonly Rgb Dark = new(30, 41, 59);
        private static readonly Rgb White = new(255, 255, 255);
        private static readonly Rgb Body = new(80, 80, 80);
        private static readonly Rgb Gray = new(150, 150, 150);
        private static readonly Rgb LineGray = new(220, 220, 220);
        private static readonly Rgb HeadFill = new(241, 245, 249);
        private static readonly Rgb ZebraFill = new(248, 250, 252);
        private static readonly Rgb Green = new(6, 78, 59);
        private static readonly Rgb Red = new(153, 27, 27);
        private static readonly Rgb Teal = new(11, 181, 162);

        public static byte[] Build(string userName, ReportData data, IList<Transaction> transactions)
        {
            var doc = new PdfDocumentBuilder();
            var page = NewPage(doc, userName);
            double y = 40;

            // ---- Financial Summary ----
            page.Text(Left, y, "Financial Summary", 12, true, Dark);
            y += 8;

            var cards = new[]
            {
                (Label: "Total Income",   Value: ReportData.Money(data.TotalIncome),  Color: Green),
                (Label: "Total Expenses", Value: ReportData.Money(data.TotalExpense), Color: Red),
                (Label: "Net Savings",    Value: ReportData.Money(data.NetSavings),   Color: Dark),
                (Label: "Savings Rate",   Value: data.SavingsRate.ToString("0.0", CultureInfo.InvariantCulture) + "%", Color: Teal)
            };

            double cardW = (PdfDocumentBuilder.PageWidthMm - Left * 2 - 9) / 4;
            for (int i = 0; i < cards.Length; i++)
            {
                double cx = Left + i * (cardW + 3);
                page.FillRect(cx, y, cardW, 22, cards[i].Color, 3);
                page.Text(cx + 3, y + 7, cards[i].Label, 7, false, White);
                page.Text(cx + 3, y + 17, cards[i].Value, 10, true, White);
            }
            y += 32;

            // ---- Key Insights ----
            page.Text(Left, y, "Key Insights", 12, true, Dark);
            y += 7;
            foreach (var insight in data.Insights)
            {
                var lines = PdfFont.Wrap(insight.Text, Right - Left - 8, 9, false);
                for (int i = 0; i < lines.Count; i++)
                {
                    if (i == 0) page.Text(Left + 2, y, "\u2022", 9, false, Body);
                    page.Text(Left + 6, y, lines[i], 9, false, Body);
                    y += 5;
                }
                y += 1;
            }
            y += 6;

            // ---- Spending by Category ----
            if (data.Categories.Count > 0)
            {
                Ensure(doc, ref page, ref y, 30, userName);
                page.Text(Left, y, "Spending by Category", 12, true, Dark);
                y += 5;

                DrawHeaderRow(page, y, new[] { ("Category", Left + 2, "left"), ("Amount", 150.0, "right"), ("Share", Right - 2, "right") });
                y += 7;

                int row = 0;
                foreach (var c in data.Categories.Take(10))
                {
                    Ensure(doc, ref page, ref y, 7, userName);
                    if (row % 2 == 1) page.FillRect(Left, y, Right - Left, 6.5, ZebraFill);
                    page.Text(Left + 2, y + 4.5, PdfFont.Fit(c.Category, 90, 8, false), 8, false, Body);
                    page.Text(150, y + 4.5, ReportData.Money(c.Amount), 8, false, Body, "right");
                    page.Text(Right - 2, y + 4.5, c.Percent.ToString("0.0", CultureInfo.InvariantCulture) + "%", 8, false, Body, "right");
                    y += 6.5;
                    row++;
                }
                y += 8;
            }

            // ---- Transactions ----
            Ensure(doc, ref page, ref y, 30, userName);
            page.Text(Left, y, "Transactions", 12, true, Dark);
            y += 5;

            var headCols = new[] { ("Date", Left + 2, "left"), ("Type", 42.0, "left"), ("Category", 62.0, "left"), ("Description", 92.0, "left"), ("Amount", Right - 2, "right") };
            DrawHeaderRow(page, y, headCols);
            y += 7;

            if (transactions.Count == 0)
            {
                page.Text(Left + 2, y + 4.5, "No transactions recorded.", 8, false, Gray);
                y += 7;
            }

            int n = 0;
            foreach (var t in transactions.Take(MaxTransactionRows))
            {
                if (y + 6.5 > BottomLimit)
                {
                    page = NewPage(doc, userName);
                    y = 40;
                    DrawHeaderRow(page, y, headCols);
                    y += 7;
                }

                if (n % 2 == 1) page.FillRect(Left, y, Right - Left, 6.5, ZebraFill);

                bool income = t.Type == "Income";
                string amount = (income ? "+" : "-") + ReportData.Money(t.Amount);

                page.Text(Left + 2, y + 4.5, t.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), 8, false, Body);
                page.Text(42, y + 4.5, t.Type ?? "", 8, false, Body);
                page.Text(62, y + 4.5, PdfFont.Fit(t.Category ?? "", 28, 8, false), 8, false, Body);
                page.Text(92, y + 4.5, PdfFont.Fit(t.Description ?? "", 72, 8, false), 8, false, Body);
                page.Text(Right - 2, y + 4.5, amount, 8, true, income ? Green : Red, "right");
                y += 6.5;
                n++;
            }

            if (transactions.Count > MaxTransactionRows)
            {
                y += 2;
                Ensure(doc, ref page, ref y, 7, userName);
                page.Text(Left + 2, y + 4.5, $"Showing the latest {MaxTransactionRows} of {transactions.Count} transactions. Use Export to Excel for the full list.", 8, false, Gray);
            }

            // ---- Footer on every page ----
            int total = doc.Pages.Count;
            for (int i = 0; i < total; i++)
            {
                var p = doc.Pages[i];
                p.Line(Left, 282, Right, 282, LineGray, 0.2);
                p.Text(Left, 289, "FinanceFlow \u2022 Confidential Financial Report", 8, false, Gray);
                p.Text(Right, 289, $"Page {i + 1} of {total}", 8, false, Gray, "right");
            }

            return doc.Build();
        }

        private static PdfPage NewPage(PdfDocumentBuilder doc, string userName)
        {
            var page = doc.AddPage();
            page.FillRect(0, 0, PdfDocumentBuilder.PageWidthMm, 28, Dark);
            page.Text(Left, 12, "FinanceFlow", 18, true, White);
            page.Text(Left, 20, "Financial Analytics Report", 9, false, White);
            page.Text(Right, 12, PdfFont.Fit("Prepared for: " + userName, 90, 9, false), 9, false, White, "right");
            page.Text(Right, 20, "Generated: " + DateTime.Now.ToString("dd MMM yyyy", CultureInfo.InvariantCulture), 9, false, White, "right");
            return page;
        }

        private static void Ensure(PdfDocumentBuilder doc, ref PdfPage page, ref double y, double needed, string userName)
        {
            if (y + needed > BottomLimit)
            {
                page = NewPage(doc, userName);
                y = 40;
            }
        }

        private static void DrawHeaderRow(PdfPage page, double y, (string Text, double X, string Align)[] columns)
        {
            page.FillRect(Left, y, Right - Left, 7, HeadFill);
            foreach (var c in columns)
            {
                page.Text(c.X, y + 4.8, c.Text, 8, true, Dark, c.Align);
            }
        }
    }
}
