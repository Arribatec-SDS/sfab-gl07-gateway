namespace A1arErpSfabGl07Gateway.Api.Services;

/// <summary>
/// Factory implementation for selecting transformers by type.
/// </summary>
public class TransformationServiceFactory : ITransformationServiceFactory
{
    private readonly IEnumerable<ITransformationService> _transformers;
    private readonly ILogger<TransformationServiceFactory> _logger;

    public TransformationServiceFactory(
        IEnumerable<ITransformationService> transformers,
        ILogger<TransformationServiceFactory> logger)
    {
        _transformers = transformers;
        _logger = logger;
    }

    public ITransformationService GetTransformer(string transformerType)
    {
        var transformer = _transformers.FirstOrDefault(t => 
            t.TransformerType.Equals(transformerType, StringComparison.OrdinalIgnoreCase));

        if (transformer == null)
        {
            var available = string.Join(", ", GetAvailableTransformerTypes());
            _logger.LogError("Transformer '{TransformerType}' not found. Available: {Available}", 
                transformerType, available);
            throw new NotSupportedException(
                $"Transformer '{transformerType}' is not registered. Available transformers: {available}");
        }

        _logger.LogDebug("Selected transformer: {TransformerType}", transformerType);
        return transformer;
    }

    public IEnumerable<string> GetAvailableTransformerTypes()
    {
        return _transformers.Select(t => t.TransformerType);
    }
}
