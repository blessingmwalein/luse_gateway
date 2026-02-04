using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LuseGateway.Core.Models;

namespace LuseGateway.Core.Services
{
    /// <summary>
    /// Interface for STT Web API client
    /// </summary>
    public interface ISttApiClient
    {
        /// <summary>
        /// Authenticate with the STT API and retrieve access token
        /// </summary>
        Task<SttAuthResponse> AuthenticateAsync(string username, string password);

        /// <summary>
        /// Get order status from the exchange
        /// </summary>
        Task<SttOrderStatus?> GetOrderStatusAsync(int idOrder);

        /// <summary>
        /// Get client details by CSD account number
        /// </summary>
        Task<SttClientDetails?> GetClientDetailsAsync(string accountNumber);

        /// <summary>
        /// Get current market tickers
        /// </summary>
        Task<IEnumerable<SttInstrument>> GetTickersAsync();

        /// <summary>
        /// Get recent trades for a contract
        /// </summary>
        Task<IEnumerable<SttTrade>> GetTradesAsync(int contractId, int limit = 100);

        /// <summary>
        /// Get historical trades for a contract
        /// </summary>
        Task<IEnumerable<SttTrade>> GetTradesHistoricalAsync(int contractId, DateTime fromDate, DateTime toDate);

        /// <summary>
        /// Get all instruments/definitions
        /// </summary>
        Task<IEnumerable<SttInstrument>> GetInstrumentsAsync();

        /// <summary>
        /// Test API connectivity
        /// </summary>
        Task<bool> PingAsync();

        /// <summary>
        /// Get server time
        /// </summary>
        Task<DateTime?> GetServerTimeAsync();
    }
}
