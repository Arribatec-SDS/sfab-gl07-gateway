using System.Text.Json.Serialization;

namespace SfabGl07Gateway.Api.Models.Unit4;

/// <summary>
/// Root request object for posting financial transactions to Unit4 API.
/// POST /v1/financial-transaction-batch
/// </summary>
public class Unit4TransactionBatchRequest
{
    [JsonPropertyName("batchInformation")]
    public BatchInformation? BatchInformation { get; set; }

    [JsonPropertyName("transactionInformation")]
    public List<TransactionInformation> TransactionInformation { get; set; } = new();
}

/// <summary>
/// Batch header information.
/// </summary>
public class BatchInformation
{
    [JsonPropertyName("interface")]
    public string? Interface { get; set; }

    [JsonPropertyName("batchId")]
    public string? BatchId { get; set; }
}

/// <summary>
/// Transaction header - one per voucher.
/// </summary>
public class TransactionInformation
{
    [JsonPropertyName("companyId")]
    public string? CompanyId { get; set; }

    [JsonPropertyName("period")]
    public string? Period { get; set; }

    [JsonPropertyName("transactionDate")]
    public string? TransactionDate { get; set; }

    [JsonPropertyName("transactionType")]
    public string? TransactionType { get; set; }

    [JsonPropertyName("voucherNumber")]
    public string? VoucherNumber { get; set; }

    [JsonPropertyName("voucherType")]
    public string? VoucherType { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("invoice")]
    public Invoice? Invoice { get; set; }

    [JsonPropertyName("transactionDetailInformation")]
    public List<TransactionDetailInformation> TransactionDetailInformation { get; set; } = new();
}

/// <summary>
/// Invoice/AP/AR related information.
/// </summary>
public class Invoice
{
    [JsonPropertyName("customerOrSupplierId")]
    public string? CustomerOrSupplierId { get; set; }

    [JsonPropertyName("ledgerType")]
    public string? LedgerType { get; set; } // 'S' for Supplier, 'C' for Customer

    [JsonPropertyName("invoiceNumber")]
    public string? InvoiceNumber { get; set; }

    [JsonPropertyName("dueDate")]
    public string? DueDate { get; set; }

    [JsonPropertyName("paymentMethod")]
    public string? PaymentMethod { get; set; }
}

/// <summary>
/// Transaction line detail.
/// </summary>
public class TransactionDetailInformation
{
    [JsonPropertyName("sequenceNumber")]
    public int SequenceNumber { get; set; }

    [JsonPropertyName("lineType")]
    public string? LineType { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("valueDate")]
    public string? ValueDate { get; set; }

    [JsonPropertyName("accountingInformation")]
    public AccountingInformation? AccountingInformation { get; set; }

    [JsonPropertyName("amounts")]
    public AmountsDto? Amounts { get; set; }

    [JsonPropertyName("taxInformation")]
    public TaxInformation? TaxInformation { get; set; }

    [JsonPropertyName("additionalInformation")]
    public AdditionalInformation? AdditionalInformation { get; set; }

    [JsonPropertyName("statisticalInformation")]
    public StatisticalInformation? StatisticalInformation { get; set; }
}

/// <summary>
/// GL account and dimension coding.
/// </summary>
public class AccountingInformation
{
    [JsonPropertyName("account")]
    public string? Account { get; set; }

    [JsonPropertyName("accountingDimension1")]
    public string? AccountingDimension1 { get; set; }

    [JsonPropertyName("accountingDimension2")]
    public string? AccountingDimension2 { get; set; }

    [JsonPropertyName("accountingDimension3")]
    public string? AccountingDimension3 { get; set; }

    [JsonPropertyName("accountingDimension4")]
    public string? AccountingDimension4 { get; set; }

    [JsonPropertyName("accountingDimension5")]
    public string? AccountingDimension5 { get; set; }

    [JsonPropertyName("accountingDimension6")]
    public string? AccountingDimension6 { get; set; }

    [JsonPropertyName("accountingDimension7")]
    public string? AccountingDimension7 { get; set; }
}

/// <summary>
/// Amount details for a transaction line.
/// </summary>
public class AmountsDto
{
    [JsonPropertyName("debitCreditFlag")]
    public string? DebitCreditFlag { get; set; } // 'D' or 'C'

    [JsonPropertyName("amount")]
    public decimal? Amount { get; set; }

    [JsonPropertyName("currencyAmount")]
    public decimal? CurrencyAmount { get; set; }

    [JsonPropertyName("currencyCode")]
    public string? CurrencyCode { get; set; }
}

/// <summary>
/// Tax related information.
/// </summary>
public class TaxInformation
{
    [JsonPropertyName("taxCode")]
    public string? TaxCode { get; set; }

    [JsonPropertyName("taxSystem")]
    public string? TaxSystem { get; set; }

    [JsonPropertyName("taxDetails")]
    public TaxDetails? TaxDetails { get; set; }
}

/// <summary>
/// Detailed tax amounts.
/// </summary>
public class TaxDetails
{
    [JsonPropertyName("taxAccount")]
    public string? TaxAccount { get; set; }

    [JsonPropertyName("baseAmount")]
    public decimal? BaseAmount { get; set; }

    [JsonPropertyName("baseCurrencyCode")]
    public string? BaseCurrencyCode { get; set; }

    [JsonPropertyName("taxAmount")]
    public decimal? TaxAmount { get; set; }

    [JsonPropertyName("taxCurrencyCode")]
    public string? TaxCurrencyCode { get; set; }
}

/// <summary>
/// Statistical/numerical information.
/// </summary>
public class StatisticalInformation
{
    [JsonPropertyName("number1")]
    public decimal? Number1 { get; set; }

    [JsonPropertyName("value1")]
    public decimal? Value1 { get; set; }

    [JsonPropertyName("value2")]
    public decimal? Value2 { get; set; }

    [JsonPropertyName("value3")]
    public decimal? Value3 { get; set; }
}

/// <summary>
/// Additional processing information.
/// </summary>
public class AdditionalInformation
{
    [JsonPropertyName("distributionKey")]
    public string? DistributionKey { get; set; }

    [JsonPropertyName("periodNumber")]
    public string? PeriodNumber { get; set; }

    [JsonPropertyName("sequenceReference")]
    public string? SequenceReference { get; set; }

    [JsonPropertyName("text1")]
    public string? Text1 { get; set; }

    [JsonPropertyName("text2")]
    public string? Text2 { get; set; }

    [JsonPropertyName("text3")]
    public string? Text3 { get; set; }

    [JsonPropertyName("text4")]
    public string? Text4 { get; set; }

    [JsonPropertyName("text5")]
    public string? Text5 { get; set; }
}
