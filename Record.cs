namespace BankStatementsParser
{
    public class Record
    {
        public DateTime Date { get; set; }
        public string? Number { get; set; }
        public string? Type { get; set; }
        public string? Counterparty { get; set; }
        public string? Currency { get; set; }
        public double Amount { get; set; }
        public string? Name { get; set; }
        public List<string>? Address { get; set; }
        public string? Message { get; set; }
        public string? Info { get; set; }

        public string? GetAddressLine(int index)
        {
            if (Address is null || index < 0 || index >= Address.Count)
                return null;
            return Address[index];
        }
    }
}
