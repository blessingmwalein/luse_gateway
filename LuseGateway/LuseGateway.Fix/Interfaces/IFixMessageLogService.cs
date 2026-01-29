using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LuseGateway.Fix.Interfaces
{
    public interface IFixMessageLogService
    {
        Task LogMessageAsync(string direction, string message);
        IEnumerable<FixMessageEntry> GetRecentMessages();
    }

    public class FixMessageEntry
    {
        public DateTime Timestamp { get; set; }
        public string Direction { get; set; } // "IN" or "OUT"
        public string Content { get; set; }
    }
}
