using System.Text;

namespace PFMA.Helpers
{
    public static class CsvUtil
    {
        /// <summary>Quotes a value for CSV when it contains a delimiter, quote or line break.</summary>
        public static string Escape(string? value)
        {
            value ??= "";
            if (value.IndexOfAny(new[] { ',', ';', '"', '\n', '\r' }) >= 0)
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }
            return value;
        }

        /// <summary>Finds the delimiter (comma, semicolon or tab) from the header line of the file.</summary>
        public static char DetectDelimiter(string text)
        {
            foreach (var rawLine in text.Split('\n'))
            {
                var line = rawLine.ToLowerInvariant();
                if (line.Contains("date") && line.Contains("amount"))
                {
                    int commas = line.Count(c => c == ',');
                    int semis = line.Count(c => c == ';');
                    int tabs = line.Count(c => c == '\t');
                    if (semis > commas && semis >= tabs) return ';';
                    if (tabs > commas && tabs > semis) return '\t';
                    return ',';
                }
            }
            return ',';
        }

        /// <summary>Parses CSV text (supports quoted values, escaped quotes and line breaks inside quotes).</summary>
        public static List<List<string>> Parse(string text, char delimiter)
        {
            var rows = new List<List<string>>();
            var row = new List<string>();
            var cell = new StringBuilder();
            bool inQuotes = false;

            void EndCell()
            {
                row.Add(cell.ToString());
                cell.Clear();
            }

            void EndRow()
            {
                EndCell();
                rows.Add(row);
                row = new List<string>();
            }

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < text.Length && text[i + 1] == '"')
                        {
                            cell.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        cell.Append(c);
                    }
                }
                else if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == delimiter)
                {
                    EndCell();
                }
                else if (c == '\r')
                {
                    // ignore, handled by \n
                }
                else if (c == '\n')
                {
                    EndRow();
                }
                else
                {
                    cell.Append(c);
                }
            }

            if (cell.Length > 0 || row.Count > 0)
            {
                EndRow();
            }

            return rows;
        }
    }
}
