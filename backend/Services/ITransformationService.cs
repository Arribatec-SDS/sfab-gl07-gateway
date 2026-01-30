using SfabGl07Gateway.Api.Models.Unit4;

namespace SfabGl07Gateway.Api.Services;

/// <summary>
/// Base interface for file content transformers.
/// Uses Strategy pattern to support different file formats.
/// </summary>
public interface ITransformationService
{
    /// <summary>
    /// Transformer type identifier (matches SourceSystem.TransformerType).
    /// </summary>
    string TransformerType { get; }
    
    /// <summary>
    /// Transform file content to Unit4 transaction batch request.
    /// </summary>
    /// <param name="fileContent">Raw file content</param>
    /// <returns>Transformed Unit4 batch request</returns>
    Unit4TransactionBatchRequest Transform(string fileContent);
    
    /// <summary>
    /// Check if this transformer can handle the given file content.
    /// </summary>
    bool CanHandle(string fileContent);
}
