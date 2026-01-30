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
    public class OrderReconciliationService : IOrderReconciliationService
    {
        private readonly LuseDbContext _dbContext;
        private readonly ILogger<OrderReconciliationService> _logger;

        public OrderReconciliationService(LuseDbContext dbContext, ILogger<OrderReconciliationService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task ReconcileOrdersAsync()
        {
            _logger.LogInformation("Starting order reconciliation...");
            
            // Reconcile PreOrders with LiveOrders
            var preOrders = await _dbContext.PreOrders.ToListAsync();
            var liveOrders = await _dbContext.LiveOrders.ToListAsync();

            foreach (var preOrder in preOrders)
            {
                var relatedLiveOrders = liveOrders.Where(l => l.OrderIdentifier == preOrder.OrderNumber).ToList();
                
                if (relatedLiveOrders.Any())
                {
                    var latestLive = relatedLiveOrders.OrderByDescending(l => l.CreateDate).First();
                    if (preOrder.OrderStatus != latestLive.OrderStatus)
                    {
                        _logger.LogWarning("Discrepancy found for Order {OrderNumber}: PreOrder Status={PreStatus}, LiveOrder Status={LiveStatus}. Syncing...", 
                            preOrder.OrderNumber, preOrder.OrderStatus, latestLive.OrderStatus);
                        
                        preOrder.OrderStatus = latestLive.OrderStatus;
                        preOrder.ExchangeOrderNumber = latestLive.ExchangeOrderNumber;
                    }
                }
            }

            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Order reconciliation completed.");
        }

        public async Task<IEnumerable<OrderHistoryEntry>> GetOrderHistoryAsync(string clOrdId)
        {
            var history = new List<OrderHistoryEntry>();

            // Get from LiveOrders
            var liveHistory = await _dbContext.LiveOrders
                .Where(l => l.OrderIdentifier == clOrdId)
                .OrderBy(l => l.CreateDate)
                .Select(l => new OrderHistoryEntry
                {
                    Timestamp = l.CreateDate ?? DateTime.Now,
                    Status = l.OrderStatus,
                    Message = "Live Order Log",
                    ExecutionType = "Sync",
                    Quantity = l.Quantity,
                    Price = l.BasePrice
                })
                .ToListAsync();

            history.AddRange(liveHistory);

            // Get from Trades
            var trades = await _dbContext.Trades
                .Where(t => t.OrderNumber == clOrdId)
                .OrderBy(t => t.MatchedDate)
                .ToListAsync();

            var tradeHistory = trades.Select(t => new OrderHistoryEntry
                {
                    Timestamp = t.MatchedDate,
                    Status = "MATCHED",
                    Message = "Trade Capture",
                    ExecutionType = "Trade",
                    Quantity = decimal.TryParse(t.MatchedQuantity, out var q) ? q : 0,
                    Price = double.TryParse(t.MatchedPrice, out var p) ? p : 0
                });

            history.AddRange(tradeHistory);

            return history.OrderBy(h => h.Timestamp);
        }
    }
}
