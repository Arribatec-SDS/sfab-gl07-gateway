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

    public Unit4TransactionBatchRequest Transform(string fileContent)
    {
        _logger.LogDebug("Starting ABWTransaction transformation");

        var abwTransaction = _xmlParser.Parse(fileContent);

        var request = new Unit4TransactionBatchRequest
        {
            BatchInformation = new BatchInformation
            {
                Interface = abwTransaction.Interface,
                BatchId = abwTransaction.BatchId
            },
            TransactionInformation = new List<TransactionInformation>()
        };

        if (abwTransaction.Vouchers == null || abwTransaction.Vouchers.Count == 0)
        {
            _logger.LogWarning("No vouchers found in ABWTransaction");
            return request;
        }

        foreach (var voucher in abwTransaction.Vouchers)
        {
            var transactionInfo = MapVoucherToTransactionInfo(voucher);
            request.TransactionInformation.Add(transactionInfo);
        }

        _logger.LogInformation("Transformed {VoucherCount} vouchers to Unit4 format",
            request.TransactionInformation.Count);

        return request;
    }

    private TransactionInformation MapVoucherToTransactionInfo(Voucher voucher)
    {
        var transactionInfo = new TransactionInformation
        {
            CompanyId = voucher.CompanyCode,
            Period = voucher.Period,
            TransactionDate = voucher.VoucherDate,
            VoucherNumber = voucher.VoucherNo,
            VoucherType = voucher.VoucherType,
            Description = voucher.Description,
            TransactionDetailInformation = new List<TransactionDetailInformation>()
        };

        if (voucher.Transactions == null || voucher.Transactions.Count == 0)
        {
            return transactionInfo;
        }

        var sequenceNumber = 1;
        foreach (var transaction in voucher.Transactions)
        {
            var detailInfo = MapTransactionToDetailInfo(transaction, sequenceNumber++);
            transactionInfo.TransactionDetailInformation.Add(detailInfo);

            // Set invoice info from first transaction that has AP/AR info
            if (transactionInfo.Invoice == null && transaction.ApArInfo != null)
            {
                transactionInfo.Invoice = MapApArToInvoice(transaction.ApArInfo);
                transactionInfo.TransactionType = transaction.TransType;
            }
        }

        return transactionInfo;
    }

    private TransactionDetailInformation MapTransactionToDetailInfo(Transaction transaction, int sequenceNumber)
    {
        var detail = new TransactionDetailInformation
        {
            SequenceNumber = transaction.SequenceNo ?? sequenceNumber,
            LineType = transaction.TransType,
            Description = transaction.Description,
            ValueDate = transaction.TransDate,
            AccountingInformation = MapGLAnalysisToAccountingInfo(transaction.GLAnalysis),
            Amounts = MapAmounts(transaction.Amounts, transaction.GLAnalysis?.Currency),
            TaxInformation = MapTaxInfo(transaction.GLAnalysis, transaction.TaxTransInfo),
            StatisticalInformation = MapStatisticalInfo(transaction.Amounts),
            AdditionalInformation = MapAdditionalInfo(transaction.ApArInfo?.SundryInfo)
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

    private AmountsDto? MapAmounts(Amounts? amounts, string? currency)
    {
        if (amounts == null) return null;

        return new AmountsDto
        {
            DebitCreditFlag = amounts.DcFlag,
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
            LedgerType = apArInfo.ApArType,
            InvoiceNumber = apArInfo.InvoiceNo,
            DueDate = apArInfo.DueDate,
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
                TaxAccount = taxTransInfo.Account2,
                BaseAmount = taxTransInfo.BaseAmount,
                BaseCurrencyCode = taxTransInfo.BaseCurr,
                TaxAmount = taxTransInfo.TaxAmount,
                TaxCurrencyCode = taxTransInfo.TaxCurr
            } : null
        };
    }

    private StatisticalInformation? MapStatisticalInfo(Amounts? amounts)
    {
        if (amounts == null ||
            (amounts.Number1 == null && amounts.Value1 == null &&
             amounts.Value2 == null && amounts.Value3 == null))
        {
            return null;
        }

        return new StatisticalInformation
        {
            Number1 = amounts.Number1,
            Value1 = amounts.Value1,
            Value2 = amounts.Value2,
            Value3 = amounts.Value3
        };
    }

    private AdditionalInformation? MapAdditionalInfo(SundryInfo? sundryInfo)
    {
        if (sundryInfo == null) return null;

        return new AdditionalInformation
        {
            Text1 = sundryInfo.Text1,
            Text2 = sundryInfo.Text2,
            Text3 = sundryInfo.Text3,
            Text4 = sundryInfo.Text4,
            Text5 = sundryInfo.Text5
        };
    }
}
