using System.Xml;
using System.Xml.Serialization;
using SfabGl07Gateway.Api.Models.Xml;

namespace SfabGl07Gateway.Api.Services;

/// <summary>
/// Service implementation for parsing ABWTransaction XML documents.
/// </summary>
public class XmlParserService : IXmlParserService
{
    private readonly ILogger<XmlParserService> _logger;
    private readonly XmlSerializer _serializer;

    public XmlParserService(ILogger<XmlParserService> logger)
    {
        _logger = logger;
        _serializer = new XmlSerializer(typeof(ABWTransaction));
    }

    public ABWTransaction Parse(string xmlContent)
    {
        if (string.IsNullOrWhiteSpace(xmlContent))
        {
            throw new ArgumentException("XML content cannot be empty", nameof(xmlContent));
        }

        _logger.LogDebug("Parsing XML content ({Length} characters)", xmlContent.Length);

        try
        {
            using var stringReader = new StringReader(xmlContent);
            using var xmlReader = XmlReader.Create(stringReader, new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null
            });

            var result = _serializer.Deserialize(xmlReader) as ABWTransaction;

            if (result == null)
            {
                throw new InvalidOperationException("Failed to deserialize XML content");
            }

            var voucherCount = result.Vouchers?.Count ?? 0;
            var transactionCount = result.Vouchers?
                .Sum(v => v.Transactions?.Count ?? 0) ?? 0;

            _logger.LogInformation("Parsed XML: {VoucherCount} vouchers, {TransactionCount} transactions",
                voucherCount, transactionCount);

            return result;
        }
        catch (InvalidOperationException ex) when (ex.InnerException is XmlException)
        {
            _logger.LogError(ex, "XML parsing error");
            throw new InvalidOperationException($"Invalid XML format: {ex.InnerException.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error parsing XML");
            throw;
        }
    }

    public ABWTransaction ParseStream(Stream stream)
    {
        if (stream == null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        _logger.LogDebug("Parsing XML from stream");

        try
        {
            using var xmlReader = XmlReader.Create(stream, new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null
            });

            var result = _serializer.Deserialize(xmlReader) as ABWTransaction;

            if (result == null)
            {
                throw new InvalidOperationException("Failed to deserialize XML stream");
            }

            var voucherCount = result.Vouchers?.Count ?? 0;
            var transactionCount = result.Vouchers?
                .Sum(v => v.Transactions?.Count ?? 0) ?? 0;

            _logger.LogInformation("Parsed XML stream: {VoucherCount} vouchers, {TransactionCount} transactions",
                voucherCount, transactionCount);

            return result;
        }
        catch (InvalidOperationException ex) when (ex.InnerException is XmlException)
        {
            _logger.LogError(ex, "XML parsing error");
            throw new InvalidOperationException($"Invalid XML format: {ex.InnerException.Message}", ex);
        }
    }
}
