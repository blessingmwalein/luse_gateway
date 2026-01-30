using System.Collections.Generic;
using System.Threading.Tasks;
using LuseGateway.Core.Models;

namespace LuseGateway.Core.Services
{
    public interface IOrderReconciliationService
    {
        Task ReconcileOrdersAsync();
        Task<IEnumerable<OrderHistoryEntry>> GetOrderHistoryAsync(string clOrdId);
    }

    public class OrderHistoryEntry
    {
        public System.DateTime Timestamp { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
        public string ExecutionType { get; set; }
        public decimal Quantity { get; set; }
        public double Price { get; set; }
    }
}
