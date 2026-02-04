using System;
using System.Collections.Generic;

namespace LuseGateway.Core.Models
{
    public class SttMarketPrice
    {
        public string Symbol { get; set; } = string.Empty;
        public string SecurityDescription { get; set; } = string.Empty;
        public decimal OpenPx { get; set; }
        public decimal HighPx { get; set; }
        public decimal LowPx { get; set; }
        public decimal ClosePx { get; set; }
        public long Volume { get; set; }
        public decimal Turnover { get; set; }
        public int Deals { get; set; }
        public DateTime TradeDate { get; set; }
    }

    public class SttTurnoverVolumeDeal
    {
        public string Symbol { get; set; } = string.Empty;
        public long Volume { get; set; }
        public decimal Turnover { get; set; }
        public int Deals { get; set; }
    }

    public class SttTradeDailySummary
    {
        public DateTime TradeDate { get; set; }
        public long TotalVolume { get; set; }
        public decimal TotalTurnover { get; set; }
        public int TotalDeals { get; set; }
    }

    public class SttMarketCap
    {
        public string Symbol { get; set; } = string.Empty;
        public long IssuedShares { get; set; }
        public decimal MarketPrice { get; set; }
        public decimal MarketCapitalization { get; set; }
    }
}
