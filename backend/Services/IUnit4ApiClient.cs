using SfabGl07Gateway.Api.Models.Unit4;

namespace SfabGl07Gateway.Api.Services;

/// <summary>
/// Service interface for communicating with Unit4 REST API.
/// </summary>
public interface IUnit4ApiClient
{
    /// <summary>
    /// Post a transaction batch to Unit4 API.
    /// </summary>
    Task<Unit4TransactionBatchResponse> PostTransactionBatchAsync(Unit4TransactionBatchRequest request);
    
    /// <summary>
    /// Test connection to Unit4 API by validating OAuth token.
    /// </summary>
    Task<bool> TestConnectionAsync();
}
