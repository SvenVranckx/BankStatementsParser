namespace BankStatementsParser
{
    public class Record
    {
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
    }
}
