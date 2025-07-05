using System.Text.RegularExpressions;

namespace BankStatementsParser.Parsers
{
    public class PdfParser : IParser
    {
        public static readonly IParser Instance = new PdfParser();

        public IEnumerable<Record> Parse(StreamReader input)
        {
            // Accounting year
            var now = DateTime.Now.Date;
            int year = now.Year;
            if (now.Month <= 3)
                year--;

            // Regex patterns
            var start = string.Format(@"^\s*(\d+\/\d+\/{0})\s+({0}-\d+)\s+", year);
            var pattern = new
            {
                record = start + @"([^\W\d_]+)\s+([A-Z]{2}\d{2} \d{4} \d{4} \d{4})\s+([\d.,+-]*)\s+EUR\s*$",
                malformed = start + @"(.+)\s+([\d.,+-]*)\s+EUR\s*$",
                message = @"Mededeling:\s*(.*)$",
                info = @"Info:\s*(.*)$",
            };

            // Regex
            var rxrecord = new Regex(pattern.record, RegexOptions.IgnoreCase);
            var rxmalformed = new Regex(pattern.malformed, RegexOptions.IgnoreCase);
            var rxmessage = new Regex(pattern.message, RegexOptions.IgnoreCase);
            var rxinfo = new Regex(pattern.info, RegexOptions.IgnoreCase);

            // Parse input file
            string? line;
            Record? record = null;
            int address = 0;
            while ((line = input.ReadLine()) != null)
            {
                // Skip blank/whitespace lines
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                // Match start of new record
                var match = rxrecord.Match(line);
                if (match.Success)
                {
                    // Write previous record (if any)
                    if (!Record.IsNullOrEmpty(record))
                        yield return record!;
                    // Create new record
                    record = new Record();
                    for (int i = 1; i <= 4; i++)
                        record.Append(match.Groups[i].Value);
                    // Add amount
                    record.Append(match.Groups[5].Value.Replace(".", ""));
                    // Next 3 lines are address lines
                    address = 3;
                }
                else if ((match = rxmalformed.Match(line)).Success)
                {
                    // Malformed record (starts with date and number, but other fields are missing/malformed)
                    // Write previous record (if any)
                    if (!Record.IsNullOrEmpty(record))
                        yield return record!;
                    // Create new record
                    record = new Record();
                    for (int i = 1; i <= 3; i++)
                        record.Append(match.Groups[i].Value);
                    // Add separator for missing column
                    record.Append(string.Empty);
                    // Add amount
                    record.Append(match.Groups[4].Value.Replace(".", ""));
                    // Next 3 lines are address lines
                    address = 3;
                }
                else if ((match = rxmessage.Match(line)).Success && record != null)
                {
                    // Write separators for missing address lines
                    for (int i = 0; i < address; i++)
                        record.Append(string.Empty);
                    // Message field
                    record.Append(match.Groups[1].Value);
                    // No more address lines
                    address = 0;
                }
                else if ((match = rxinfo.Match(line)).Success && record != null)
                {
                    // Info line (final field)
                    record.Append(match.Groups[1].Value);
                    // No more address lines
                    address = 0;
                }
                else if (record != null && address > 0)
                {
                    // Address line
                    record.Append(line.TrimEnd());
                    address--;
                }
            }
            // Write final record
            if (!Record.IsNullOrEmpty(record))
                yield return record!;
        }
    }
}
