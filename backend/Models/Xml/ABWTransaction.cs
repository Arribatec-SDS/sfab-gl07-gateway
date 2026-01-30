using System.Xml.Serialization;

namespace SfabGl07Gateway.Api.Models.Xml;

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
    [XmlElement("VoucherNo", Namespace = AgressoNamespaces.SchemaLib)]
    public string? VoucherNo { get; set; }

    [XmlElement("VoucherType", Namespace = AgressoNamespaces.SchemaLib)]
    public string? VoucherType { get; set; }

    [XmlElement("CompanyCode", Namespace = AgressoNamespaces.SchemaLib)]
    public string? CompanyCode { get; set; }

    [XmlElement("Period", Namespace = AgressoNamespaces.SchemaLib)]
    public string? Period { get; set; }

    [XmlElement("VoucherDate", Namespace = AgressoNamespaces.Transaction)]
    public string? VoucherDate { get; set; }

    [XmlElement("Description", Namespace = AgressoNamespaces.SchemaLib)]
    public string? Description { get; set; }

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

    [XmlElement("Client", Namespace = AgressoNamespaces.SchemaLib)]
    public string? Client { get; set; }

    [XmlElement("Description", Namespace = AgressoNamespaces.SchemaLib)]
    public string? Description { get; set; }

    [XmlElement("Status", Namespace = AgressoNamespaces.SchemaLib)]
    public string? Status { get; set; }

    [XmlElement("TransDate", Namespace = AgressoNamespaces.Transaction)]
    public string? TransDate { get; set; }

    [XmlElement("ExternalRef", Namespace = AgressoNamespaces.Transaction)]
    public string? ExternalRef { get; set; }

    [XmlElement("SequenceNo", Namespace = AgressoNamespaces.SchemaLib)]
    public int? SequenceNo { get; set; }

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
    [XmlElement("DcFlag", Namespace = AgressoNamespaces.SchemaLib)]
    public string? DcFlag { get; set; } // 'D' for Debit, 'C' for Credit

    [XmlElement("Amount", Namespace = AgressoNamespaces.SchemaLib)]
    public decimal? Amount { get; set; }

    [XmlElement("CurrAmount", Namespace = AgressoNamespaces.SchemaLib)]
    public decimal? CurrAmount { get; set; }

    [XmlElement("Number1", Namespace = AgressoNamespaces.SchemaLib)]
    public decimal? Number1 { get; set; }

    [XmlElement("Value1", Namespace = AgressoNamespaces.SchemaLib)]
    public decimal? Value1 { get; set; }

    [XmlElement("Value2", Namespace = AgressoNamespaces.SchemaLib)]
    public decimal? Value2 { get; set; }

    [XmlElement("Value3", Namespace = AgressoNamespaces.SchemaLib)]
    public decimal? Value3 { get; set; }

    [XmlElement("CurrencyCode", Namespace = AgressoNamespaces.SchemaLib)]
    public string? CurrencyCode { get; set; }
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
}

/// <summary>
/// Accounts Payable/Accounts Receivable information.
/// </summary>
public class ApArInfo
{
    [XmlElement("ApArType")]
    public string? ApArType { get; set; } // 'S' for Supplier, 'C' for Customer

    [XmlElement("ApArNo")]
    public string? ApArNo { get; set; }

    [XmlElement("InvoiceNo")]
    public string? InvoiceNo { get; set; }

    [XmlElement("DueDate")]
    public string? DueDate { get; set; }

    [XmlElement("PayMethod")]
    public string? PayMethod { get; set; }

    [XmlElement("SundryInfo")]
    public SundryInfo? SundryInfo { get; set; }
}

/// <summary>
/// Additional sundry information for AP/AR.
/// </summary>
public class SundryInfo
{
    [XmlElement("Text1")]
    public string? Text1 { get; set; }

    [XmlElement("Text2")]
    public string? Text2 { get; set; }

    [XmlElement("Text3")]
    public string? Text3 { get; set; }

    [XmlElement("Text4")]
    public string? Text4 { get; set; }

    [XmlElement("Text5")]
    public string? Text5 { get; set; }
}

/// <summary>
/// Tax transaction information.
/// </summary>
public class TaxTransInfo
{
    [XmlElement("Account2")]
    public string? Account2 { get; set; }

    [XmlElement("BaseAmount")]
    public decimal? BaseAmount { get; set; }

    [XmlElement("BaseCurr")]
    public string? BaseCurr { get; set; }

    [XmlElement("TaxAmount")]
    public decimal? TaxAmount { get; set; }

    [XmlElement("TaxCurr")]
    public string? TaxCurr { get; set; }
}
