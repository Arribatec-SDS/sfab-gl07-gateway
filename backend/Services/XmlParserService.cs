using System.Xml;
using System.Xml.Serialization;
using SfabGl07Gateway.Api.Models.Xml;

namespace SfabGl07Gateway.Api.Services;

/// <summary>
/// Service implementation for parsing ABWTransaction XML documents.
/// Supports both 2005 and 2011 namespace versions.
/// </summary>
public class XmlParserService : IXmlParserService
{
    private readonly ILogger<XmlParserService> _logger;
    private readonly XmlSerializer _serializer;

    // Old 2005 namespace versions (found in some Agresso exports)
    private const string OldTransactionNamespace = "http://services.agresso.com/schema/ABWTransaction/2005/05/13";
    private const string OldSchemaLibNamespace = "http://services.agresso.com/schema/ABWSchemaLib/2005/05/13";

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
            // Normalize old 2005 namespaces to 2011 namespaces
            var normalizedXml = NormalizeNamespaces(xmlContent);

            using var stringReader = new StringReader(normalizedXml);
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
        catch (InvalidOperationException ex) when (ex.InnerException is XmlException xmlEx)
        {
            _logger.LogError(ex, "XML parsing error at line {Line}, position {Position}: {Message}", 
                xmlEx.LineNumber, xmlEx.LinePosition, xmlEx.Message);
            throw new InvalidOperationException($"Invalid XML format: {ex.InnerException.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error parsing XML: {Message}", ex.Message);
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
            // Read stream to string so we can normalize namespaces
            using var streamReader = new StreamReader(stream);
            var xmlContent = streamReader.ReadToEnd();

            return Parse(xmlContent);
        }
        catch (InvalidOperationException ex) when (ex.InnerException is XmlException)
        {
            _logger.LogError(ex, "XML parsing error");
            throw new InvalidOperationException($"Invalid XML format: {ex.InnerException.Message}", ex);
        }
    }

    /// <summary>
    /// Normalizes old 2005 namespace versions to 2011 namespace versions.
    /// This allows the same model classes to work with both old and new XML formats.
    /// </summary>
    private string NormalizeNamespaces(string xmlContent)
    {
        // Check if old namespaces are present
        if (xmlContent.Contains(OldTransactionNamespace) || xmlContent.Contains(OldSchemaLibNamespace))
        {
            _logger.LogInformation("Detected old 2005 namespace format, normalizing to 2011 format");

            // Replace old namespaces with new ones
            xmlContent = xmlContent
                .Replace(OldTransactionNamespace, AgressoNamespaces.Transaction)
                .Replace(OldSchemaLibNamespace, AgressoNamespaces.SchemaLib);
        }

        return xmlContent;
    }
}
