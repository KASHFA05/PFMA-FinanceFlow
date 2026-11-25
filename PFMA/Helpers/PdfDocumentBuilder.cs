using System.Globalization;
using System.Text;

namespace PFMA.Helpers
{
    public readonly record struct Rgb(int R, int G, int B);

    /// <summary>
    /// Very small dependency-free PDF writer (A4, Helvetica). Coordinates are in millimetres,
    /// measured from the TOP-LEFT corner of the page (same as jsPDF), so no external library
    /// or internet connection is needed to generate reports.
    /// </summary>
    public class PdfDocumentBuilder
    {
        public const double PageWidthMm = 210.0;
        public const double PageHeightMm = 297.0;
        internal const double Mm = 72.0 / 25.4;

        private readonly List<PdfPage> _pages = new();
        public IReadOnlyList<PdfPage> Pages => _pages;

        public PdfPage AddPage()
        {
            var p = new PdfPage();
            _pages.Add(p);
            return p;
        }

        public byte[] Build()
        {
            if (_pages.Count == 0) AddPage();

            var ms = new MemoryStream();
            var offsets = new List<long>();

            void Write(string s)
            {
                var b = Encoding.ASCII.GetBytes(s);
                ms.Write(b, 0, b.Length);
            }

            void BeginObj(int n)
            {
                offsets.Add(ms.Position);
                Write(n.ToString(CultureInfo.InvariantCulture) + " 0 obj\n");
            }

            Write("%PDF-1.4\n");

            // 1: catalog
            BeginObj(1);
            Write("<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");

            // 2: page tree
            var kids = new StringBuilder();
            for (int i = 0; i < _pages.Count; i++)
            {
                kids.Append(5 + 2 * i).Append(" 0 R ");
            }
            BeginObj(2);
            Write("<< /Type /Pages /Kids [" + kids + "] /Count " + _pages.Count + " >>\nendobj\n");

            // 3, 4: fonts
            BeginObj(3);
            Write("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>\nendobj\n");
            BeginObj(4);
            Write("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>\nendobj\n");

            string w = PdfPage.N(PageWidthMm * Mm);
            string h = PdfPage.N(PageHeightMm * Mm);

            for (int i = 0; i < _pages.Count; i++)
            {
                int pageObj = 5 + 2 * i;
                int contentObj = 6 + 2 * i;

                BeginObj(pageObj);
                Write("<< /Type /Page /Parent 2 0 R /MediaBox [0 0 " + w + " " + h + "] " +
                      "/Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents " + contentObj + " 0 R >>\nendobj\n");

                string content = _pages[i].Content;
                int length = Encoding.ASCII.GetByteCount(content);
                BeginObj(contentObj);
                Write("<< /Length " + length + " >>\nstream\n");
                Write(content);
                Write("endstream\nendobj\n");
            }

            long xrefPos = ms.Position;
            int objCount = offsets.Count + 1;
            Write("xref\n0 " + objCount + "\n");
            Write("0000000000 65535 f \n");
            foreach (var off in offsets)
            {
                Write(off.ToString("D10", CultureInfo.InvariantCulture) + " 00000 n \n");
            }
            Write("trailer\n<< /Size " + objCount + " /Root 1 0 R >>\nstartxref\n" + xrefPos + "\n%%EOF\n");

            return ms.ToArray();
        }
    }

    public class PdfPage
    {
        private readonly StringBuilder _sb = new();
        internal string Content => _sb.ToString();

        private const double Mm = PdfDocumentBuilder.Mm;

        internal static string N(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);

        private static double X(double mm) => mm * Mm;
        private static double Y(double mmFromTop) => (PdfDocumentBuilder.PageHeightMm - mmFromTop) * Mm;

        private static string Col(Rgb c) =>
            N(c.R / 255.0) + " " + N(c.G / 255.0) + " " + N(c.B / 255.0);

        public void FillRect(double x, double y, double w, double h, Rgb color, double radius = 0)
        {
            _sb.Append(Col(color)).Append(" rg\n");
            double px = X(x), py = Y(y + h), pw = w * Mm, ph = h * Mm;

            if (radius <= 0)
            {
                _sb.Append(N(px)).Append(' ').Append(N(py)).Append(' ')
                   .Append(N(pw)).Append(' ').Append(N(ph)).Append(" re\n");
            }
            else
            {
                double r = Math.Min(radius * Mm, Math.Min(pw, ph) / 2);
                double k = 0.5523 * r;
                _sb.Append($"{N(px + r)} {N(py)} m\n");
                _sb.Append($"{N(px + pw - r)} {N(py)} l\n");
                _sb.Append($"{N(px + pw - r + k)} {N(py)} {N(px + pw)} {N(py + r - k)} {N(px + pw)} {N(py + r)} c\n");
                _sb.Append($"{N(px + pw)} {N(py + ph - r)} l\n");
                _sb.Append($"{N(px + pw)} {N(py + ph - r + k)} {N(px + pw - r + k)} {N(py + ph)} {N(px + pw - r)} {N(py + ph)} c\n");
                _sb.Append($"{N(px + r)} {N(py + ph)} l\n");
                _sb.Append($"{N(px + r - k)} {N(py + ph)} {N(px)} {N(py + ph - r + k)} {N(px)} {N(py + ph - r)} c\n");
                _sb.Append($"{N(px)} {N(py + r)} l\n");
                _sb.Append($"{N(px)} {N(py + r - k)} {N(px + r - k)} {N(py)} {N(px + r)} {N(py)} c\n");
                _sb.Append("h\n");
            }
            _sb.Append("f\n");
        }

