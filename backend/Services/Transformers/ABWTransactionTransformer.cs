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

    public Unit4TransactionBatchRequest Transform(string fileContent, SourceSystem sourceSystem)
    {
        _logger.LogDebug("Starting ABWTransaction transformation for source system {SystemCode}", sourceSystem.SystemCode);

        var abwTransaction = _xmlParser.Parse(fileContent);

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
            // BatchId is configured as a prefix (max 10 chars) - append timestamp for uniqueness
            batchId = $"{sourceSystem.BatchId}-{DateTime.UtcNow:yyMMddHHmmssff}";
        }
        else
        {
            // Use batchId from source file, or generate default if not present
            batchId = abwTransaction.BatchId ?? $"BATCH-{DateTime.UtcNow:yyMMddHHmmssff}";
        }

        _logger.LogDebug("Using Interface: {Interface} (Override: {IsOverride}), BatchId: {BatchId} (Prefix: {IsPrefixMode})",
            interfaceValue, !string.IsNullOrWhiteSpace(sourceSystem.Interface),
            batchId, !string.IsNullOrWhiteSpace(sourceSystem.BatchId));

        var request = new Unit4TransactionBatchRequest
        {
            BatchInformation = new BatchInformation
            {
                Interface = interfaceValue,
                BatchId = batchId
            }
        };

        if (abwTransaction.Vouchers == null || abwTransaction.Vouchers.Count == 0)
        {
            _logger.LogWarning("No vouchers found in ABWTransaction");
            return request;
        }

        // For the correct JSON structure, we have a single TransactionInformation
        // If there are multiple vouchers, we need to handle differently based on requirements
        // For now, take the first voucher as the main transaction
        var firstVoucher = abwTransaction.Vouchers.First();
        request.TransactionInformation = MapVoucherToTransactionInfo(firstVoucher, sourceSystem);

        _logger.LogInformation("Transformed voucher to Unit4 format (VoucherNo: {VoucherNo})",
            firstVoucher.VoucherNo);

        return request;
    }

    private TransactionInformation MapVoucherToTransactionInfo(Voucher voucher, SourceSystem sourceSystem)
    {
        // Determine transactionType: use SourceSystem.TransactionType if set, otherwise use XML voucherType
        var transactionType = !string.IsNullOrWhiteSpace(sourceSystem.TransactionType)
            ? sourceSystem.TransactionType
            : voucher.VoucherType;

        var transactionInfo = new TransactionInformation
        {
            CompanyId = voucher.CompanyCode,
            Period = ParsePeriod(voucher.Period),
            TransactionDate = ParseDate(voucher.VoucherDate),
            TransactionType = transactionType
        };

        if (voucher.Transactions != null && voucher.Transactions.Count > 0)
        {
            var firstTransaction = voucher.Transactions.First();

            // Map first transaction to detail info
            transactionInfo.TransactionDetailInformation = MapTransactionToDetailInfo(firstTransaction, 1);

            // Set invoice info if AP/AR info exists
            if (firstTransaction.ApArInfo != null)
            {
                transactionInfo.Invoice = MapApArToInvoice(firstTransaction.ApArInfo);
            }
        }

        return transactionInfo;
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
        var detail = new TransactionDetailInformation
        {
            SequenceNumber = transaction.SequenceNo ?? sequenceNumber,
            LineType = transaction.TransType,
            Description = transaction.Description,
            ValueDate = ParseDate(transaction.TransDate),
            AccountingInformation = MapGLAnalysisToAccountingInfo(transaction.GLAnalysis),
            Amounts = MapAmounts(transaction.Amounts, transaction.GLAnalysis?.Currency),
            TaxInformation = MapTaxInfo(transaction.GLAnalysis, transaction.TaxTransInfo),
            StatisticalInformation = MapStatisticalInfo(transaction.Amounts)
        };

        return detail;
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
            DebitCreditFlag = ParseDebitCreditFlag(amounts.DcFlag),
            Amount = amounts.Amount,
            CurrencyAmount = amounts.CurrAmount,
            CurrencyCode = currency
        };
    }

    private int? ParseDebitCreditFlag(string? dcFlag)
    {
        // Convert D/C to 0/1
        if (string.IsNullOrWhiteSpace(dcFlag)) return null;
        return dcFlag.Equals("D", StringComparison.OrdinalIgnoreCase) ? 0 : 1;
    }

    private Invoice? MapApArToInvoice(ApArInfo? apArInfo)
    {
        if (apArInfo == null) return null;

        return new Invoice
        {
            CustomerOrSupplierId = apArInfo.ApArNo,
            LedgerType = apArInfo.ApArType?.ToLowerInvariant(), // 's' for supplier, 'c' for customer
            InvoiceNumber = apArInfo.InvoiceNo,
            DueDate = ParseDate(apArInfo.DueDate),
            PaymentMethod = apArInfo.PayMethod
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
                BaseCurrencyAmount = taxTransInfo.BaseCurr != null ? ParseDecimal(taxTransInfo.BaseCurr) : null
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
