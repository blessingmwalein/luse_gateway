using System.Collections.Generic;
using System.Threading.Tasks;
using LuseGateway.Core.Models;

namespace LuseGateway.Core.Services
{
    public interface IOrderService
    {
        Task<IEnumerable<PreOrderLive>> GetPendingOrdersAsync();
        Task MarkAsPostedAsync(long orderNo);
        Task UpdateOrderStatusAsync(string clOrdId, string status, string exchangeOrderId = null, string rejectionReason = null);
        Task ProcessExecutionReportAsync(string clOrdId, string execType, string ordStatus, decimal lastQty, double lastPx, decimal cumQty, string leavesQty, string exchangeOrderId);
        Task ProcessTradeCaptureReportAsync(string clOrdId, decimal lastQty, double lastPx, string side, string account, DateTime matchedDate);
        Task UpdateMarketPriceAsync(string symbol, double? bid, double? ask, double? lastPx, string securityType);
        Task UpsertSecurityDefinitionAsync(string symbol, string securityId, string securityType, string isin);
        Task<IEnumerable<CompanyPrice>> GetCompanyPricesAsync();
        Task<Dictionary<string, int>> GetOrderStatsAsync();
    }
}
