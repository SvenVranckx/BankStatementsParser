namespace BankStatementsParser
{
    public class Record
    {
        private int _index = 0;

        public string? Date { get; set; }
        public string? Number { get; set; }
        public string? Type { get; set; }
        public string? Counterparty { get; set; }
        public string? Amount { get; set; }
        public string? Name { get; set; }
        public string? Address1 { get; set; }
        public string? Address2 { get; set; }
        public string? Message { get; set; }
        public string? Info { get; set; }

        public void Append<T>(T data) => AppendImpl(data);

        private void AppendImpl(object? data)
        {
            switch (_index)
            {
                case 0: Date = data?.ToString(); break;
                case 1: Number = data?.ToString(); break;
                case 2: Type = data?.ToString(); break;
                case 3: Counterparty = data?.ToString(); break;
                case 4: Amount = data?.ToString(); break;
                case 5: Name = data?.ToString(); break;
                case 6: Address1 = data?.ToString(); break;
                case 7: Address2 = data?.ToString(); break;
                case 8: Message = data?.ToString(); break;
                case 9: Info = data?.ToString(); break;
            }
            _index++;
        }

        public void Clear()
        {
            _index = 0;
            Date = null;
            Number = null;
            Type = null;
            Counterparty = null;
            Amount = null;
            Name = null;
            Address1 = null;
            Address2 = null;
            Message = null;
            Info = null;
        }

        public bool IsEmpty => _index == 0;

        public static bool IsNullOrEmpty(Record? record) => record == null || record.IsEmpty;

        public static void WriteHeader(StreamWriter output, string separator = ";")
        {
            var columns = new[] { "Datum", "Nummer", "Type", "Tegenpartij", "Bedrag", "Naam", "Adres1", "Adres2", "Mededeling", "Info" };
            var header = string.Join(separator, columns);
            output.WriteLine(header);
        }

        public void WriteTo(StreamWriter output, string separator = ";")
        {
            var values = new[] { Date, Number, Type, Counterparty, Amount, Name, Address1, Address2, Message, Info };
            var line = string.Join(separator, values.Select(v => v ?? string.Empty));
            output.WriteLine(line);
        }
    }
}
