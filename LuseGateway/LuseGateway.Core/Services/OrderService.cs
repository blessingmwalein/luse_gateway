using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LuseGateway.Core.Data;
using LuseGateway.Core.Models;

namespace LuseGateway.Core.Services
{
    public class OrderService : IOrderService
    {
        private readonly LuseDbContext _dbContext;
        private readonly ILogger<OrderService> _logger;
        private readonly IBillingService _billingService;

        public OrderService(LuseDbContext dbContext, ILogger<OrderService> logger, IBillingService billingService)
        {
            _dbContext = dbContext;
            _logger = logger;
            _billingService = billingService;
        }

        public async Task<IEnumerable<PreOrderLive>> GetOrdersAsync(int page = 1, int pageSize = 10, DateTime? fromDate = null, string[]? statuses = null)
        {
            var query = _dbContext.PreOrders.AsQueryable();

            if (fromDate.HasValue)
                query = query.Where(o => o.CreateDate >= fromDate.Value);

            if (statuses != null && statuses.Length > 0)
                query = query.Where(o => statuses.Contains(o.OrderStatus));

            return await query
                .OrderByDescending(o => o.OrderNo)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetOrderCountAsync(DateTime? fromDate = null, string[]? statuses = null)
        {
            var query = _dbContext.PreOrders.AsQueryable();

            if (fromDate.HasValue)
                query = query.Where(o => o.CreateDate >= fromDate.Value);

            if (statuses != null && statuses.Length > 0)
                query = query.Where(o => statuses.Contains(o.OrderStatus));

            return await query.CountAsync();
        }

        public async Task<IEnumerable<PreOrderLive>> GetPendingOrdersAsync()
        {
            // Orders marked as OPEN and not yet sent
            return await _dbContext.PreOrders
                .Where(o => o.OrderStatus == "OPEN")
                .ToListAsync();
        }

        public async Task MarkAsPostedAsync(long orderNo)
        {
            var order = await _dbContext.PreOrders.FindAsync(orderNo);
            if (order != null)
            {
                order.OrderStatus = "POSTED";
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task UpdateOrderStatusAsync(string clOrdId, string status, string exchangeOrderId = null, string rejectionReason = null)
        {
            var order = await _dbContext.PreOrders
                .OrderByDescending(o => o.OrderNo)
                .FirstOrDefaultAsync(o => o.OrderNumber == clOrdId || o.ExchangeOrderNumber == clOrdId);

            if (order != null)
            {
                order.OrderStatus = status;
                if (exchangeOrderId != null) order.ExchangeOrderNumber = exchangeOrderId;
                if (rejectionReason != null) order.RejectionReason = rejectionReason;
                
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task ProcessExecutionReportAsync(string clOrdId, string execType, string ordStatus, decimal lastQty, double lastPx, decimal cumQty, string leavesQty, string exchangeOrderId)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var order = await _dbContext.PreOrders
                    .OrderByDescending(o => o.OrderNo)
                    .FirstOrDefaultAsync(o => o.OrderNumber == clOrdId || o.ExchangeOrderNumber == clOrdId);

                if (order == null)
                {
                    _logger.LogWarning("Order {ClOrdId} not found for ExecutionReport", clOrdId);
                    return;
                }

                order.ExchangeOrderNumber = exchangeOrderId;
                string internalStatus = MapOrdStatus(ordStatus);
                order.OrderStatus = internalStatus;

                if (ordStatus == "1") order.LeavesQuantity = leavesQty;
                if (ordStatus == "2")
                {
                    order.MatchedPrice = (decimal)lastPx;
                    order.MatchedDate = DateTime.Now;
                    order.LeavesQuantity = "0";
                }

                // 2. Sync to Live_Orders (Legacy requirement)
                await SyncToLiveOrdersAsync(order, internalStatus, lastQty);

                // 3. Handle Special Logic (Refunds for Rejection/Cancellation)
                if (ordStatus == "8" || ordStatus == "4") // Rejected or Cancelled
                {
                    await HandleOrderRefundAsync(order);
                }

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error processing ExecutionReport for {ClOrdId}", clOrdId);
                throw;
            }
        }

        private string MapOrdStatus(string fixStatus)
        {
            return fixStatus switch
            {
                "0" => "NEW",
                "1" => "PARTIALLY MATCHED",
                "2" => "MATCHED ORDER",
                "4" => "CANCELLED",
                "5" => "REPLACED",
                "8" => "REJECTED",
                _ => $"STATUS_{fixStatus}"
            };
        }

        private async Task SyncToLiveOrdersAsync(PreOrderLive order, string status, decimal qty)
        {
            var liveOrder = new LiveOrder
            {
                Company = order.Symbol,
                SecurityType = order.SecurityType,
                CreateDate = DateTime.Now,
                OrderStatus = status,
                Quantity = qty > 0 ? (int)qty : order.Quantity,
                BasePrice = order.BasePrice,
                TimeInForce = order.TimeInForce,
                MaturityDate = order.ExpiryDate,
                Side = order.Side,
                OrderIdentifier = order.OrderNumber,
                CdsAccount = order.CdsAccount,
                BrokerCode = order.BrokerCode,
                Trader = order.Trader,
                OrderAttribute = order.OrderAttribute,
                ExchangeOrderNumber = order.ExchangeOrderNumber,
                BrokerRef = order.BrokerRef
            };
            _dbContext.LiveOrders.Add(liveOrder);
        }

        private async Task HandleOrderRefundAsync(PreOrderLive order)
        {
            if (order.Side == "BUY")
            {
                // Legacy calculation: qnt * pric * 1.015 (1.5% fee refund)
                decimal chargeRate = await _billingService.CalculateLuseChargesAsync((decimal)(order.Quantity * order.BasePrice));
                decimal refundAmount = (decimal)(order.Quantity * order.BasePrice) + chargeRate;

                await _billingService.ProcessRefundAsync(order.CdsAccount, order.BrokerRef, refundAmount);
                _logger.LogInformation("Processed refund for order {OrderNo}", order.OrderNo);
            }
        }

        public async Task ProcessTradeCaptureReportAsync(string clOrdId, decimal lastQty, double lastPx, string side, string account, DateTime matchedDate)
        {
            var order = await _dbContext.PreOrders
                .OrderByDescending(o => o.OrderNo)
                .FirstOrDefaultAsync(o => o.OrderNumber == clOrdId);

            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                // 1. Create Trade record
                var trade = new Trade
                {
                    SecurityId = order?.Company,
                    MatchedQuantity = lastQty.ToString(),
                    MatchedPrice = lastPx.ToString(),
                    GrossTradeAmount = (lastQty * (decimal)lastPx).ToString(),
                    MatchedDate = matchedDate,
                    OrderNumber = clOrdId,
                    TradingAccount = account,
                    Side = side
                };
                _dbContext.Trades.Add(trade);

                // 2. Update Order Status if not already filled
                if (order != null && order.OrderStatus != "MATCHED ORDER")
                {
                    order.OrderStatus = "MATCHED ORDER";
                    order.MatchedPrice = (decimal)lastPx;
                    order.MatchedDate = matchedDate;
                }

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error processing TradeCaptureReport for {ClOrdId}", clOrdId);
                throw;
            }
        }

        public async Task UpdateMarketPriceAsync(string symbol, double? bid, double? ask, double? lastPx, string securityType)
        {
            var price = await _dbContext.CompanyPrices.FindAsync(symbol);
            if (price == null)
            {
                price = new CompanyPrice { Company = symbol, SecurityType = securityType };
                _dbContext.CompanyPrices.Add(price);
            }

            if (bid.HasValue) price.BestBid = bid.Value;
            if (ask.HasValue) price.BestAsk = ask.Value;
            if (lastPx.HasValue) price.VwapPrice = lastPx.Value;

            await _dbContext.SaveChangesAsync();
        }

        public async Task UpsertSecurityDefinitionAsync(string symbol, string securityId, string securityType, string isin)
        {
            var paraCompany = await _dbContext.ParaCompanies.FindAsync(symbol);
            if (paraCompany == null)
            {
                paraCompany = new ParaCompany { Symbol = symbol };
                _dbContext.ParaCompanies.Add(paraCompany);
            }

            paraCompany.Company = securityId;
            paraCompany.Fnam = symbol;
            paraCompany.Exchange = "LUSE";
            paraCompany.SecurityType = securityType;
            paraCompany.DateCreated = DateTime.Now;
            paraCompany.IsinNo = isin;

            await _dbContext.SaveChangesAsync();
        }

        public async Task<IEnumerable<CompanyPrice>> GetCompanyPricesAsync()
        {
            return await _dbContext.CompanyPrices.ToListAsync();
        }

        public async Task<Dictionary<string, int>> GetOrderStatsAsync()
        {
            var stats = await _dbContext.PreOrders
                .GroupBy(o => o.OrderStatus)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            return stats.ToDictionary(x => x.Status ?? "UNKNOWN", x => x.Count);
        }
    }
}
