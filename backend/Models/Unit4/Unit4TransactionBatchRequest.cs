using System.Text.Json.Serialization;

namespace SfabGl07Gateway.Api.Models.Unit4;

/// <summary>
/// Root request object for posting financial transactions to Unit4 API.
/// POST /v1/financial-transaction-batch
/// </summary>
public class Unit4TransactionBatchRequest
{
    [JsonPropertyName("notificationMessages")]
    public object? NotificationMessages { get; set; }

    [JsonPropertyName("batchInformation")]
    public BatchInformation? BatchInformation { get; set; }

    [JsonPropertyName("transactionInformation")]
    public TransactionInformation? TransactionInformation { get; set; }
}

/// <summary>
/// Batch header information.
/// </summary>
public class BatchInformation
{
    [JsonPropertyName("notificationMessages")]
    public object? NotificationMessages { get; set; }

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
    [JsonPropertyName("notificationMessages")]
    public object? NotificationMessages { get; set; }

    [JsonPropertyName("companyId")]
    public string? CompanyId { get; set; }

    [JsonPropertyName("period")]
    public int? Period { get; set; }

    [JsonPropertyName("transactionDate")]
    public DateTime? TransactionDate { get; set; }

    [JsonPropertyName("transactionType")]
    public string? TransactionType { get; set; }

    [JsonPropertyName("registeredDate")]
    public DateTime? RegisteredDate { get; set; }

    [JsonPropertyName("registeredTransactionNumber")]
    public int? RegisteredTransactionNumber { get; set; }

    [JsonPropertyName("transactionNumber")]
    public int? TransactionNumber { get; set; }

    [JsonPropertyName("invoice")]
    public Invoice? Invoice { get; set; }

    [JsonPropertyName("transactionDetailInformation")]
    public TransactionDetailInformation? TransactionDetailInformation { get; set; }

    [JsonPropertyName("additionalInformation")]
    public TransactionAdditionalInformation? AdditionalInformation { get; set; }
}

/// <summary>
/// Invoice/AP/AR related information.
/// </summary>
public class Invoice
{
    [JsonPropertyName("notificationMessages")]
    public object? NotificationMessages { get; set; }

    [JsonPropertyName("customerOrSupplierId")]
    public string? CustomerOrSupplierId { get; set; }

    [JsonPropertyName("ledgerType")]
    public string? LedgerType { get; set; } // 's' for Supplier, 'c' for Customer

    [JsonPropertyName("companyReference")]
    public string? CompanyReference { get; set; }

    [JsonPropertyName("contractId")]
    public string? ContractId { get; set; }

    [JsonPropertyName("discountDate")]
    public DateTime? DiscountDate { get; set; }

    [JsonPropertyName("currencyAmountDiscount")]
    public decimal? CurrencyAmountDiscount { get; set; }

    [JsonPropertyName("dueDate")]
    public DateTime? DueDate { get; set; }

    [JsonPropertyName("invoiceNumber")]
    public string? InvoiceNumber { get; set; }

    [JsonPropertyName("externalReference")]
    public string? ExternalReference { get; set; }

    [JsonPropertyName("kid")]
    public string? Kid { get; set; }

    [JsonPropertyName("orderNumber")]
    public int? OrderNumber { get; set; }

    [JsonPropertyName("invoiceReference")]
    public int? InvoiceReference { get; set; }

    [JsonPropertyName("payRecipient")]
    public string? PayRecipient { get; set; }

    [JsonPropertyName("paymentCurrency")]
    public string? PaymentCurrency { get; set; }

    [JsonPropertyName("paymentOnAccount")]
    public bool? PaymentOnAccount { get; set; }

    [JsonPropertyName("paymentMethod")]
    public string? PaymentMethod { get; set; }

    [JsonPropertyName("paymentTransfer")]
    public string? PaymentTransfer { get; set; }

    [JsonPropertyName("responsible")]
    public string? Responsible { get; set; }
}

/// <summary>
/// Transaction line detail.
/// </summary>
public class TransactionDetailInformation
{
    [JsonPropertyName("notificationMessages")]
    public object? NotificationMessages { get; set; }

