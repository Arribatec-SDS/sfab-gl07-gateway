using System.Xml.Serialization;

namespace A1arErpSfabGl07Gateway.Api.Models.Xml;

// Namespace constants for Agresso XML
public static class AgressoNamespaces
{
    public const string Transaction = "http://services.agresso.com/schema/ABWTransaction/2011/11/14";
    public const string SchemaLib = "http://services.agresso.com/schema/ABWSchemaLib/2011/11/14";
}

/// <summary>
/// Root element for ABWTransaction XML documents.
/// Namespace: http://services.agresso.com/schema/ABWTransaction/2011/11/14
/// </summary>
[XmlRoot("ABWTransaction", Namespace = AgressoNamespaces.Transaction)]
public class ABWTransaction
{
    [XmlElement("Interface", Namespace = AgressoNamespaces.Transaction)]
    public string? Interface { get; set; }

    [XmlElement("BatchId", Namespace = AgressoNamespaces.SchemaLib)]
    public string? BatchId { get; set; }

    [XmlElement("ReportClient", Namespace = AgressoNamespaces.SchemaLib)]
    public string? ReportClient { get; set; }

    // Vouchers are directly under ABWTransaction (no wrapper element)
    [XmlElement("Voucher", Namespace = AgressoNamespaces.Transaction)]
    public List<Voucher> Vouchers { get; set; } = new();
}

/// <summary>
/// Represents a voucher containing multiple transactions.
/// </summary>
public class Voucher
{
    [XmlElement("VoucherNo", Namespace = AgressoNamespaces.Transaction)]
    public string? VoucherNo { get; set; }

    [XmlElement("VoucherType", Namespace = AgressoNamespaces.SchemaLib)]
    public string? VoucherType { get; set; }

    [XmlElement("CompanyCode", Namespace = AgressoNamespaces.SchemaLib)]
    public string? CompanyCode { get; set; }

    [XmlElement("Period", Namespace = AgressoNamespaces.SchemaLib)]
    public string? Period { get; set; }

    [XmlElement("VoucherDate", Namespace = AgressoNamespaces.Transaction)]
    public string? VoucherDate { get; set; }

    // Transactions are directly under Voucher (no wrapper element)
    [XmlElement("Transaction", Namespace = AgressoNamespaces.Transaction)]
    public List<Transaction> Transactions { get; set; } = new();
}

/// <summary>
/// Represents a single transaction line.
/// </summary>
public class Transaction
{
    [XmlElement("TransType", Namespace = AgressoNamespaces.SchemaLib)]
    public string? TransType { get; set; }

    [XmlElement("Description", Namespace = AgressoNamespaces.SchemaLib)]
    public string? Description { get; set; }

    [XmlElement("Status", Namespace = AgressoNamespaces.SchemaLib)]
    public string? Status { get; set; }

    [XmlElement("TransDate", Namespace = AgressoNamespaces.Transaction)]
    public string? TransDate { get; set; }

    [XmlElement("AllocationKey", Namespace = AgressoNamespaces.SchemaLib)]
    public string? AllocationKey { get; set; }

    [XmlElement("PeriodNo", Namespace = AgressoNamespaces.Transaction)]
    public int? PeriodNo { get; set; }

    [XmlElement("PayTransfer", Namespace = AgressoNamespaces.Transaction)]
    public string? PayTransfer { get; set; }

    [XmlElement("ExternalRef", Namespace = AgressoNamespaces.Transaction)]
    public string? ExternalRef { get; set; }

    [XmlElement("Amounts", Namespace = AgressoNamespaces.Transaction)]
    public Amounts? Amounts { get; set; }

    [XmlElement("GLAnalysis", Namespace = AgressoNamespaces.Transaction)]
    public GLAnalysis? GLAnalysis { get; set; }

    [XmlElement("ApArInfo", Namespace = AgressoNamespaces.Transaction)]
    public ApArInfo? ApArInfo { get; set; }

    [XmlElement("TaxTransInfo", Namespace = AgressoNamespaces.Transaction)]
    public TaxTransInfo? TaxTransInfo { get; set; }
}

