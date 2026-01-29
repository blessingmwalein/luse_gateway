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

        public OrderService(LuseDbContext dbContext, ILogger<OrderService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<IEnumerable<PreOrderLive>> GetPendingOrdersAsync()
        {
            // Orders marked as OPEN and not yet sent
            return await _dbContext.PreOrders
                .Where(o => o.OrderStatus == "OPEN")
                .ToListAsync();
        }

        public async Task MarkAsPostedAsync(decimal orderNo)
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

        public async Task ProcessExecutionReportAsync(string clOrdId, string execType, string ordStatus, decimal lastQty, decimal lastPx, decimal cumQty, decimal leavesQty, string exchangeOrderId)
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

            // Map FIX ordStatus to internal status
            // 0=New, 1=PartiallyFilled, 2=Filled, 4=Cancelled, 5=Replaced, 8=Rejected
            switch (ordStatus)
            {
                case "0":
                    order.OrderStatus = "NEW";
                    break;
                case "1":
                    order.OrderStatus = "PARTIALLY MATCHED";
                    order.LeavesQuantity = leavesQty;
                    // Logic to handle partial matching residue if necessary
                    break;
                case "2":
                    order.OrderStatus = "MATCHED ORDER";
                    order.MatchedPrice = lastPx;
                    order.MatchedDate = DateTime.Now;
                    order.LeavesQuantity = 0;
                    break;
                case "4":
                    order.OrderStatus = "CANCELLED";
                    break;
                case "8":
                    order.OrderStatus = "REJECTED";
                    break;
                default:
                    order.OrderStatus = $"STATUS_{ordStatus}";
                    break;
            }

            await _dbContext.SaveChangesAsync();
        }

        public async Task ProcessTradeCaptureReportAsync(string clOrdId, decimal lastQty, decimal lastPx, string side, string account, DateTime matchedDate)
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
                    MatchedQuantity = lastQty,
                    MatchedPrice = lastPx,
                    GrossTradeAmount = lastQty * lastPx,
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
                    order.MatchedPrice = lastPx;
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

        public async Task UpdateMarketPriceAsync(string symbol, decimal? bid, decimal? ask, decimal? lastPx, string securityType)
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
            // Implementation for ParaCompany update if needed
            // Currently using CompanyPrice as a proxy for security metadata in some cases
            await UpdateMarketPriceAsync(symbol, null, null, null, securityType);
        }
    }
}
