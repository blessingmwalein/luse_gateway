using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using LuseGateway.Core.Models;

namespace LuseGateway.Core.Services
{
    /// <summary>
    /// STT Web API client implementation with authentication and retry logic
    /// </summary>
    public class SttApiClient : ISttApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<SttApiClient> _logger;
        private readonly IConfiguration _configuration;
        private string? _accessToken;
        private DateTime _tokenExpiry;

        public SttApiClient(HttpClient httpClient, ILogger<SttApiClient> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _configuration = configuration;

            var baseUrl = _configuration["SttApi:BaseUrl"] ?? "http://10.200.2.14:5000";
            _httpClient.BaseAddress = new Uri(baseUrl);
            _httpClient.Timeout = TimeSpan.FromSeconds(
                int.Parse(_configuration["SttApi:TimeoutSeconds"] ?? "30"));
        }

        public async Task<SttAuthResponse> AuthenticateAsync(string username, string password)
        {
            try
            {
                _logger.LogInformation("Authenticating with STT API as user: {Username}", username);

                var response = await _httpClient.PostAsync(
                    $"/api/Token?username={Uri.EscapeDataString(username)}&password={Uri.EscapeDataString(password)}",
                    null);

                response.EnsureSuccessStatusCode();

                var authResponse = await response.Content.ReadFromJsonAsync<SttAuthResponse>();
                if (authResponse != null)
                {
                    _accessToken = authResponse.AccessToken;
                    // Token expires in 60 minutes, refresh at 50 minutes
                    _tokenExpiry = DateTime.UtcNow.AddMinutes(
                        int.Parse(_configuration["SttApi:TokenRefreshMinutes"] ?? "50"));
                    
                    _logger.LogInformation("Successfully authenticated with STT API");
                }

                return authResponse ?? throw new Exception("Authentication response was null");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to authenticate with STT API");
                throw;
            }
        }

        private async Task EnsureAuthenticatedAsync()
        {
            if (string.IsNullOrEmpty(_accessToken) || DateTime.UtcNow >= _tokenExpiry)
            {
                var username = _configuration["SttApi:Username"] ?? throw new Exception("STT API username not configured");
                var password = _configuration["SttApi:Password"] ?? throw new Exception("STT API password not configured");
                await AuthenticateAsync(username, password);
            }

            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
        }

        public async Task<SttOrderStatus?> GetOrderStatusAsync(int idOrder)
        {
            try
            {
                await EnsureAuthenticatedAsync();

                _logger.LogInformation("Fetching order status for IdOrder: {IdOrder}", idOrder);

                var response = await _httpClient.GetAsync($"/api/LusakaCustom/OrderStatus?idOrder={idOrder}");
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to get order status for {IdOrder}: {StatusCode}", 
                        idOrder, response.StatusCode);
                    return null;
                }

                var orderStatuses = await response.Content.ReadFromJsonAsync<List<SttOrderStatus>>();
                return orderStatuses?.Count > 0 ? orderStatuses[0] : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching order status for IdOrder: {IdOrder}", idOrder);
                return null;
            }
        }

        public async Task<SttClientDetails?> GetClientDetailsAsync(string accountNumber)
        {
            try
            {
                await EnsureAuthenticatedAsync();

                _logger.LogInformation("Fetching client details for account: {AccountNumber}", accountNumber);

                var response = await _httpClient.GetAsync(
                    $"/api/LusakaCustom/ClientLookup?accountNumber={Uri.EscapeDataString(accountNumber)}");
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to get client details for {AccountNumber}: {StatusCode}", 
                        accountNumber, response.StatusCode);
                    return null;
                }

                var clients = await response.Content.ReadFromJsonAsync<List<SttClientDetails>>();
                return clients?.Count > 0 ? clients[0] : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching client details for account: {AccountNumber}", accountNumber);
                return null;
            }
        }

        public async Task<IEnumerable<SttInstrument>> GetTickersAsync()
        {
            try
            {
                await EnsureAuthenticatedAsync();

                _logger.LogInformation("Fetching market tickers from STT API");

                var response = await _httpClient.GetAsync("/api/LusakaCustom/Tickers");
                response.EnsureSuccessStatusCode();

                var tickers = await response.Content.ReadFromJsonAsync<List<SttInstrument>>();
                return tickers ?? new List<SttInstrument>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching tickers from STT API");
                return new List<SttInstrument>();
            }
        }

        public async Task<IEnumerable<SttTrade>> GetTradesAsync(int contractId, int limit = 100)
        {
            try
            {
                await EnsureAuthenticatedAsync();

                _logger.LogInformation("Fetching trades for contract {ContractId}, limit: {Limit}", 
                    contractId, limit);

                var response = await _httpClient.GetAsync(
                    $"/api/MarketData/Trades?contractId={contractId}&limit={limit}");
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to get trades for contract {ContractId}: {StatusCode}", 
                        contractId, response.StatusCode);
                    return new List<SttTrade>();
                }

                var trades = await response.Content.ReadFromJsonAsync<List<SttTrade>>();
                return trades ?? new List<SttTrade>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching trades for contract {ContractId}", contractId);
                return new List<SttTrade>();
            }
        }

        public async Task<IEnumerable<SttTrade>> GetTradesHistoricalAsync(int contractId, DateTime fromDate, DateTime toDate)
        {
            try
            {
                await EnsureAuthenticatedAsync();

                _logger.LogInformation("Fetching historical trades for contract {ContractId} from {FromDate} to {ToDate}", 
                    contractId, fromDate, toDate);

                var fromDateStr = fromDate.ToString("yyyy-MM-dd");
                var toDateStr = toDate.ToString("yyyy-MM-dd");

                var response = await _httpClient.GetAsync(
                    $"/api/MarketData/TradesHistorical?contractId={contractId}&fromDate={fromDateStr}&toDate={toDateStr}");
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to get historical trades for contract {ContractId}: {StatusCode}", 
                        contractId, response.StatusCode);
                    return new List<SttTrade>();
                }

                var trades = await response.Content.ReadFromJsonAsync<List<SttTrade>>();
                return trades ?? new List<SttTrade>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching historical trades for contract {ContractId}", contractId);
                return new List<SttTrade>();
            }
        }

        public async Task<IEnumerable<SttInstrument>> GetInstrumentsAsync()
        {
            try
            {
                await EnsureAuthenticatedAsync();

                _logger.LogInformation("Fetching instruments from STT API");

                var response = await _httpClient.GetAsync("/api/Definitions/Instruments");
                response.EnsureSuccessStatusCode();

                var instruments = await response.Content.ReadFromJsonAsync<List<SttInstrument>>();
                return instruments ?? new List<SttInstrument>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching instruments from STT API");
                return new List<SttInstrument>();
            }
        }

        public async Task<bool> PingAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/Ping");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ping to STT API failed");
                return false;
            }
        }

        public async Task<DateTime?> GetServerTimeAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/Time");
                if (!response.IsSuccessStatusCode) return null;

                var timeStr = await response.Content.ReadAsStringAsync();
                if (DateTime.TryParse(timeStr.Trim('"'), out var serverTime))
                {
                    return serverTime;
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get server time from STT API");
                return null;
            }
        }
    }
}
