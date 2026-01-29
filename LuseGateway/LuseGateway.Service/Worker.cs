using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;
using QuickFix;
using QuickFix.Transport;
using QuickFix.Fields;
using QuickFix.Store;
using QuickFix.Logger;
using LuseGateway.Core.Services;
using LuseGateway.Fix;
using LuseGateway.Core.Models;

namespace LuseGateway.Service
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly LuseFixApplication _fixApplication;
        private IInitiator? _initiator;

        public Worker(ILogger<Worker> logger, IServiceProvider serviceProvider, LuseFixApplication fixApplication)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _fixApplication = fixApplication;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("LUSE Gateway Worker starting at: {time}", DateTimeOffset.Now);

            try
            {
                // 1. Initialize FIX Session
                // In a real app, these paths would be in configuration
                string fixConfigPath = "fix_client.cfg"; 
                if (!System.IO.File.Exists(fixConfigPath))
                {
                    _logger.LogError("FIX configuration file not found: {Path}. Waiting before retry...", fixConfigPath);
                    await Task.Delay(10000, stoppingToken);
                    return;
                }

                var settings = new SessionSettings(fixConfigPath);
                var storeFactory = new FileStoreFactory(settings);
                var logFactory = new FileLogFactory(settings);
                var messageFactory = new DefaultMessageFactory();

                _initiator = new SocketInitiator(_fixApplication, storeFactory, settings, logFactory, messageFactory);
                _initiator.Start();

                _logger.LogInformation("FIX Initiator started.");

                // 2. Main Processing Loop
                while (!stoppingToken.IsCancellationRequested)
                {
                    if (_fixApplication.ActiveSessionId != null && _initiator.IsLoggedOn)
                    {
                        await PollAndSendOrdersAsync();
                    }
                    else
                    {
                        _logger.LogWarning("FIX Session not active. Waiting...");
                    }

                    await Task.Delay(5000, stoppingToken); // Poll every 5 seconds
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Fatal error in Worker Service");
            }
            finally
            {
                _initiator?.Stop();
                _logger.LogInformation("LUSE Gateway Worker stopped.");
            }
        }

        private async Task PollAndSendOrdersAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();

            var pendingOrders = await orderService.GetPendingOrdersAsync();

            foreach (var order in pendingOrders)
            {
                try
                {
                    _logger.LogInformation("Sending Order: {OrderNo} - {Symbol}", order.OrderNo, order.Symbol);
                    
                    var fixOrder = CreateFixOrder(order);
                    bool sent = Session.SendToTarget(fixOrder, _fixApplication.ActiveSessionId!);

                    if (sent)
                    {
                        await orderService.MarkAsPostedAsync(order.OrderNo);
                        _logger.LogInformation("Order {OrderNo} marked as POSTED", order.OrderNo);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending order {OrderNo}", order.OrderNo);
                }
            }
        }

        private QuickFix.FIX50.NewOrderSingle CreateFixOrder(PreOrderLive order)
        {
            var newOrder = new QuickFix.FIX50.NewOrderSingle(
                new ClOrdID(order.OrderNumber ?? Guid.NewGuid().ToString("N")),
                new Side(order.Side == "BUY" ? Side.BUY : Side.SELL),
                new TransactTime(DateTime.UtcNow),
                new OrdType(OrdType.LIMIT)
            );

            newOrder.Set(new Symbol(order.Symbol ?? ""));
            newOrder.Set(new OrderQty(order.Quantity));
            newOrder.Set(new Price((decimal)order.BasePrice));
            newOrder.Set(new Account(order.CdsAccount ?? ""));
            newOrder.Set(new SecurityID(order.Company ?? ""));
            
            if (!string.IsNullOrEmpty(order.SecurityType))
                newOrder.Set(new SecurityType(order.SecurityType));

            // Set Party IDs (Broker and Trader)
            var partyGroup = new QuickFix.FIX50.NewOrderSingle.NoPartyIDsGroup();
            partyGroup.Set(new PartyID(order.BrokerCode ?? ""));
            partyGroup.Set(new PartyRole(PartyRole.EXECUTING_FIRM));
            newOrder.AddGroup(partyGroup);

            if (!string.IsNullOrEmpty(order.Trader))
            {
                var traderGroup = new QuickFix.FIX50.NewOrderSingle.NoPartyIDsGroup();
                traderGroup.Set(new PartyID(order.Trader ?? ""));
                traderGroup.Set(new PartyRole(PartyRole.ORDER_ORIGINATION_TRADER));
                newOrder.AddGroup(traderGroup);
            }

            return newOrder;
        }
    }
}
