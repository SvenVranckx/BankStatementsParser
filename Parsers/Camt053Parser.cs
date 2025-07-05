using System.Xml.Serialization;

namespace BankStatementsParser.Parsers
{
    public class Camt053Parser : IParser
    {
        private readonly StreamReader _input;
        public Camt053Parser(StreamReader input) => _input = input ?? throw new ArgumentNullException(nameof(input));

        public IEnumerable<Record> ParseRecords()
        {
            var serializer = new XmlSerializer(typeof(Model.Document));
            var document = (Model.Document?)serializer.Deserialize(_input);
            if (document?.BankToCustomerStatement?.Statements is null)
                yield break;

            foreach (var statement in document.BankToCustomerStatement.Statements)
            {
                if (statement?.Entries is null)
                    continue;
                foreach (var entry in statement.Entries)
                {
                    var record = new Record();
                    var type = entry.TransactionType;
                    record.Date = entry.BookingDateTime.Value;
                    record.Number = entry.Reference;
                    record.Type = TranslateType(entry.Code);
                    record.Currency = entry.Amount.Currency;
                    var details = entry.Details.TransactionDetails;
                    var parties = details.RelatedParties;
                    if (type == Model.TransactionType.Debit)
                    {
                        record.Counterparty = Format.IBAN(parties?.CreditorAccount?.Id.IBAN);
                        record.Name = parties?.Creditor?.Name;
                        record.Amount = -entry.Amount.Value;
                        record.Address = parties?.Creditor?.PostalAddress?.AddressLines;
                    }
                    else
                    {
                        record.Counterparty = Format.IBAN(parties?.DebtorAccount?.Id.IBAN);
                        record.Name = parties?.Debtor?.Name;
                        record.Amount = entry.Amount.Value;
                        record.Address = parties?.Debtor?.PostalAddress?.AddressLines;
                    }
                    var info = details.RemittanceInformation;
                    if (info.Structured?.Information != null)
                        record.Message = Format.StructuredInfo(info.Structured.Information.Reference);
                    else
                    {
                        var information = info.Unstructured?
                            .Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries)
                            .Select(i => i?.Trim())
                            .Where(i => !string.IsNullOrEmpty(i))
                            .ToArray();
                        record.Message = information?.FirstOrDefault();
                        if (information?.Length > 1)
                            record.Info = string.Join(", ", information.Skip(1));
                    }

                    if (details.Refs?.MandateId != null)
                    {
                        var mandate = $"Mandaatreferte: {details.Refs.MandateId}";
                        if (string.IsNullOrEmpty(record.Info))
                            record.Info = mandate;
                        else
                            record.Info += ", " + mandate;
                    }

                    yield return record;
                }
            }
        }

