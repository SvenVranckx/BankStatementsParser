using System.Text;

namespace BankStatementsParser
{
    public static class Format
    {
        public static string? IBAN(string? iban)
        {
            if (string.IsNullOrEmpty(iban) || iban.Length < 16)
                return iban;
            var builder = new StringBuilder(20);
            for (int i = 0; i < iban.Length; i++)
            {
                if (i > 0 && i % 4 == 0)
                    builder.Append(' ');
                builder.Append(iban[i]);
            }
            return builder.ToString();
        }

        public static string? StructuredInfo(string? info)
        {
            if (string.IsNullOrEmpty(info) || info.Length < 12)
                return info;
            var builder = new StringBuilder(20);
            builder.Append("+++");
            for (int i = 0; i < info.Length; i++)
            {
                builder.Append(info[i]);
                if (i == 2 || i == 6)
                    builder.Append('/');
            }
            builder.Append("+++");
            return builder.ToString();
        }
    }
}