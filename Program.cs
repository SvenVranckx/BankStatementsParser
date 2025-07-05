using System.Text;

namespace BankStatementsParser
{
    public class Program
    {
        public static void Main(string[] args)
        {
            using var input = new StreamReader(@"C:\Users\Sven\Desktop\Uittreksels.xml", Encoding.UTF8);
            using var output = new StreamWriter(@"C:\Users\Sven\Desktop\Uittreksels.csv", false, Encoding.UTF8);
            var parser = new Parsers.Camt053Parser(input);
            var writer = new Writers.CsvWriter(output);
            Convert(parser, writer);
        }

        public static void Convert(IParser parser, IWriter writer)
        {
            writer.WriteHeader();
            foreach (var record in parser.ParseRecords())
                writer.WriteRecord(record);
        }
    }
}