    [JsonPropertyName("sequenceNumber")]
    public int? SequenceNumber { get; set; }

    [JsonPropertyName("lineType")]
    public string? LineType { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("valueDate")]
    public DateTime? ValueDate { get; set; }

    [JsonPropertyName("accountingInformation")]
    public AccountingInformation? AccountingInformation { get; set; }

    [JsonPropertyName("amounts")]
    public Amounts? Amounts { get; set; }

    [JsonPropertyName("taxInformation")]
    public TaxInformation? TaxInformation { get; set; }

    [JsonPropertyName("statisticalInformation")]
    public StatisticalInformation? StatisticalInformation { get; set; }
}

/// <summary>
/// GL account and dimension coding.
/// </summary>
public class AccountingInformation
{
    [JsonPropertyName("notificationMessages")]
    public object? NotificationMessages { get; set; }

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
public class Amounts
{
    [JsonPropertyName("notificationMessages")]
    public object? NotificationMessages { get; set; }

    [JsonPropertyName("debitCreditFlag")]
    public int? DebitCreditFlag { get; set; } // 0 = Debit, 1 = Credit

    [JsonPropertyName("amount")]
    public decimal? Amount { get; set; }

    [JsonPropertyName("amount3")]
    public decimal? Amount3 { get; set; }

    [JsonPropertyName("amount4")]
    public decimal? Amount4 { get; set; }

    [JsonPropertyName("currencyAmount")]
    public decimal? CurrencyAmount { get; set; }

    [JsonPropertyName("vatAmount")]
    public decimal? VatAmount { get; set; }

    [JsonPropertyName("vatAmount3")]
    public decimal? VatAmount3 { get; set; }

    [JsonPropertyName("vatAmount4")]
    public decimal? VatAmount4 { get; set; }

    [JsonPropertyName("vatCurrencyAmount")]
    public decimal? VatCurrencyAmount { get; set; }

    [JsonPropertyName("currencyCode")]
    public string? CurrencyCode { get; set; }

    [JsonPropertyName("exchangeRate")]
    public decimal? ExchangeRate { get; set; }

    [JsonPropertyName("exchangeRate3")]
    public decimal? ExchangeRate3 { get; set; }

    [JsonPropertyName("exchangeRate4")]
    public decimal? ExchangeRate4 { get; set; }
}

/// <summary>
/// Tax related information.
/// </summary>
public class TaxInformation
{
    [JsonPropertyName("notificationMessages")]
    public object? NotificationMessages { get; set; }

    [JsonPropertyName("taxPointDate")]
    public DateTime? TaxPointDate { get; set; }

    [JsonPropertyName("taxCode")]
    public string? TaxCode { get; set; }

    [JsonPropertyName("taxSystem")]
    public string? TaxSystem { get; set; }

    [JsonPropertyName("vatPercentage")]
    public decimal? VatPercentage { get; set; }

    [JsonPropertyName("reductionCode")]
    public string? ReductionCode { get; set; }

    [JsonPropertyName("taxDetails")]
    public TaxDetails? TaxDetails { get; set; }
}

/// <summary>
/// Detailed tax amounts.
/// </summary>
public class TaxDetails
{
    [JsonPropertyName("notificationMessages")]
    public object? NotificationMessages { get; set; }

    [JsonPropertyName("taxBaseAccount")]
    public string? TaxBaseAccount { get; set; }

    [JsonPropertyName("baseAmount")]
    public decimal? BaseAmount { get; set; }

    [JsonPropertyName("baseAmount3")]
    public decimal? BaseAmount3 { get; set; }

    [JsonPropertyName("baseAmount4")]
    public decimal? BaseAmount4 { get; set; }

    [JsonPropertyName("baseCurrencyAmount")]
    public decimal? BaseCurrencyAmount { get; set; }

    [JsonPropertyName("vatReverseChargeAmount")]
    public decimal? VatReverseChargeAmount { get; set; }

    [JsonPropertyName("vatReverseChargeAmount3")]
    public decimal? VatReverseChargeAmount3 { get; set; }

