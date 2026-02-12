using A1arErpSfabGl07Gateway.Api.Models.Unit4;

namespace A1arErpSfabGl07Gateway.Api.Services;

/// <summary>
/// Service interface for communicating with Unit4 REST API.
/// </summary>
public interface IUnit4ApiClient
{
    /// <summary>
    /// Post a transaction batch to Unit4 API.
    /// The API accepts an array of transaction batch requests.
    /// </summary>
    Task<Unit4TransactionBatchResponse> PostTransactionBatchAsync(List<Unit4TransactionBatchRequest> requests);

    /// <summary>
    /// Order a report job in Unit4.
    /// Called after successful batch submission to trigger GL07 report generation.
    /// </summary>
    Task<Unit4ReportJobResponse> OrderReportJobAsync(Unit4ReportJobRequest request);

    /// <summary>
    /// Test connection to Unit4 API by validating OAuth token.
    /// </summary>
    Task<bool> TestConnectionAsync();
}