/// <summary>
/// Amount information for a transaction.
/// </summary>
public class Amounts
{
    [XmlElement("DcFlag", Namespace = AgressoNamespaces.Transaction)]
    public int? DcFlag { get; set; } // 1 = Debit, -1 = Credit

    [XmlElement("Amount", Namespace = AgressoNamespaces.SchemaLib)]
    public decimal? Amount { get; set; }

    [XmlElement("CurrAmount", Namespace = AgressoNamespaces.SchemaLib)]
    public decimal? CurrAmount { get; set; }

    [XmlElement("Number1", Namespace = AgressoNamespaces.Transaction)]
    public int? Number1 { get; set; }

    [XmlElement("Value1", Namespace = AgressoNamespaces.Transaction)]
    public decimal? Value1 { get; set; } // xs:float in XSD, decimal is safer

    [XmlElement("Value2", Namespace = AgressoNamespaces.Transaction)]
    public decimal? Value2 { get; set; }

    [XmlElement("Value3", Namespace = AgressoNamespaces.Transaction)]
    public decimal? Value3 { get; set; }
}

/// <summary>
/// General Ledger analysis dimensions.
/// </summary>
public class GLAnalysis
{
    [XmlElement("Account", Namespace = AgressoNamespaces.SchemaLib)]
    public string? Account { get; set; }

    [XmlElement("Dim1", Namespace = AgressoNamespaces.Transaction)]
    public string? Dim1 { get; set; }

    [XmlElement("Dim2", Namespace = AgressoNamespaces.Transaction)]
    public string? Dim2 { get; set; }

    [XmlElement("Dim3", Namespace = AgressoNamespaces.Transaction)]
    public string? Dim3 { get; set; }

    [XmlElement("Dim4", Namespace = AgressoNamespaces.Transaction)]
    public string? Dim4 { get; set; }

    [XmlElement("Dim5", Namespace = AgressoNamespaces.Transaction)]
    public string? Dim5 { get; set; }

    [XmlElement("Dim6", Namespace = AgressoNamespaces.Transaction)]
    public string? Dim6 { get; set; }

    [XmlElement("Dim7", Namespace = AgressoNamespaces.Transaction)]
    public string? Dim7 { get; set; }

    [XmlElement("Currency", Namespace = AgressoNamespaces.SchemaLib)]
    public string? Currency { get; set; }

    [XmlElement("TaxCode", Namespace = AgressoNamespaces.SchemaLib)]
    public string? TaxCode { get; set; }

    [XmlElement("TaxSystem", Namespace = AgressoNamespaces.SchemaLib)]
    public string? TaxSystem { get; set; }

    [XmlElement("TaxId", Namespace = AgressoNamespaces.Transaction)]
    public int? TaxId { get; set; } // Triangle trade indicator (EU)
}

/// <summary>
/// Accounts Payable/Accounts Receivable information.
/// </summary>
public class ApArInfo
{
    [XmlElement("ApArType", Namespace = AgressoNamespaces.SchemaLib)]
    public string? ApArType { get; set; } // 'S' for Supplier, 'C' for Customer

    [XmlElement("ApArNo", Namespace = AgressoNamespaces.SchemaLib)]
    public string? ApArNo { get; set; }

    [XmlElement("FactorShort", Namespace = AgressoNamespaces.Transaction)]
    public string? FactorShort { get; set; } // Payment recipient

    [XmlElement("InvoiceNo", Namespace = AgressoNamespaces.SchemaLib)]
    public string? InvoiceNo { get; set; }

    [XmlElement("Responsible", Namespace = AgressoNamespaces.SchemaLib)]
    public string? Responsible { get; set; }

    [XmlElement("DueDate", Namespace = AgressoNamespaces.SchemaLib)]
    public string? DueDate { get; set; }

    [XmlElement("DiscDate", Namespace = AgressoNamespaces.Transaction)]
    public string? DiscDate { get; set; }

    [XmlElement("Discount", Namespace = AgressoNamespaces.SchemaLib)]
    public decimal? Discount { get; set; }

    [XmlElement("BacsId", Namespace = AgressoNamespaces.SchemaLib)]
    public string? BacsId { get; set; }

    [XmlElement("PayMethod", Namespace = AgressoNamespaces.SchemaLib)]
    public string? PayMethod { get; set; }

