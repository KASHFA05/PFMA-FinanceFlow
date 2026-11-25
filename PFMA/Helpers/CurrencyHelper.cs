using System.Threading;
using Microsoft.AspNetCore.Http;

namespace PFMA.Helpers
{
    // Per-user display currency (stored in a cookie, so no database change is needed).
    public static class CurrencyHelper
    {
        public const string DefaultCode = "PKR";
        private const string CookiePrefix = "ff_currency_";

        // Code, symbol shown on screen, symbol used inside PDF/CSV (plain ASCII), name
        public static readonly (string Code, string Symbol, string Ascii, string Name)[] All =
        {
            ("PKR", "Rs.", "Rs.", "Pakistani Rupee (Rs.)"),
            ("USD", "$",   "$",   "US Dollar ($)"),
            ("EUR", "€",   "EUR", "Euro (€)"),
            ("GBP", "£",   "GBP", "British Pound (£)"),
            ("SAR", "SAR", "SAR", "Saudi Riyal (SAR)"),
            ("AED", "AED", "AED", "UAE Dirham (AED)"),
            ("INR", "₹",   "INR", "Indian Rupee (₹)")
        };

        // Used by PDF reports (set by the controller before building the report)
        public static readonly AsyncLocal<string?> PdfSymbol = new AsyncLocal<string?>();

        public static string CookieName(HttpContext? ctx)
        {
            int? userId = null;
            try { userId = ctx?.Session.GetInt32("UserId"); } catch { }
            return CookiePrefix + (userId?.ToString() ?? "0");
        }

        public static string Normalize(string? code)
        {
            code = (code ?? "").Trim().ToUpperInvariant();
            foreach (var c in All)
            {
                if (c.Code == code) return code;
            }
            return DefaultCode;
        }

        public static string Code(HttpContext? ctx)
        {
            if (ctx == null) return DefaultCode;
            string? value = null;
            try { value = ctx.Request.Cookies[CookieName(ctx)]; } catch { }
            return Normalize(value);
        }

        public static string Symbol(HttpContext? ctx)
        {
            var code = Code(ctx);
            foreach (var c in All)
            {
                if (c.Code == code) return c.Symbol;
            }
            return "Rs.";
        }

        public static string AsciiSymbol(HttpContext? ctx)
        {
            var code = Code(ctx);
            foreach (var c in All)
            {
                if (c.Code == code) return c.Ascii;
            }
            return "Rs.";
        }
    }
}
