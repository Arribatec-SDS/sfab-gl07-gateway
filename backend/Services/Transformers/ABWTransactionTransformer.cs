using SfabGl07Gateway.Api.Models.Settings;
using SfabGl07Gateway.Api.Models.Unit4;
using SfabGl07Gateway.Api.Models.Xml;

namespace SfabGl07Gateway.Api.Services.Transformers;

/// <summary>
/// Transformer for ABWTransaction XML format to Unit4 transaction batch.
/// This is the default transformer for standard Agresso XML files.
/// </summary>
public class ABWTransactionTransformer : ITransformationService
{
    private readonly IXmlParserService _xmlParser;
    private readonly ILogger<ABWTransactionTransformer> _logger;

    public string TransformerType => "ABWTransaction";

    public ABWTransactionTransformer(
        IXmlParserService xmlParser,
        ILogger<ABWTransactionTransformer> logger)
    {
        _xmlParser = xmlParser;
        _logger = logger;
    }

    public bool CanHandle(string fileContent)
    {
        return !string.IsNullOrEmpty(fileContent) &&
               fileContent.Contains("ABWTransaction") &&
               fileContent.Contains("http://services.agresso.com/schema/ABWTransaction");
    }

    public List<Unit4TransactionBatchRequest> Transform(string fileContent, SourceSystem sourceSystem)
    {
        _logger.LogDebug("Starting ABWTransaction transformation for source system {SystemCode}", sourceSystem.SystemCode);

        var abwTransaction = _xmlParser.Parse(fileContent);
        var results = new List<Unit4TransactionBatchRequest>();

        // Determine interface: use SourceSystem.Interface if set, otherwise use from XML
        var interfaceValue = !string.IsNullOrWhiteSpace(sourceSystem.Interface)
            ? sourceSystem.Interface
            : abwTransaction.Interface;

        // Determine batchId: 
        // - If SourceSystem.BatchId is set, use it as prefix with timestamp: {prefix}-{yyMMddHHmmssff}
        // - Otherwise, use batchId from XML or generate a default
        string batchId;
        if (!string.IsNullOrWhiteSpace(sourceSystem.BatchId))
        {
            batchId = $"{sourceSystem.BatchId}-{DateTime.UtcNow:yyMMddHHmmssff}";
        }
        else
        {
            batchId = abwTransaction.BatchId ?? $"BATCH-{DateTime.UtcNow:yyMMddHHmmssff}";
        }

        _logger.LogDebug("Using Interface: {Interface}, BatchId: {BatchId}", interfaceValue, batchId);

        // BatchInformation is the SAME for all rows
        var batchInfo = new BatchInformation
        {
            Interface = interfaceValue,
            BatchId = batchId
        };

        if (abwTransaction.Vouchers == null || abwTransaction.Vouchers.Count == 0)
        {
            _logger.LogWarning("No vouchers found in ABWTransaction");
            return results;
        }

        // Track auto-incrementing voucher number (used when VoucherNo not in XML)
        int autoVoucherNumber = 1;

        // Flatten: each Transaction becomes one array item
        foreach (var voucher in abwTransaction.Vouchers)
        {
            // Determine transactionNumber: use VoucherNo if exists, otherwise auto-increment
            int transactionNumber;
            if (!string.IsNullOrWhiteSpace(voucher.VoucherNo) && int.TryParse(voucher.VoucherNo, out var parsedVoucherNo))
            {
                transactionNumber = parsedVoucherNo;
            }
            else
            {
                transactionNumber = autoVoucherNumber++;
            }

            // Determine transactionType: use SourceSystem.TransactionType if set, otherwise use XML voucherType
            var transactionType = !string.IsNullOrWhiteSpace(sourceSystem.TransactionType)
                ? sourceSystem.TransactionType
                : voucher.VoucherType;

            if (voucher.Transactions == null || voucher.Transactions.Count == 0)
            {
                _logger.LogWarning("Voucher {VoucherNo} has no transactions", voucher.VoucherNo);
                continue;
            }

            // SequenceNumber starts at 0 and resets per voucher
            int sequenceNumber = 0;

            foreach (var transaction in voucher.Transactions)
            {
                var request = new Unit4TransactionBatchRequest
                {
                    BatchInformation = batchInfo,
                    TransactionInformation = new TransactionInformation
                    {
                        CompanyId = voucher.CompanyCode,
                        Period = ParsePeriod(voucher.Period),
                        TransactionDate = ParseDate(voucher.VoucherDate),
                        TransactionType = transactionType,
                        TransactionNumber = transactionNumber,
                        Invoice = MapApArToInvoice(transaction.ApArInfo),
                        TransactionDetailInformation = MapTransactionToDetailInfo(transaction, sequenceNumber),
                        AdditionalInformation = MapAdditionalInfo(transaction)
                    }
                };

                results.Add(request);
                sequenceNumber++;
            }

            _logger.LogDebug("Mapped voucher {VoucherNo} with {Count} transactions",
                voucher.VoucherNo ?? transactionNumber.ToString(), voucher.Transactions.Count);
        }

        _logger.LogInformation("Transformed {VoucherCount} vouchers to {RowCount} Unit4 rows",
            abwTransaction.Vouchers.Count, results.Count);

        return results;
    }

