using QuickFix;
using QuickFix.FIX50SP2;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using LuseGateway.Core.Services;
using LuseGateway.Core.Models;

namespace LuseGateway.Fix
{
    public class LuseMessageCracker : QuickFix.MessageCracker
    {
        private readonly ILogger<LuseMessageCracker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public LuseMessageCracker(ILogger<LuseMessageCracker> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public void OnMessage(ExecutionReport message, SessionID sessionID)
        {
            _logger.LogInformation("Received ExecutionReport: {ClOrdID}, Status: {OrdStatus}", 
                message.IsSetClOrdID() ? message.ClOrdID.getValue() : "N/A", 
                message.OrdStatus.getValue());

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();

                string clOrdId = message.IsSetClOrdID() ? message.ClOrdID.Value : "";
                string execType = message.ExecType.Value.ToString();
                string ordStatus = message.OrdStatus.Value.ToString();
                decimal lastQty = message.IsSetLastQty() ? message.LastQty.Value : 0;
                double lastPx = message.IsSetLastPx() ? (double)message.LastPx.Value : 0;
                decimal cumQty = message.IsSetCumQty() ? message.CumQty.Value : 0;
                string leavesQty = message.IsSetLeavesQty() ? message.LeavesQty.Value.ToString() : "0";
                string exchangeOrderId = message.IsSetOrderID() ? message.OrderID.Value : "";

                orderService.ProcessExecutionReportAsync(clOrdId, execType, ordStatus, lastQty, lastPx, cumQty, leavesQty, exchangeOrderId).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing ExecutionReport");
            }
        }

        public void OnMessage(TradeCaptureReport message, SessionID sessionID)
        {
            _logger.LogInformation("Received TradeCaptureReport");
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();

                var group = new TradeCaptureReport.NoSidesGroup();
                message.GetGroup(1, group);
                
                string clOrdId = group.IsSetClOrdID() ? group.ClOrdID.Value : "";
                decimal lastQty = message.LastQty.Value;
                double lastPx = (double)message.LastPx.Value;
                string side = group.Side.Value == QuickFix.Fields.Side.BUY ? "BUY" : "SELL";
                string account = group.Account.Value;
                DateTime matchedDate = DateTime.ParseExact(message.TradeDate.Value, "yyyyMMdd", null);

                orderService.ProcessTradeCaptureReportAsync(clOrdId, lastQty, lastPx, side, account, matchedDate).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing TradeCaptureReport");
            }
        }

        public void OnMessage(MarketDataSnapshotFullRefresh message, SessionID sessionID)
        {
            _logger.LogInformation("Received MarketDataSnapshotFullRefresh for {Symbol}", message.Symbol.getValue());
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();

                string symbol = message.Symbol.Value;
                string securityType = message.IsSetSecurityType() ? message.SecurityType.Value : "";
                
                double? bid = null;
                double? ask = null;
                double? vwap = null;

                var entryGroup = new MarketDataSnapshotFullRefresh.NoMDEntriesGroup();
                for (int i = 1; i <= message.NoMDEntries.Value; i++)
                {
                    message.GetGroup(i, entryGroup);
                    char type = entryGroup.MDEntryType.Value;
                    double px = (double)entryGroup.MDEntryPx.Value;

                    if (type == QuickFix.Fields.MDEntryType.BID) bid = px;
                    else if (type == QuickFix.Fields.MDEntryType.OFFER) ask = px;
                    else if (type == QuickFix.Fields.MDEntryType.TRADING_SESSION_VWAP_PRICE) vwap = px;
                }

                orderService.UpdateMarketPriceAsync(symbol, bid, ask, vwap, securityType).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing MarketDataSnapshotFullRefresh");
            }
        }

        public void OnMessage(SecurityDefinition message, SessionID sessionID)
        {
            _logger.LogInformation("Received SecurityDefinition for {Symbol}", message.Symbol.getValue());
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();

                string symbol = message.Symbol.Value;
                string securityId = message.SecurityID.Value;
                string securityType = message.SecurityType.Value;
                
                string isin = "";
                if (message.IsSetNoSecurityAltID())
                {
                    var altIdGroup = new SecurityDefinition.NoSecurityAltIDGroup();
                    message.GetGroup(1, altIdGroup);
                    isin = altIdGroup.SecurityAltID.Value;
                }

                orderService.UpsertSecurityDefinitionAsync(symbol, securityId, securityType, isin).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing SecurityDefinition");
            }
        }
        public void OnMessage(QuickFix.FIXT11.Reject message, SessionID sessionID)
        {
            _logger.LogWarning("FIX Reject received: {Text} (RefSeqNum: {RefSeqNum}, RefTagID: {RefTagID})", 
                message.IsSetText() ? message.Text.Value : "No text", 
                message.IsSetRefSeqNum() ? message.RefSeqNum.Value.ToString() : "N/A",
                message.IsSetRefTagID() ? message.RefTagID.Value.ToString() : "N/A");
        }

        public void OnMessage(QuickFix.FIX50SP2.BusinessMessageReject message, SessionID sessionID)
        {
            _logger.LogError("Business Message Reject received: {Text} (RefMsgType: {RefMsgType}, Reason: {Reason})", 
                message.IsSetText() ? message.Text.Value : "No text",
                message.IsSetRefMsgType() ? message.RefMsgType.Value : "N/A",
                message.IsSetBusinessRejectReason() ? message.BusinessRejectReason.Value.ToString() : "N/A");

            // Update order status if possible
            if (message.IsSetBusinessRejectRefID())
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
                    orderService.UpdateOrderStatusAsync(message.BusinessRejectRefID.Value, "REJECTED", null, message.IsSetText() ? message.Text.Value : "Business Reject").GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating status for Business Reject");
                }
            }
        }
    }
}
