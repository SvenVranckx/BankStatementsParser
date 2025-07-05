namespace BankStatementsParser
{
    public interface IParser
    {
        IEnumerable<Record> ParseRecords();
    }
}