    private TransactionAdditionalInformation? MapAdditionalInfo(Transaction transaction)
    {
        var apAr = transaction.ApArInfo;
        var glAnalysis = transaction.GLAnalysis;

        // Only create if we have relevant data
        if (apAr == null && transaction.AllocationKey == null && transaction.PeriodNo == null && glAnalysis?.TaxId == null)
        {
            return null;
        }

        return new TransactionAdditionalInformation
        {
            DistributionKey = ParseInt(transaction.AllocationKey),
            PeriodNumber = transaction.PeriodNo,
            CommitmentId = apAr?.Commitment,
            ComplaintCode = apAr?.ComplaintCode,
            ComplaintDelay = ParseDate(apAr?.ComplaintDate),
            CurrencyLicenseCode = apAr?.CurrLicense,
            InterestRuleId = apAr?.IntruleId,
            PaymentPlanTemplateCode = apAr?.PayTemplate,
            SequenceReference = ParseInt(apAr?.SequenceRef),
            TaxId = glAnalysis?.TaxId == 1
        };
    }

    private int? ParseInt(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return int.TryParse(value, out var result) ? result : null;
    }

    private int? ParsePeriod(string? period)
    {
        if (string.IsNullOrWhiteSpace(period)) return null;
        return int.TryParse(period, out var result) ? result : null;
    }

    private DateTime? ParseDate(string? dateStr)
    {
        if (string.IsNullOrWhiteSpace(dateStr)) return null;
        return DateTime.TryParse(dateStr, out var result) ? result : null;
    }

    private TransactionDetailInformation MapTransactionToDetailInfo(Transaction transaction, int sequenceNumber)
    {
        return new TransactionDetailInformation
        {
            SequenceNumber = sequenceNumber,
            LineType = transaction.TransType,
            Description = transaction.Description,
            Status = transaction.Status,
            ValueDate = ParseDate(transaction.TransDate),
            AccountingInformation = MapGLAnalysisToAccountingInfo(transaction.GLAnalysis),
            Amounts = MapAmounts(transaction.Amounts, transaction.GLAnalysis?.Currency),
            TaxInformation = MapTaxInfo(transaction.GLAnalysis, transaction.TaxTransInfo),
            StatisticalInformation = MapStatisticalInfo(transaction.Amounts)
        };
    }

    private AccountingInformation? MapGLAnalysisToAccountingInfo(GLAnalysis? glAnalysis)
    {
        if (glAnalysis == null) return null;

        return new AccountingInformation
        {
            Account = glAnalysis.Account,
            AccountingDimension1 = glAnalysis.Dim1,
            AccountingDimension2 = glAnalysis.Dim2,
            AccountingDimension3 = glAnalysis.Dim3,
            AccountingDimension4 = glAnalysis.Dim4,
            AccountingDimension5 = glAnalysis.Dim5,
            AccountingDimension6 = glAnalysis.Dim6,
            AccountingDimension7 = glAnalysis.Dim7
        };
    }

    private Models.Unit4.Amounts? MapAmounts(Models.Xml.Amounts? amounts, string? currency)
    {
        if (amounts == null) return null;

        return new Models.Unit4.Amounts
        {
            DebitCreditFlag = amounts.DcFlag, // 1 = Debit, -1 = Credit per XSD
            Amount = amounts.Amount,
            CurrencyAmount = amounts.CurrAmount,
            CurrencyCode = currency
        };
    }

    private Invoice? MapApArToInvoice(ApArInfo? apArInfo)
    {
        if (apArInfo == null) return null;

        return new Invoice
        {
            CustomerOrSupplierId = apArInfo.ApArNo,
            LedgerType = apArInfo.ApArType?.ToLowerInvariant(),
            InvoiceNumber = apArInfo.InvoiceNo,
            DueDate = ParseDate(apArInfo.DueDate),
            DiscountDate = ParseDate(apArInfo.DiscDate),
            PaymentMethod = apArInfo.PayMethod,
            Responsible = apArInfo.Responsible,
            PayRecipient = apArInfo.FactorShort,
            PaymentCurrency = apArInfo.PayCurrency,
            ExternalReference = apArInfo.OrigReference,
            OrderNumber = ParseInt(apArInfo.OrderNo),
            InvoiceReference = ParseInt(apArInfo.VoucherRef)
        };
    }

    private TaxInformation? MapTaxInfo(GLAnalysis? glAnalysis, TaxTransInfo? taxTransInfo)
    {
        if (glAnalysis?.TaxCode == null && taxTransInfo == null) return null;

        return new TaxInformation
        {
            TaxCode = glAnalysis?.TaxCode,
            TaxSystem = glAnalysis?.TaxSystem,
            TaxDetails = taxTransInfo != null ? new TaxDetails
            {
                TaxBaseAccount = taxTransInfo.Account2,
                BaseAmount = taxTransInfo.BaseAmount,
                BaseCurrencyAmount = taxTransInfo.BaseCurr
            } : null
        };
    }

    private decimal? ParseDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return decimal.TryParse(value, out var result) ? result : null;
    }

    private StatisticalInformation? MapStatisticalInfo(Models.Xml.Amounts? amounts)
    {
        if (amounts == null ||
            (amounts.Number1 == null && amounts.Value1 == null))
        {
            return null;
        }

        return new StatisticalInformation
        {
            Number = amounts.Number1.HasValue ? (int)amounts.Number1.Value : null,
            Value = amounts.Value1
        };
    }
}
