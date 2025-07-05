namespace BankStatementsParser.Writers
{
    public class CsvWriter : IWriter
    {
        private readonly StreamWriter _output;
        private readonly string _separator = ";";

        public CsvWriter(StreamWriter output) => _output = output ?? throw new ArgumentNullException(nameof(output));

        public void WriteHeader()
        {
            var columns = new[] { "Datum", "Nummer", "Type", "Tegenpartij", "Munt", "Bedrag", "Naam", "Adres1", "Adres2", "Mededeling", "Info" };
            var header = string.Join(_separator, columns);
            _output.WriteLine(header);
        }

        public void WriteRecord(Record record)
        {
            var values = new[]
            {
                record.Date.ToString("dd/MM/yyy"),
                record.Number,
                record.Type,
                record.Counterparty,
                record.Currency,
                record.Amount.ToString("F2"),
                record.Name,
                record.GetAddressLine(0),
                record.GetAddressLine(1),
                record.Message,
                record.Info
            };
            var line = string.Join(_separator, values.Select(v => v ?? string.Empty));
            _output.WriteLine(line);
        }
    }
}