        private static string? TranslateType(Model.BookingTransactionCode transactionCode)
        {
            var code = transactionCode?.Domain?.Code?.ToUpper();
            if (string.IsNullOrWhiteSpace(code))
                return code;
            var family = $"{transactionCode?.Domain?.Family?.Code}/{transactionCode?.Domain?.Family?.SubFamilyCode}".ToUpper();
            return code switch
            {
                "PMNT" => family switch
                {
                    "ICDT/ESCT" => "Overschrijving",
                    "IRCT/ESCT" => "Instantoverschrijving",
                    "RDDT/PMDD" => "Domiciliëring",
                    "CCRD/POSC" => "Afrekening kredietkaart",
                    "CCRD/POSD" => "Betaling Bancontact",
                    "MDOP/PMNT" => "Afrekening factuur bank",
                    "RCDT/ESCT" => "Ontvangst",
                    "RCDT/XBCT" => "Internationale ontvangst",
                    _ => "PMNT?",
                },
                "SECU" => family switch
                {
                    "SETT/TRAD" => "Aankoop",
                    "CORP/DVCA" => "Afrekening coupons",
                    _ => "SECU?",
                },
                _ => $"{code}?",
            };
        }
    }

    namespace Model
    {
        [XmlRoot(ElementName = "MsgPgntn")]
        public class MessagePagination
        {

            [XmlElement(ElementName = "PgNb")]
            public int PageNumber { get; set; }

            [XmlElement(ElementName = "LastPgInd")]
            public bool LastPage { get; set; }
        }

        [XmlRoot(ElementName = "GrpHdr")]
        public class GroupHeader
        {

            [XmlElement(ElementName = "MsgId")]
            public string MessageId { get; set; }

            [XmlElement(ElementName = "CreDtTm")]
            public DateTime CreationDateTime { get; set; }

            [XmlElement(ElementName = "MsgPgntn")]
            public MessagePagination Pagination { get; set; }
        }

        [XmlRoot(ElementName = "FrToDt")]
        public class DateTimeRange
        {

            [XmlElement(ElementName = "FrDtTm")]
            public DateTime From { get; set; }

            [XmlElement(ElementName = "ToDtTm")]
            public DateTime To { get; set; }
        }

        [XmlRoot(ElementName = "Id")]
        public class Id
        {

            [XmlElement(ElementName = "IBAN")]
            public string IBAN { get; set; }

            [XmlElement(ElementName = "OrgId")]
            public OrganizationId Organization { get; set; }
        }

        [XmlRoot(ElementName = "Tp")]
        public class Type
        {

            [XmlElement(ElementName = "Prtry")]
            public string Proprietary { get; set; }

            [XmlElement(ElementName = "CdOrPrtry")]
            public CodeOrProprietary CodeOrProprietary { get; set; }

            [XmlElement(ElementName = "Issr")]
            public string Issuer { get; set; }
        }

        [XmlRoot(ElementName = "Othr")]
        public class Other
        {

            [XmlElement(ElementName = "Id")]
            public string Id { get; set; }
        }

        [XmlRoot(ElementName = "OrgId")]
        public class OrganizationId
        {

            [XmlElement(ElementName = "Othr")]
            public Other Other { get; set; }
        }

        [XmlRoot(ElementName = "Ownr")]
        public class Owner
        {

            [XmlElement(ElementName = "Nm")]
            public string Name { get; set; }

            [XmlElement(ElementName = "Id")]
            public Id Id { get; set; }
        }

        [XmlRoot(ElementName = "FinInstnId")]
        public class FinancialInstitutionId
        {

            [XmlElement(ElementName = "BIC")]
            public string BIC { get; set; }
        }

        [XmlRoot(ElementName = "Svcr")]
        public class Servicer
        {

            [XmlElement(ElementName = "FinInstnId")]
            public FinancialInstitutionId Institution { get; set; }
        }

        [XmlRoot(ElementName = "Acct")]
        public class Account
        {

            [XmlElement(ElementName = "Id")]
            public Id Id { get; set; }

            [XmlElement(ElementName = "Tp")]
            public Type Type { get; set; }

            [XmlElement(ElementName = "Ccy")]
            public string Currency { get; set; }

            [XmlElement(ElementName = "Ownr")]
            public Owner Owner { get; set; }

            [XmlElement(ElementName = "Svcr")]
            public Servicer Servicer { get; set; }
        }

        [XmlRoot(ElementName = "CdOrPrtry")]
        public class CodeOrProprietary
        {

            [XmlElement(ElementName = "Cd")]
            public string Code { get; set; }
        }

        [XmlRoot(ElementName = "Amt")]
        public class Amount
        {

            [XmlAttribute(AttributeName = "Ccy")]
            public string Currency { get; set; }

            [XmlText]
            public double Value { get; set; }

            public override string ToString() => $"{Currency} {Value:F2}";
        }

        [XmlRoot(ElementName = "Dt")]
        public class Date
        {

            [XmlElement(ElementName = "Dt")]
            public DateTime Value { get; set; }

            public override string ToString() => Value.ToString("dd/MM/yyyy");
        }

        [XmlRoot(ElementName = "Bal")]
        public class Balance
        {

            [XmlElement(ElementName = "Tp")]
            public Type Type { get; set; }

            [XmlElement(ElementName = "Amt")]
            public Amount Amount { get; set; }

            [XmlElement(ElementName = "CdtDbtInd")]
            public string CreditOrDebit { get; set; }

            [XmlElement(ElementName = "Dt")]
            public Date Date { get; set; }
        }

        [XmlRoot(ElementName = "TtlNtries")]
        public class TotalNumberOfEntries
        {

            [XmlElement(ElementName = "NbOfNtries")]
            public int NumberOfEntries { get; set; }
        }

        [XmlRoot(ElementName = "TxsSummry")]
        public class TransactionsSummary
        {

            [XmlElement(ElementName = "TtlNtries")]
            public TotalNumberOfEntries TotalNumberOfEntries { get; set; }
        }

        [XmlRoot(ElementName = "BookgDt")]
        public class BookingDateTime
        {

            [XmlElement(ElementName = "DtTm")]
            public DateTime Value { get; set; }

            public override string ToString() => Value.ToString("dd/MM/yyy");
        }

        [XmlRoot(ElementName = "ValDt")]
        public class ValueDate
        {

            [XmlElement(ElementName = "Dt")]
            public DateTime Value { get; set; }

            public override string ToString() => Value.ToString("dd/MM/yyy");
        }

        [XmlRoot(ElementName = "Fmly")]
        public class Family
        {

            [XmlElement(ElementName = "Cd")]
            public string Code { get; set; }

            [XmlElement(ElementName = "SubFmlyCd")]
            public string SubFamilyCode { get; set; }

            public override string ToString() => $"{Code} / {SubFamilyCode}";
        }

        [XmlRoot(ElementName = "Domn")]
        public class Domain
        {

            [XmlElement(ElementName = "Cd")]
            public string Code { get; set; }

            [XmlElement(ElementName = "Fmly")]
            public Family Family { get; set; }
        }

        [XmlRoot(ElementName = "Prtry")]
        public class Proprietary
        {

            [XmlElement(ElementName = "Cd")]
            public string Code { get; set; }

            [XmlElement(ElementName = "Issr")]
            public string Issuer { get; set; }
        }

        [XmlRoot(ElementName = "BkTxCd")]
        public class BookingTransactionCode
        {

            [XmlElement(ElementName = "Domn")]
            public Domain Domain { get; set; }

            [XmlElement(ElementName = "Prtry")]
            public Proprietary Proprietary { get; set; }
        }

        [XmlRoot(ElementName = "Refs")]
        public class References
        {

            [XmlElement(ElementName = "AcctSvcrRef")]
            public string AccountServicerReference { get; set; }

            [XmlElement(ElementName = "EndToEndId")]
            public string EndToEndId { get; set; }

            [XmlElement(ElementName = "MndtId")]
            public string MandateId { get; set; }
        }

        [XmlRoot(ElementName = "Cdtr")]
        public class Creditor
        {

            [XmlElement(ElementName = "Nm")]
            public string Name { get; set; }

            [XmlElement(ElementName = "PstlAdr")]
            public PostalAddress PostalAddress { get; set; }
        }

        [XmlRoot(ElementName = "CdtrAcct")]
        public class CreditorAccount
        {

            [XmlElement(ElementName = "Id")]
            public Id Id { get; set; }
        }

        [XmlRoot(ElementName = "RltdPties")]
        public class RelatedParties
        {

            [XmlElement(ElementName = "Cdtr")]
            public Creditor Creditor { get; set; }

            [XmlElement(ElementName = "CdtrAcct")]
            public CreditorAccount CreditorAccount { get; set; }

            [XmlElement(ElementName = "Dbtr")]
            public Debtor Debtor { get; set; }

            [XmlElement(ElementName = "DbtrAcct")]
            public DebtorAccount DebtorAccount { get; set; }
        }

        [XmlRoot(ElementName = "CdtrAgt")]
        public class CreditorAgent
        {

            [XmlElement(ElementName = "FinInstnId")]
            public FinancialInstitutionId Institution { get; set; }
        }

        [XmlRoot(ElementName = "RltdAgts")]
        public class RelatedAgents
        {

            [XmlElement(ElementName = "CdtrAgt")]
            public CreditorAgent CreditorAgent { get; set; }

            [XmlElement(ElementName = "DbtrAgt")]
            public DebtorAgent DebtorAgent { get; set; }
        }

        [XmlRoot(ElementName = "CdtrRefInf")]
        public class CreditorReferenceInformation
        {

            [XmlElement(ElementName = "Tp")]
            public Type Type { get; set; }

            [XmlElement(ElementName = "Ref")]
            public string Reference { get; set; }
        }

        [XmlRoot(ElementName = "Strd")]
        public class StructuredInformation
        {

            [XmlElement(ElementName = "CdtrRefInf")]
            public CreditorReferenceInformation Information { get; set; }
        }

        [XmlRoot(ElementName = "RmtInf")]
        public class RemittanceInformation
        {

            [XmlElement(ElementName = "Strd")]
            public StructuredInformation Structured { get; set; }

            [XmlElement(ElementName = "Ustrd")]
            public string Unstructured { get; set; }
        }

        [XmlRoot(ElementName = "TxDtls")]
        public class TransactionDetails
        {

            [XmlElement(ElementName = "Refs")]
            public References Refs { get; set; }

            [XmlElement(ElementName = "RltdPties")]
            public RelatedParties RelatedParties { get; set; }

            [XmlElement(ElementName = "RltdAgts")]
            public RelatedAgents RelatedAgents { get; set; }

            [XmlElement(ElementName = "RmtInf")]
            public RemittanceInformation RemittanceInformation { get; set; }
        }

        [XmlRoot(ElementName = "NtryDtls")]
        public class EntryDetails
        {

            [XmlElement(ElementName = "TxDtls")]
            public TransactionDetails TransactionDetails { get; set; }
        }

        public enum TransactionType
        {
            Unknown,
            Credit,
            Debit
        }

        public static class Convertor
        {
            public static TransactionType ParseTransactionType(string type)
            {
                if (string.IsNullOrEmpty(type))
                    return TransactionType.Unknown;
                switch (type.ToUpper())
                {
                    case "DBIT": return TransactionType.Debit;
                    case "CRDT": return TransactionType.Credit;
                }
                return TransactionType.Unknown;
            }
        }

        [XmlRoot(ElementName = "Ntry")]
        public class Entry
        {

            [XmlElement(ElementName = "NtryRef")]
            public string Reference { get; set; }

            [XmlElement(ElementName = "Amt")]
            public Amount Amount { get; set; }

            [XmlElement(ElementName = "CdtDbtInd")]
            public string CreditOrDebit { get; set; }

            public TransactionType TransactionType => Convertor.ParseTransactionType(CreditOrDebit);

            [XmlElement(ElementName = "Sts")]
            public string Status { get; set; }

            [XmlElement(ElementName = "BookgDt")]
            public BookingDateTime BookingDateTime { get; set; }

            [XmlElement(ElementName = "ValDt")]
            public ValueDate ValueDate { get; set; }

            [XmlElement(ElementName = "AcctSvcrRef")]
            public string AccountServicerReference { get; set; }

            [XmlElement(ElementName = "BkTxCd")]
            public BookingTransactionCode Code { get; set; }

            [XmlElement(ElementName = "NtryDtls")]
            public EntryDetails Details { get; set; }
        }

        [XmlRoot(ElementName = "Stmt")]
        public class Statement
        {

            [XmlElement(ElementName = "Id")]
            public string Id { get; set; }

            [XmlElement(ElementName = "CreDtTm")]
            public DateTime CreationDateTime { get; set; }

            [XmlElement(ElementName = "FrToDt")]
            public DateTimeRange DateRange { get; set; }

            [XmlElement(ElementName = "Acct")]
            public Account Account { get; set; }

            [XmlElement(ElementName = "Bal")]
            public List<Balance> Balance { get; set; }

            [XmlElement(ElementName = "TxsSummry")]
            public TransactionsSummary Summary { get; set; }

            [XmlElement(ElementName = "Ntry")]
            public List<Entry> Entries { get; set; }
        }

        [XmlRoot(ElementName = "PstlAdr")]
        public class PostalAddress
        {

            [XmlElement(ElementName = "Ctry")]
            public string Country { get; set; }

            [XmlElement(ElementName = "AdrLine")]
            public List<string> AddressLines { get; set; }
        }

        [XmlRoot(ElementName = "Dbtr")]
        public class Debtor
        {

            [XmlElement(ElementName = "Nm")]
            public string Name { get; set; }

            [XmlElement(ElementName = "PstlAdr")]
            public PostalAddress PostalAddress { get; set; }
        }

        [XmlRoot(ElementName = "DbtrAcct")]
        public class DebtorAccount
        {

            [XmlElement(ElementName = "Id")]
            public Id Id { get; set; }
        }

        [XmlRoot(ElementName = "DbtrAgt")]
        public class DebtorAgent
        {

            [XmlElement(ElementName = "FinInstnId")]
            public FinancialInstitutionId Institution { get; set; }
        }

        [XmlRoot(ElementName = "BkToCstmrStmt")]
        public class BankToCustomerStatement
        {

            [XmlElement(ElementName = "GrpHdr")]
            public GroupHeader Header { get; set; }

            [XmlElement(ElementName = "Stmt")]
            public List<Statement> Statements { get; set; }
        }

        [XmlRoot(ElementName = "Document", Namespace = "urn:iso:std:iso:20022:tech:xsd:camt.053.001.02", IsNullable = true)]
        public class Document
        {

            [XmlElement(ElementName = "BkToCstmrStmt")]
            public BankToCustomerStatement BankToCustomerStatement { get; set; }

            [XmlAttribute(AttributeName = "xmlns")]
            public string Namespace { get; set; }

            [XmlText]
            public string Text { get; set; }
        }
    }
}
