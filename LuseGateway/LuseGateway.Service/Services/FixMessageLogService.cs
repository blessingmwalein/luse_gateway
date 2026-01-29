using Microsoft.AspNetCore.SignalR;
using LuseGateway.Service.Hubs;
using LuseGateway.Fix.Interfaces;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LuseGateway.Service.Services
{
    public class FixMessageLogService : IFixMessageLogService
    {
        private readonly IHubContext<GatewayHub> _hubContext;
        private readonly ConcurrentQueue<FixMessageEntry> _recentMessages = new ConcurrentQueue<FixMessageEntry>();
        private const int MaxMessages = 100;

        public FixMessageLogService(IHubContext<GatewayHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task LogMessageAsync(string direction, string message)
        {
            var entry = new FixMessageEntry
            {
                Timestamp = System.DateTime.Now,
                Direction = direction,
                Content = message
            };

            _recentMessages.Enqueue(entry);
            while (_recentMessages.Count > MaxMessages)
            {
                _recentMessages.TryDequeue(out _);
            }

            await _hubContext.Clients.All.SendAsync("ReceiveFixMessage", entry);
        }

        public IEnumerable<FixMessageEntry> GetRecentMessages() => _recentMessages.ToList();
    }
}
