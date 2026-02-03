using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using SfabGl07Gateway.Api.Models.Unit4;

namespace SfabGl07Gateway.Api.Services;

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

    public async Task<Unit4TransactionBatchResponse> PostTransactionBatchAsync(Unit4TransactionBatchRequest request)
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

        _logger.LogInformation("Posting transaction batch to Unit4: {Url}", url);
        _logger.LogDebug("Request batchId: {BatchId}, interface: {Interface}",
            request.BatchInformation?.BatchId, request.BatchInformation?.Interface);

        var response = await _httpClient.PostAsJsonAsync(url, request);

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

        var result = JsonSerializer.Deserialize<Unit4TransactionBatchResponse>(responseContent,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        _logger.LogInformation("Unit4 API response: {Status}", result?.Status ?? "Unknown");

        return result ?? new Unit4TransactionBatchResponse { Status = "Unknown", Message = "Empty response" };
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

            if (string.IsNullOrEmpty(settings.TokenEndpoint) ||
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
                settings.TokenEndpoint,
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
