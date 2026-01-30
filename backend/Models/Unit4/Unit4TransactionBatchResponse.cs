using System.Text.Json.Serialization;

namespace SfabGl07Gateway.Api.Models.Unit4;

/// <summary>
/// Response from Unit4 API for transaction batch posting.
/// </summary>
public class Unit4TransactionBatchResponse
{
    [JsonPropertyName("batchId")]
    public string? BatchId { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("errors")]
    public List<Unit4Error>? Errors { get; set; }

    [JsonPropertyName("warnings")]
    public List<Unit4Warning>? Warnings { get; set; }

    [JsonPropertyName("transactionResults")]
    public List<TransactionResult>? TransactionResults { get; set; }
}

/// <summary>
/// Error details from Unit4 API.
/// </summary>
public class Unit4Error
{
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("field")]
    public string? Field { get; set; }
}

/// <summary>
/// Warning details from Unit4 API.
/// </summary>
public class Unit4Warning
{
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

/// <summary>
/// Result for individual transaction in batch.
/// </summary>
public class TransactionResult
{
    [JsonPropertyName("voucherNumber")]
    public string? VoucherNumber { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

/// <summary>
/// OAuth2 token response from Unit4 auth server.
/// </summary>
public class Unit4TokenResponse
{
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("token_type")]
    public string? TokenType { get; set; }

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("scope")]
    public string? Scope { get; set; }
}
