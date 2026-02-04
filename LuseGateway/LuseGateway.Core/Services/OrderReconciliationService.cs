using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private readonly ISttApiClient _sttApiClient;
        private readonly ILogger<OrderReconciliationService> _logger;

        public OrderReconciliationService(
            LuseDbContext dbContext, 
            ISttApiClient sttApiClient,
            ILogger<OrderReconciliationService> logger)
        {
            _dbContext = dbContext;
            _sttApiClient = sttApiClient;
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

        public async Task<ReconciliationReport> ReconcileOrdersWithSttApiAsync()
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Starting three-way reconciliation with STT API...");

            var report = new ReconciliationReport
            {
                ReconciledAt = DateTime.UtcNow,
                Discrepancies = new List<OrderDiscrepancy>(),
                AutoCorrectedOrders = new List<string>(),
                ManualReviewRequired = new List<string>()
            };

            try
            {
                // Get all active orders from database
                var preOrders = await _dbContext.PreOrders
                    .Where(p => p.OrderStatus != "FILLED" && p.OrderStatus != "CANCELLED" && p.OrderStatus != "REJECTED")
                    .ToListAsync();

                report.TotalOrders = preOrders.Count;

                foreach (var preOrder in preOrders)
                {
                    try
                    {
                        // Get exchange order ID from ExchangeOrderNumber
                        if (string.IsNullOrEmpty(preOrder.ExchangeOrderNumber) || 
                            !int.TryParse(preOrder.ExchangeOrderNumber, out var exchangeOrderId))
                        {
                            _logger.LogWarning("Order {ClOrdId} has no valid exchange order ID", preOrder.OrderNumber);
                            continue;
                        }

                        // Query STT API for order status
                        var exchangeStatus = await _sttApiClient.GetOrderStatusAsync(exchangeOrderId);

                        if (exchangeStatus == null)
                        {
                            _logger.LogWarning("Could not fetch exchange status for order {ClOrdId}", preOrder.OrderNumber);
                            report.Discrepancies.Add(new OrderDiscrepancy
                            {
                                ClOrdId = preOrder.OrderNumber ?? "",
                                DiscrepancyType = "MISSING_ON_EXCHANGE",
                                LocalStatus = preOrder.OrderStatus ?? "",
                                ExchangeStatus = "NOT_FOUND",
                                Details = "Order not found on exchange",
                                Severity = "CRITICAL"
                            });
                            report.ManualReviewRequired.Add(preOrder.OrderNumber ?? "");
                            continue;
                        }

                        // Compare statuses
                        var statusMatch = CompareOrderStatus(preOrder.OrderStatus, exchangeStatus.Status);
                        var qtyMatch = preOrder.Quantity == exchangeStatus.OriginalQty;

                        if (!statusMatch || !qtyMatch)
                        {
                            var discrepancy = new OrderDiscrepancy
                            {
                                ClOrdId = preOrder.OrderNumber ?? "",
                                LocalStatus = preOrder.OrderStatus ?? "",
                                ExchangeStatus = exchangeStatus.Status,
                                LocalQty = (long)preOrder.Quantity,
                                ExchangeQty = exchangeStatus.OriginalQty,
                                LocalPrice = (decimal)preOrder.BasePrice,
                                ExchangePrice = exchangeStatus.OrderPx
                            };

                            if (!statusMatch)
                            {
                                discrepancy.DiscrepancyType = "STATUS_MISMATCH";
                                discrepancy.Details = $"Local: {preOrder.OrderStatus}, Exchange: {exchangeStatus.Status}";
                                discrepancy.Severity = "WARNING";

                                // Auto-correct status if exchange is more recent
                                if (ShouldAutoCorrectStatus(preOrder.OrderStatus, exchangeStatus.Status))
                                {
                                    preOrder.OrderStatus = MapExchangeStatus(exchangeStatus.Status);
                                    report.AutoCorrectedOrders.Add(preOrder.OrderNumber ?? "");
                                    _logger.LogInformation("Auto-corrected status for order {ClOrdId} to {NewStatus}", 
                                        preOrder.OrderNumber, preOrder.OrderStatus);
                                }
                                else
                                {
                                    report.ManualReviewRequired.Add(preOrder.OrderNumber ?? "");
                                }
                            }

                            if (!qtyMatch)
                            {
                                discrepancy.DiscrepancyType = "QTY_MISMATCH";
                                discrepancy.Details = $"Local: {preOrder.Quantity}, Exchange: {exchangeStatus.OriginalQty}";
                                discrepancy.Severity = "CRITICAL";
                                report.ManualReviewRequired.Add(preOrder.OrderNumber ?? "");
                            }

                            report.Discrepancies.Add(discrepancy);
                        }
                        else
                        {
                            report.MatchedOrders++;
                        }

                        // Check for missing executions
                        if (exchangeStatus.Trades != null && exchangeStatus.Trades.Any())
                        {
                            var localTrades = await _dbContext.Trades
                                .Where(t => t.OrderNumber == preOrder.OrderNumber)
                                .ToListAsync();

                            if (exchangeStatus.Trades.Count != localTrades.Count)
                            {
                                report.Discrepancies.Add(new OrderDiscrepancy
                                {
                                    ClOrdId = preOrder.OrderNumber ?? "",
                                    DiscrepancyType = "MISSING_EXECUTION",
                                    LocalStatus = preOrder.OrderStatus ?? "",
                                    ExchangeStatus = exchangeStatus.Status,
                                    Details = $"Exchange has {exchangeStatus.Trades.Count} trades, local has {localTrades.Count}",
                                    Severity = "WARNING"
                                });
                                report.ManualReviewRequired.Add(preOrder.OrderNumber ?? "");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error reconciling order {ClOrdId}", preOrder.OrderNumber);
                    }
                }

                // Save auto-corrections
                if (report.AutoCorrectedOrders.Any())
                {
                    await _dbContext.SaveChangesAsync();
                }

                report.DiscrepanciesFound = report.Discrepancies.Count;
                stopwatch.Stop();
                report.Duration = stopwatch.Elapsed;

                _logger.LogInformation("Reconciliation completed. Total: {Total}, Matched: {Matched}, Discrepancies: {Discrepancies}, Auto-corrected: {AutoCorrected}",
                    report.TotalOrders, report.MatchedOrders, report.DiscrepanciesFound, report.AutoCorrectedOrders.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during reconciliation");
                throw;
            }

            return report;
        }

        public async Task<ReconciliationReport> ReconcileSpecificOrderAsync(string clOrdId)
        {
            _logger.LogInformation("Reconciling specific order: {ClOrdId}", clOrdId);

            var report = new ReconciliationReport
            {
                ReconciledAt = DateTime.UtcNow,
                TotalOrders = 1,
                Discrepancies = new List<OrderDiscrepancy>()
            };

            var preOrder = await _dbContext.PreOrders.FirstOrDefaultAsync(p => p.OrderNumber == clOrdId);
            if (preOrder == null)
            {
                _logger.LogWarning("Order {ClOrdId} not found in database", clOrdId);
                return report;
            }

            if (string.IsNullOrEmpty(preOrder.ExchangeOrderNumber) || 
                !int.TryParse(preOrder.ExchangeOrderNumber, out var exchangeOrderId))
            {
                report.Discrepancies.Add(new OrderDiscrepancy
                {
                    ClOrdId = clOrdId,
                    DiscrepancyType = "NO_EXCHANGE_ID",
                    Details = "Order has no exchange order ID",
                    Severity = "CRITICAL"
                });
                return report;
            }

            var exchangeStatus = await _sttApiClient.GetOrderStatusAsync(exchangeOrderId);
            if (exchangeStatus == null)
            {
                report.Discrepancies.Add(new OrderDiscrepancy
                {
                    ClOrdId = clOrdId,
                    DiscrepancyType = "NOT_FOUND_ON_EXCHANGE",
                    Details = "Order not found on exchange",
                    Severity = "CRITICAL"
                });
                return report;
            }

            // Compare and report
            if (preOrder.OrderStatus != exchangeStatus.Status)
            {
                report.Discrepancies.Add(new OrderDiscrepancy
                {
                    ClOrdId = clOrdId,
                    DiscrepancyType = "STATUS_MISMATCH",
                    LocalStatus = preOrder.OrderStatus ?? "",
                    ExchangeStatus = exchangeStatus.Status,
                    Details = $"Local: {preOrder.OrderStatus}, Exchange: {exchangeStatus.Status}",
                    Severity = "WARNING"
                });
            }
            else
            {
                report.MatchedOrders = 1;
            }

            report.DiscrepanciesFound = report.Discrepancies.Count;
            return report;
        }

        public async Task<OrderValidationResult> ValidateOrderWithExchangeAsync(string clOrdId)
        {
            var result = new OrderValidationResult
            {
                ClOrdId = clOrdId,
                ValidationMessages = new List<string>()
            };

            var preOrder = await _dbContext.PreOrders.FirstOrDefaultAsync(p => p.OrderNumber == clOrdId);
            if (preOrder == null)
            {
                result.IsValid = false;
                result.ValidationMessages.Add("Order not found in local database");
                return result;
            }

            if (string.IsNullOrEmpty(preOrder.ExchangeOrderNumber) || 
                !int.TryParse(preOrder.ExchangeOrderNumber, out var exchangeOrderId))
            {
                result.IsValid = false;
                result.ValidationMessages.Add("Order has no exchange order ID");
                return result;
            }

            var exchangeStatus = await _sttApiClient.GetOrderStatusAsync(exchangeOrderId);
            if (exchangeStatus == null)
            {
                result.IsValid = false;
                result.ExistsOnExchange = false;
                result.ValidationMessages.Add("Order not found on exchange");
                return result;
            }

            result.ExistsOnExchange = true;
            result.ExchangeData = exchangeStatus;
            result.StatusMatches = CompareOrderStatus(preOrder.OrderStatus, exchangeStatus.Status);
            result.QuantityMatches = preOrder.Quantity == exchangeStatus.OriginalQty;

            if (!result.StatusMatches)
            {
                result.ValidationMessages.Add($"Status mismatch: Local={preOrder.OrderStatus}, Exchange={exchangeStatus.Status}");
            }

            if (!result.QuantityMatches)
            {
                result.ValidationMessages.Add($"Quantity mismatch: Local={preOrder.Quantity}, Exchange={exchangeStatus.OriginalQty}");
            }

            result.IsValid = result.StatusMatches && result.QuantityMatches;
            return result;
        }

        public async Task SyncMissingOrdersAsync()
        {
            _logger.LogInformation("Syncing missing orders from exchange...");
            // TODO: Implement logic to fetch all orders from exchange and identify missing ones
            await Task.CompletedTask;
        }

        public async Task SyncInstrumentMasterDataAsync()
        {
            _logger.LogInformation("Syncing instrument master data from STT API...");
            
            try
            {
                var instruments = await _sttApiClient.GetInstrumentsAsync();
                _logger.LogInformation("Fetched {Count} instruments from STT API", instruments.Count());
                
                // TODO: Sync with para_company table
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing instrument master data");
            }
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
                    Status = l.OrderStatus ?? "",
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

        // Helper methods
        private bool CompareOrderStatus(string? localStatus, string exchangeStatus)
        {
            var mappedExchangeStatus = MapExchangeStatus(exchangeStatus);
            return localStatus == mappedExchangeStatus;
        }

        private string MapExchangeStatus(string exchangeStatus)
        {
            // Map STT API status to local status
            return exchangeStatus?.ToUpper() switch
            {
                "NEW" => "NEW",
                "PARTIALLY_FILLED" => "PARTIAL",
                "FILLED" => "FILLED",
                "CANCELED" => "CANCELLED",
                "REJECTED" => "REJECTED",
                _ => exchangeStatus ?? "UNKNOWN"
            };
        }

        private bool ShouldAutoCorrectStatus(string? localStatus, string exchangeStatus)
        {
            // Only auto-correct if exchange status is more "final"
            var finalStatuses = new[] { "FILLED", "CANCELED", "REJECTED" };
            return finalStatuses.Contains(exchangeStatus.ToUpper());
        }
    }
}