    [JsonPropertyName("vatReverseChargeAmount4")]
    public decimal? VatReverseChargeAmount4 { get; set; }

    [JsonPropertyName("vatReverseChargeCurrencyAmount")]
    public decimal? VatReverseChargeCurrencyAmount { get; set; }

    [JsonPropertyName("originalAmount")]
    public decimal? OriginalAmount { get; set; }

    [JsonPropertyName("originalAmount3")]
    public decimal? OriginalAmount3 { get; set; }

    [JsonPropertyName("originalAmount4")]
    public decimal? OriginalAmount4 { get; set; }

    [JsonPropertyName("originalCurrencyAmount")]
    public decimal? OriginalCurrencyAmount { get; set; }

    [JsonPropertyName("originalBaseAmount")]
    public decimal? OriginalBaseAmount { get; set; }

    [JsonPropertyName("originalBaseAmount3")]
    public decimal? OriginalBaseAmount3 { get; set; }

    [JsonPropertyName("originalBaseAmount4")]
    public decimal? OriginalBaseAmount4 { get; set; }

    [JsonPropertyName("originalBaseCurrency")]
    public decimal? OriginalBaseCurrency { get; set; }

    [JsonPropertyName("reductionPercentage")]
    public decimal? ReductionPercentage { get; set; }

    [JsonPropertyName("reductionPercentageTaxCode")]
    public decimal? ReductionPercentageTaxCode { get; set; }

    [JsonPropertyName("reductionPercentageTaxSystem")]
    public decimal? ReductionPercentageTaxSystem { get; set; }

    [JsonPropertyName("reductionPercentageReductionCode")]
    public decimal? ReductionPercentageReductionCode { get; set; }

    [JsonPropertyName("taxSequenceReference")]
    public int? TaxSequenceReference { get; set; }
}

/// <summary>
/// Statistical/numerical information.
/// </summary>
public class StatisticalInformation
{
    [JsonPropertyName("notificationMessages")]
    public object? NotificationMessages { get; set; }

    [JsonPropertyName("number")]
    public int? Number { get; set; }

    [JsonPropertyName("value")]
    public decimal? Value { get; set; }
}

/// <summary>
/// Additional processing information for transaction.
/// </summary>
public class TransactionAdditionalInformation
{
    [JsonPropertyName("notificationMessages")]
    public object? NotificationMessages { get; set; }

    [JsonPropertyName("distributionKey")]
    public int? DistributionKey { get; set; }

    [JsonPropertyName("periodNumber")]
    public int? PeriodNumber { get; set; }

    [JsonPropertyName("commitmentId")]
    public string? CommitmentId { get; set; }

    [JsonPropertyName("complaintDelay")]
    public DateTime? ComplaintDelay { get; set; }

    [JsonPropertyName("complaintCode")]
    public string? ComplaintCode { get; set; }

    [JsonPropertyName("currencyDocumentation")]
    public string? CurrencyDocumentation { get; set; }

    [JsonPropertyName("currencyLicenseCode")]
    public string? CurrencyLicenseCode { get; set; }

    [JsonPropertyName("externalArchiveReference")]
    public string? ExternalArchiveReference { get; set; }

    [JsonPropertyName("interestRuleId")]
    public string? InterestRuleId { get; set; }

    [JsonPropertyName("paymentPlanId")]
    public int? PaymentPlanId { get; set; }

    [JsonPropertyName("paymentPlanTemplateCode")]
    public string? PaymentPlanTemplateCode { get; set; }

    [JsonPropertyName("sequenceReference")]
    public int? SequenceReference { get; set; }

    [JsonPropertyName("transactionNumberReference")]
    public int? TransactionNumberReference { get; set; }

    [JsonPropertyName("reminderLevel")]
    public string? ReminderLevel { get; set; }

    [JsonPropertyName("remittanceId")]
    public int? RemittanceId { get; set; }

    [JsonPropertyName("collection")]
    public bool? Collection { get; set; }

    [JsonPropertyName("isSecondaryOpenItem")]
    public bool? IsSecondaryOpenItem { get; set; }

    [JsonPropertyName("taxId")]
    public bool? TaxId { get; set; }
}
