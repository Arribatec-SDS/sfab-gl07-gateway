using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using A1arErpSfabGl07Gateway.Api.Models.Unit4;

namespace A1arErpSfabGl07Gateway.Api.Services;

/// <summary>
/// HTTP client implementation for Unit4 REST API with OAuth2 authentication.
/// </summary>
public class Unit4ApiClient : IUnit4ApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IAppSettingsService _settingsService;
    private readonly ILogger<Unit4ApiClient> _logger;

    private string? _cachedToken;
    private DateTime _tokenExpiry = DateTime.MinValue;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    public Unit4ApiClient(
        HttpClient httpClient,
        IAppSettingsService settingsService,
        ILogger<Unit4ApiClient> logger)
    {
        _httpClient = httpClient;
        _settingsService = settingsService;
        _logger = logger;
    }

    public async Task<Unit4TransactionBatchResponse> PostTransactionBatchAsync(List<Unit4TransactionBatchRequest> requests)
    {
        var token = await GetAccessTokenAsync();
        var settings = await _settingsService.GetSettingsGroupAsync<Unit4Settings>("Unit4");

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Use BatchEndpoint if configured, otherwise fall back to default endpoint
        var batchEndpoint = !string.IsNullOrWhiteSpace(settings.BatchEndpoint)
            ? settings.BatchEndpoint
            : "/v1/financial-transaction-batch";

        var url = $"{settings.BaseUrl?.TrimEnd('/')}{batchEndpoint}";
        if (!string.IsNullOrEmpty(settings.TenantId))
        {
            url += $"?tenant={settings.TenantId}";
        }

        _logger.LogInformation("Posting transaction batch to Unit4: {Url} ({Count} rows)", url, requests.Count);
        _logger.LogDebug("Request batchId: {BatchId}, interface: {Interface}",
            requests.FirstOrDefault()?.BatchInformation?.BatchId,
            requests.FirstOrDefault()?.BatchInformation?.Interface);

        // Post the array of transactions
        var response = await _httpClient.PostAsJsonAsync(url, requests);

        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Unit4 API error: {StatusCode} - {Content}",
                response.StatusCode, responseContent);

            return new Unit4TransactionBatchResponse
            {
                Status = "Error",
                Message = $"HTTP {(int)response.StatusCode}: {responseContent}",
                Errors = new List<Unit4Error>
                {
                    new Unit4Error
                    {
                        Code = response.StatusCode.ToString(),
                        Message = responseContent
                    }
                }
            };
        }

        // Handle empty successful responses (some APIs return 200/201/204 with no body)
        if (string.IsNullOrWhiteSpace(responseContent))
        {
            _logger.LogInformation("Unit4 API returned success with empty body: {StatusCode}", response.StatusCode);
            return new Unit4TransactionBatchResponse
            {
                Status = "Success",
                Message = $"HTTP {(int)response.StatusCode}: Request accepted"
            };
        }

        try
        {
            var result = JsonSerializer.Deserialize<Unit4TransactionBatchResponse>(responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            _logger.LogInformation("Unit4 API response: {Status}", result?.Status ?? "Unknown");

            return result ?? new Unit4TransactionBatchResponse { Status = "Success", Message = "Request accepted" };
        }
        catch (JsonException ex)
        {
            // Log the actual response content for debugging
            _logger.LogWarning("Could not parse Unit4 response as JSON: {Error}. StatusCode: {StatusCode}, ContentLength: {Length}, Content: {Content}",
                ex.Message, response.StatusCode, responseContent?.Length ?? 0, responseContent);

            // If we got a success status code but can't parse the response, treat it as success
            return new Unit4TransactionBatchResponse
            {
                Status = "Success",
                Message = $"HTTP {(int)response.StatusCode}: Request accepted (response not JSON)"
            };
        }
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            var token = await GetAccessTokenAsync();
            return !string.IsNullOrEmpty(token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unit4 connection test failed");
            return false;
        }
    }

    private async Task<string> GetAccessTokenAsync()
    {
        await _tokenLock.WaitAsync();
        try
        {
            // Return cached token if still valid (with 60 second buffer)
            if (!string.IsNullOrEmpty(_cachedToken) && _tokenExpiry > DateTime.UtcNow.AddSeconds(60))
            {
                return _cachedToken;
            }

            _logger.LogDebug("Requesting new OAuth token from Unit4");

            var settings = await _settingsService.GetSettingsGroupAsync<Unit4Settings>("Unit4");

            if (string.IsNullOrEmpty(settings.TokenUrl) ||
                string.IsNullOrEmpty(settings.ClientId) ||
                string.IsNullOrEmpty(settings.ClientSecret))
            {
                throw new InvalidOperationException("Unit4 OAuth settings are not configured");
            }

            var tokenRequest = new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = settings.ClientId,
                ["client_secret"] = settings.ClientSecret,
                ["scope"] = settings.Scope ?? "api"
            };

            var tokenResponse = await _httpClient.PostAsync(
                settings.TokenUrl,
                new FormUrlEncodedContent(tokenRequest));

            if (!tokenResponse.IsSuccessStatusCode)
            {
                var error = await tokenResponse.Content.ReadAsStringAsync();
                _logger.LogError("OAuth token request failed: {StatusCode} - {Error}",
                    tokenResponse.StatusCode, error);
                throw new InvalidOperationException($"Failed to obtain OAuth token: {error}");
            }

            var tokenResult = await tokenResponse.Content.ReadFromJsonAsync<Unit4TokenResponse>();

            if (tokenResult == null || string.IsNullOrEmpty(tokenResult.AccessToken))
            {
                throw new InvalidOperationException("Invalid OAuth token response");
            }

            _cachedToken = tokenResult.AccessToken;
            _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenResult.ExpiresIn);

            _logger.LogInformation("Obtained new OAuth token, expires in {ExpiresIn} seconds",
                tokenResult.ExpiresIn);

            return _cachedToken;
        }
        finally
        {
            _tokenLock.Release();
        }
    }
}
