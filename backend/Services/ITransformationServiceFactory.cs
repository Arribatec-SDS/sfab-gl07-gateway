namespace A1arErpSfabGl07Gateway.Api.Services;

/// <summary>
/// Factory interface for selecting the appropriate transformer based on type.
/// </summary>
public interface ITransformationServiceFactory
{
    /// <summary>
    /// Get transformer by type identifier.
    /// </summary>
    /// <param name="transformerType">The transformer type (e.g., "ABWTransaction")</param>
    /// <returns>The transformer service</returns>
    /// <exception cref="NotSupportedException">Thrown when transformer type is not found</exception>
    ITransformationService GetTransformer(string transformerType);
    
    /// <summary>
    /// Get all registered transformer types.
    /// </summary>
    IEnumerable<string> GetAvailableTransformerTypes();
}
