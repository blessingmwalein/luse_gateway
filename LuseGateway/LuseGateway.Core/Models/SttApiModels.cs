using System;
using System.Collections.Generic;

namespace LuseGateway.Core.Models
{
    /// <summary>
    /// STT API authentication response
    /// </summary>
    public class SttAuthResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public long RefreshTokenExpirationTimestampMs { get; set; }
        public bool IsAdmin { get; set; }
        public bool TwoFactor { get; set; }
    }

    /// <summary>
    /// STT API order status response
    /// </summary>
    public class SttOrderStatus
    {
        public int IdOrder { get; set; }
        public string Side { get; set; } = string.Empty;
        public decimal OrderPx { get; set; }
        public long OriginalQty { get; set; }
        public long RemainingQty { get; set; }
        public long FilledQty { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<SttTrade> Trades { get; set; } = new();
    }

    /// <summary>
    /// STT API trade information
    /// </summary>
    public class SttTrade
    {
        public string EntryId { get; set; } = string.Empty;
        public long TradedQty { get; set; }
        public decimal TradedPx { get; set; }
        public DateTime TradeDate { get; set; }
    }

    /// <summary>
    /// STT API client details response
    /// </summary>
    public class SttClientDetails
    {
        public string MemberCode { get; set; } = string.Empty;
        public string MemberCustodian { get; set; } = string.Empty;
        public string ClientCode { get; set; } = string.Empty;
        public string ClientCustodian { get; set; } = string.Empty;
        public string CSDAccountNumber { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// STT API instrument definition
    /// </summary>
    public class SttInstrument
    {
        public int SecurityId { get; set; }
        public int ContractId { get; set; }
        public string SecurityGroup { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public bool Suspended { get; set; }
        public string ListingStatus { get; set; } = string.Empty;
        public SttSecurityType? SecurityType { get; set; }
        public SttMarket? Market { get; set; }
        public SttTickRules? TickRules { get; set; }
    }

    public class SttSecurityType
    {
        public int SecurityTypeId { get; set; }
        public string SecurityTypePrefix { get; set; } = string.Empty;
        public string SecurityType { get; set; } = string.Empty;
        public string SecurityTypeDescr { get; set; } = string.Empty;
    }

    public class SttMarket
    {
        public int MarketId { get; set; }
        public string Market { get; set; } = string.Empty;
    }

    public class SttTickRules
    {
        public string PriceOrRate { get; set; } = string.Empty;
        public decimal Factor { get; set; }
        public int LotSize { get; set; }
        public decimal ContractMultiplier { get; set; }
        public decimal MinPriceIncrement { get; set; }
    }

    /// <summary>
    /// Reconciliation report
    /// </summary>
    public class ReconciliationReport
    {
        public DateTime ReconciledAt { get; set; }
        public int TotalOrders { get; set; }
        public int MatchedOrders { get; set; }
        public int DiscrepanciesFound { get; set; }
        public List<OrderDiscrepancy> Discrepancies { get; set; } = new();
        public List<string> AutoCorrectedOrders { get; set; } = new();
        public List<string> ManualReviewRequired { get; set; } = new();
        public TimeSpan Duration { get; set; }
    }

    /// <summary>
    /// Order discrepancy details
    /// </summary>
    public class OrderDiscrepancy
    {
        public string ClOrdId { get; set; } = string.Empty;
        public string DiscrepancyType { get; set; } = string.Empty; // STATUS_MISMATCH, QTY_MISMATCH, MISSING_EXECUTION, etc.
        public string LocalStatus { get; set; } = string.Empty;
        public string ExchangeStatus { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty; // CRITICAL, WARNING, INFO
        public long? LocalQty { get; set; }
        public long? ExchangeQty { get; set; }
        public decimal? LocalPrice { get; set; }
        public decimal? ExchangePrice { get; set; }
    }

    /// <summary>
    /// Order validation result
    /// </summary>
    public class OrderValidationResult
    {
        public bool IsValid { get; set; }
        public string ClOrdId { get; set; } = string.Empty;
        public bool ExistsOnExchange { get; set; }
        public bool StatusMatches { get; set; }
        public bool QuantityMatches { get; set; }
        public List<string> ValidationMessages { get; set; } = new();
        public SttOrderStatus? ExchangeData { get; set; }
    }
}
