using QuickFix;
using QuickFix.FIX50SP2;
using QuickFix.Fields;
using LuseGateway.Fix;
using System;
using System.Threading.Tasks;

namespace LuseGateway.Service.Services
{
    public interface IFixActionService
    {
        Task RequestMarketDataAsync(string symbol);
        Task RequestSecurityDefinitionsAsync();
        Task RequestOrderStatusAsync(string clOrdId);
        Task RequestPartyIDsAsync();
    }

    public class FixActionService : IFixActionService
    {
        private readonly LuseFixApplication _fixApplication;

        public FixActionService(LuseFixApplication fixApplication)
        {
            _fixApplication = fixApplication;
        }

        public Task RequestMarketDataAsync(string symbol)
        {
            if (_fixApplication.ActiveSessionId == null) return Task.CompletedTask;

            var request = new MarketDataRequest(
                new MDReqID(Guid.NewGuid().ToString("N")),
                new SubscriptionRequestType(SubscriptionRequestType.SNAPSHOT_PLUS_UPDATES),
                new MarketDepth(1)
            );

            request.Set(new MDUpdateType(1)); // 1 = INCREMENTAL_UPDATE

            var entryGroup = new MarketDataRequest.NoMDEntryTypesGroup();
            entryGroup.Set(new MDEntryType(QuickFix.Fields.MDEntryType.BID));
            request.AddGroup(entryGroup);

            entryGroup.Set(new MDEntryType(QuickFix.Fields.MDEntryType.OFFER));
            request.AddGroup(entryGroup);

            var symbolGroup = new MarketDataRequest.NoRelatedSymGroup();
            symbolGroup.Set(new Symbol(symbol));
            request.AddGroup(symbolGroup);

            Session.SendToTarget(request, _fixApplication.ActiveSessionId);
            return Task.CompletedTask;
        }

        public Task RequestSecurityDefinitionsAsync()
        {
            if (_fixApplication.ActiveSessionId == null) return Task.CompletedTask;

            var request = new SecurityDefinitionRequest();
            request.Set(new SecurityReqID(Guid.NewGuid().ToString("N")));
            request.Set(new SecurityRequestType(3)); // 3 = REQUEST_LIST_SECURITIES

            Session.SendToTarget(request, _fixApplication.ActiveSessionId);
            return Task.CompletedTask;
        }

        public Task RequestOrderStatusAsync(string clOrdId)
        {
            if (_fixApplication.ActiveSessionId == null) return Task.CompletedTask;

            var request = new OrderStatusRequest();
            request.Set(new ClOrdID(clOrdId));
            request.Set(new Side(Side.BUY));

            Session.SendToTarget(request, _fixApplication.ActiveSessionId);
            return Task.CompletedTask;
        }

        public Task RequestPartyIDsAsync()
        {
            if (_fixApplication.ActiveSessionId == null) return Task.CompletedTask;

            // Using generic message to support custom tags 1506, 1507
            var request = new QuickFix.Message();
            request.Header.SetField(new QuickFix.Fields.MsgType("CF"));
            request.SetField(new QuickFix.Fields.StringField(1505, Guid.NewGuid().ToString("N")));
            
            // 1506 NoPartyListResponseTypes = 1
            request.SetField(new QuickFix.Fields.IntField(1506, 1));
            
            // 1507 PartyListResponseType = 1
            var group = new QuickFix.Group(1506, 1507);
            group.SetField(new QuickFix.Fields.IntField(1507, 1));
            request.AddGroup(group);

            Session.SendToTarget(request, _fixApplication.ActiveSessionId);
            return Task.CompletedTask;
        }
    }
}
