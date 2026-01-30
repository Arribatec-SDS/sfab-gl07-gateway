using SfabGl07Gateway.Api.Models.Xml;

namespace SfabGl07Gateway.Api.Services;

/// <summary>
/// Service interface for parsing ABWTransaction XML documents.
/// </summary>
public interface IXmlParserService
{
    /// <summary>
    /// Parse XML content string into ABWTransaction object.
    /// </summary>
    ABWTransaction Parse(string xmlContent);
    
    /// <summary>
    /// Parse XML from stream into ABWTransaction object.
    /// </summary>
    ABWTransaction ParseStream(Stream stream);
}
