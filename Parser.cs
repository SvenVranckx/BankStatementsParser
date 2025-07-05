namespace BankStatementsParser
{
    public interface IParser
    {
        IEnumerable<Record> Parse(StreamReader source);
    }
}
