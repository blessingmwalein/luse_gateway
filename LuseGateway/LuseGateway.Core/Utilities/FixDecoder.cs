using System;
using System.Collections.Generic;
using System.Linq;

namespace LuseGateway.Core.Utilities
{
    public static class FixDecoder
    {
        private static readonly Dictionary<string, string> TagNames = new()
        {
            { "8", "BeginString" },
            { "9", "BodyLength" },
            { "35", "MsgType" },
            { "34", "MsgSeqNum" },
            { "49", "SenderCompID" },
            { "52", "SendingTime" },
            { "56", "TargetCompID" },
            { "11", "ClOrdID" },
            { "38", "OrderQty" },
            { "40", "OrdType" },
            { "44", "Price" },
            { "48", "SecurityID" },
            { "54", "Side" },
            { "55", "Symbol" },
            { "58", "Text" },
            { "60", "TransactTime" },
            { "150", "ExecType" },
            { "151", "LeavesQty" },
            { "39", "OrdStatus" },
            { "17", "ExecID" },
            { "37", "OrderID" },
            { "32", "LastQty" },
            { "31", "LastPx" },
            { "167", "SecurityType" },
            { "453", "NoPartyIDs" },
            { "448", "PartyID" },
            { "447", "PartyIDSource" },
            { "452", "PartyRole" },
            { "528", "OrderCapacity" },
            { "10", "CheckSum" }
        };

        private static readonly Dictionary<string, string> MsgTypeNames = new()
        {
            { "0", "Heartbeat" },
            { "A", "Logon" },
            { "5", "Logout" },
            { "1", "TestRequest" },
            { "h", "TradingSessionStatus" },
            { "D", "NewOrderSingle" },
            { "8", "ExecutionReport" },
            { "W", "MarketDataSnapshot" },
            { "X", "MarketDataIncremental" },
            { "d", "SecurityDefinition" },
            { "V", "MarketDataRequest" },
            { "j", "BusinessMessageReject" },
            { "3", "Reject" }
        };

        public static List<FixTagValue> Decode(string raw)
        {
            var result = new List<FixTagValue>();
            if (string.IsNullOrEmpty(raw)) return result;

            char separator = raw.Contains('\u0001') ? '\u0001' : '|';
            var parts = raw.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                var kv = part.Split('=');
                if (kv.Length == 2)
                {
                    string tag = kv[0];
                    string value = kv[1];
                    string name = TagNames.TryGetValue(tag, out var tagName) ? tagName : $"Tag_{tag}";
                    
                    // Special handling for MsgType
                    if (tag == "35" && MsgTypeNames.TryGetValue(value, out var msgTypeName))
                    {
                        value = $"{value} ({msgTypeName})";
                    }

                    result.Add(new FixTagValue { Tag = tag, Name = name, Value = value });
                }
            }

            return result;
        }

        public static string GetSummary(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "";
            var tags = Decode(raw);
            var msgType = tags.FirstOrDefault(t => t.Tag == "35")?.Value ?? "Unknown";
            var sender = tags.FirstOrDefault(t => t.Tag == "49")?.Value ?? "";
            var target = tags.FirstOrDefault(t => t.Tag == "56")?.Value ?? "";
            
            return $"{msgType} [{sender} -> {target}]";
        }
    }

    public class FixTagValue
    {
        public string Tag { get; set; } = "";
        public string Name { get; set; } = "";
        public string Value { get; set; } = "";
    }
}
