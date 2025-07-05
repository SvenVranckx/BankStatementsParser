using System.Text;

namespace BankStatementsParser
{
    public class Program
    {
        public static void Main(string[] args)
        {
            const string xmlInput = @"C:\Users\Sven\Desktop\Uittreksels.xml";
            const string xmlOutput = @"C:\Users\Sven\Desktop\Uittreksels_xml.csv";

            Convert(xmlInput, xmlOutput);

            const string pdfInput = @"C:\Users\Sven\Desktop\Uittreksels.txt";
            const string pdfOutput = @"C:\Users\Sven\Desktop\Uittreksels_txt.csv";

            Convert(pdfInput, pdfOutput);
        }

        public static void Convert(string inputPath, string outputPath)
        {
            var parser = Path.GetExtension(inputPath).Equals(".xml", StringComparison.OrdinalIgnoreCase) ?
                Parsers.Camt053Parser.Instance :
                Parsers.PdfParser.Instance;

            using var input = new StreamReader(inputPath, Encoding.UTF8);
            using var output = new StreamWriter(outputPath, false, Encoding.UTF8);
            Record.WriteHeader(output);
            foreach (var record in parser.Parse(input))
                record.WriteTo(output);
        }
    }
}