        public void Line(double x1, double y1, double x2, double y2, Rgb color, double widthMm = 0.2)
        {
            _sb.Append(N(widthMm * Mm)).Append(" w ").Append(Col(color)).Append(" RG ")
               .Append(N(X(x1))).Append(' ').Append(N(Y(y1))).Append(" m ")
               .Append(N(X(x2))).Append(' ').Append(N(Y(y2))).Append(" l S\n");
        }

        /// <summary>Draws one line of text. y is the baseline (mm from top). align: left | right | center.</summary>
        public void Text(double x, double y, string text, double sizePt, bool bold, Rgb color, string align = "left")
        {
            if (string.IsNullOrEmpty(text)) return;

            double width = PdfFont.WidthPt(text, sizePt, bold);
            double px = X(x);
            if (align == "right") px -= width;
            else if (align == "center") px -= width / 2;

            _sb.Append("BT ").Append(Col(color)).Append(" rg ")
               .Append(bold ? "/F2 " : "/F1 ").Append(N(sizePt)).Append(" Tf ")
               .Append(N(px)).Append(' ').Append(N(Y(y))).Append(" Td (")
               .Append(PdfFont.Escape(text)).Append(") Tj ET\n");
        }
    }

    internal static class PdfFont
    {
        // Helvetica advance widths (1/1000 em) for ASCII 32..126
        private static readonly int[] Regular =
        {
            278,278,355,556,556,889,667,191,333,333,389,584,278,333,278,278,
            556,556,556,556,556,556,556,556,556,556,278,278,584,584,584,556,
            1015,667,667,722,722,667,611,778,722,278,500,667,556,833,722,778,
            667,778,722,667,611,722,667,944,667,667,611,278,278,278,469,556,
            333,556,556,500,556,556,278,556,556,222,222,500,222,833,556,556,
            556,556,333,500,278,556,500,722,500,500,500,334,260,334,584
        };

        // Helvetica-Bold
        private static readonly int[] Bold =
        {
            278,333,474,556,556,889,722,238,333,333,389,584,278,333,278,278,
            556,556,556,556,556,556,556,556,556,556,333,333,584,584,584,611,
            975,722,722,722,722,667,611,778,722,278,556,722,611,833,722,778,
            667,778,722,667,611,722,667,944,667,667,611,333,278,333,584,556,
            333,556,611,556,611,556,333,611,611,278,278,556,278,889,611,611,
            611,611,389,556,333,611,556,778,556,556,500,389,280,389,584
        };

        private static int CharWidth(char c, bool bold)
        {
            if (c >= 32 && c <= 126) return (bold ? Bold : Regular)[c - 32];
            if (c == '\u2022') return 350;
            return 556;
        }

        public static double WidthPt(string text, double sizePt, bool bold)
        {
            double total = 0;
            foreach (char c in text) total += CharWidth(c, bold);
            return total * sizePt / 1000.0;
        }

        public static double WidthMm(string text, double sizePt, bool bold) =>
            WidthPt(text, sizePt, bold) / PdfDocumentBuilder.Mm;

        /// <summary>Cuts the text with "..." so that it fits into maxWidthMm.</summary>
        public static string Fit(string text, double maxWidthMm, double sizePt, bool bold)
        {
            if (string.IsNullOrEmpty(text)) return "";
            if (WidthMm(text, sizePt, bold) <= maxWidthMm) return text;
            while (text.Length > 1 && WidthMm(text + "...", sizePt, bold) > maxWidthMm)
            {
                text = text.Substring(0, text.Length - 1);
            }
            return text + "...";
        }

        /// <summary>Splits text into lines that fit into maxWidthMm.</summary>
        public static List<string> Wrap(string text, double maxWidthMm, double sizePt, bool bold)
        {
            var lines = new List<string>();
            var current = "";
            foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                var candidate = current.Length == 0 ? word : current + " " + word;
                if (WidthMm(candidate, sizePt, bold) <= maxWidthMm || current.Length == 0)
                {
                    current = candidate;
                }
                else
                {
                    lines.Add(current);
                    current = word;
                }
            }
            if (current.Length > 0) lines.Add(current);
            return lines;
        }

        /// <summary>Escapes a string for a PDF literal string using WinAnsi encoding (pure ASCII output).</summary>
        public static string Escape(string text)
        {
            var sb = new StringBuilder();
            foreach (char c in text)
            {
                switch (c)
                {
                    case '(': sb.Append("\\("); break;
                    case ')': sb.Append("\\)"); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\u2022': sb.Append("\\225"); break;           // bullet
                    case '\u2013': sb.Append("\\226"); break;           // en dash
                    case '\u2014': sb.Append("\\227"); break;           // em dash
                    case '\u2018':
                    case '\u2019': sb.Append('\''); break;
                    case '\u201C':
                    case '\u201D': sb.Append('"'); break;
                    default:
                        if (c >= 32 && c <= 126) sb.Append(c);
                        else if (c >= 160 && c <= 255) sb.Append('\\').Append(Convert.ToString((int)c, 8).PadLeft(3, '0'));
                        else sb.Append('?');                            // characters Helvetica cannot show (e.g. Urdu)
                        break;
                }
            }
            return sb.ToString();
        }
    }
}