    [XmlElement("PayFlag", Namespace = AgressoNamespaces.Transaction)]
    public int? PayFlag { get; set; } // 1 = Pay in advance

    [XmlElement("CurrLicense", Namespace = AgressoNamespaces.Transaction)]
    public string? CurrLicense { get; set; }

    [XmlElement("VoucherRef", Namespace = AgressoNamespaces.SchemaLib)]
    public string? VoucherRef { get; set; }

    [XmlElement("SequenceRef", Namespace = AgressoNamespaces.SchemaLib)]
    public string? SequenceRef { get; set; }

    [XmlElement("ArriveId", Namespace = AgressoNamespaces.Transaction)]
    public string? ArriveId { get; set; }

    [XmlElement("Commitment", Namespace = AgressoNamespaces.Transaction)]
    public string? Commitment { get; set; }

    [XmlElement("OrderNo", Namespace = AgressoNamespaces.Transaction)]
    public string? OrderNo { get; set; }

    [XmlElement("IntruleId", Namespace = AgressoNamespaces.SchemaLib)]
    public string? IntruleId { get; set; }

    [XmlElement("PayTemplate", Namespace = AgressoNamespaces.Transaction)]
    public string? PayTemplate { get; set; }

    [XmlElement("SundryInfo", Namespace = AgressoNamespaces.Transaction)]
    public SundryInfo? SundryInfo { get; set; }

    [XmlElement("OrigReference", Namespace = AgressoNamespaces.Transaction)]
    public string? OrigReference { get; set; }

    [XmlElement("ArrivalDate", Namespace = AgressoNamespaces.Transaction)]
    public string? ArrivalDate { get; set; }

    [XmlElement("PayCurrency", Namespace = AgressoNamespaces.Transaction)]
    public string? PayCurrency { get; set; }

    [XmlElement("ComplaintCode", Namespace = AgressoNamespaces.Transaction)]
    public string? ComplaintCode { get; set; }

    [XmlElement("ComplaintDate", Namespace = AgressoNamespaces.Transaction)]
    public string? ComplaintDate { get; set; }
}

/// <summary>
/// Additional sundry information for AP/AR (for sundry suppliers/customers).
/// </summary>
public class SundryInfo
{
    [XmlElement("ApArName", Namespace = AgressoNamespaces.Transaction)]
    public string? ApArName { get; set; } // Supplier/customer name

    [XmlElement("Address", Namespace = AgressoNamespaces.SchemaLib)]
    public string? Address { get; set; }

    [XmlElement("ZipCode", Namespace = AgressoNamespaces.SchemaLib)]
    public string? ZipCode { get; set; }

    [XmlElement("Place", Namespace = AgressoNamespaces.SchemaLib)]
    public string? Place { get; set; }

    [XmlElement("Province", Namespace = AgressoNamespaces.SchemaLib)]
    public string? Province { get; set; }

    [XmlElement("VatRegNo", Namespace = AgressoNamespaces.SchemaLib)]
    public string? VatRegNo { get; set; }

    [XmlElement("BankAccountType", Namespace = AgressoNamespaces.Transaction)]
    public string? BankAccountType { get; set; }

    [XmlElement("BankAccount", Namespace = AgressoNamespaces.SchemaLib)]
    public string? BankAccount { get; set; }

    [XmlElement("ClearingCode", Namespace = AgressoNamespaces.SchemaLib)]
    public string? ClearingCode { get; set; }

    [XmlElement("Swift", Namespace = AgressoNamespaces.SchemaLib)]
    public string? Swift { get; set; }
}

/// <summary>
/// Tax transaction information. Must be filled out for TX transactions.
/// </summary>
public class TaxTransInfo
{
    [XmlElement("Account2", Namespace = AgressoNamespaces.Transaction)]
    public string? Account2 { get; set; } // Account from which tax was calculated

    [XmlElement("BaseAmount", Namespace = AgressoNamespaces.Transaction)]
    public decimal? BaseAmount { get; set; } // Tax calculation base in company currency

    [XmlElement("BaseCurr", Namespace = AgressoNamespaces.Transaction)]
    public decimal? BaseCurr { get; set; } // Tax calculation base in any currency
}
