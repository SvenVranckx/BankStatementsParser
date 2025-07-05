namespace BankStatementsParser
{
    public interface IWriter
    {
        void WriteHeader();
        void WriteRecord(Record record);
    }
}
