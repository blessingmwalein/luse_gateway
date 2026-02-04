using System.Collections.Generic;
using System.Threading.Tasks;
using LuseGateway.Core.Models;

namespace LuseGateway.Core.Services
{
    public interface IOrderReconciliationService
    {
        // Existing methods
        Task ReconcileOrdersAsync();
        Task<IEnumerable<OrderHistoryEntry>> GetOrderHistoryAsync(string clOrdId);

        // New STT API-based reconciliation methods
        Task<ReconciliationReport> ReconcileOrdersWithSttApiAsync();
        Task<ReconciliationReport> ReconcileSpecificOrderAsync(string clOrdId);
        Task<OrderValidationResult> ValidateOrderWithExchangeAsync(string clOrdId);
        Task SyncMissingOrdersAsync();
        Task SyncInstrumentMasterDataAsync();
    }

    public class OrderHistoryEntry
    {
        public System.DateTime Timestamp { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string ExecutionType { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public double Price { get; set; }
    }
}
