using A1arErpSfabGl07Gateway.Api.Models.Settings;
using A1arErpSfabGl07Gateway.Api.Models.Unit4;

namespace A1arErpSfabGl07Gateway.Api.Services;

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
    /// Transform file content to Unit4 transaction batch requests.
    /// Each XML Transaction element maps to one array item.
    /// BatchInformation (Interface, BatchId) is the same on all items.
    /// </summary>
    /// <param name="fileContent">Raw file content</param>
    /// <param name="sourceSystem">Source system configuration containing Interface, TransactionType, BatchId settings</param>
    /// <returns>List of Unit4 batch requests - one per XML Transaction element</returns>
    List<Unit4TransactionBatchRequest> Transform(string fileContent, SourceSystem sourceSystem);

    /// <summary>
    /// Check if this transformer can handle the given file content.
    /// </summary>
    bool CanHandle(string fileContent);
}
