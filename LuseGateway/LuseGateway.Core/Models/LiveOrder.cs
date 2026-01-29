using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LuseGateway.Core.Models
{
    [Table("Live_Orders")]
    public class LiveOrder
    {
        [Key]
        [Column("OrderNo")]
        public long OrderNo { get; set; }

        [Column("Company")]
        public string? Company { get; set; }

        [Column("Symbol")]
        public string? Symbol { get; set; }

        [Column("SecurityType")]
        public string? SecurityType { get; set; }

        [Column("Create_date")]
        public DateTime? CreateDate { get; set; }

        [Column("OrderStatus")]
        public string? OrderStatus { get; set; }

        [Column("OrderType")]
        public string? OrderType { get; set; }

        [Column("Quantity")]
        public int Quantity { get; set; }

        [Column("BasePrice")]
        public double BasePrice { get; set; }

        [Column("Side")]
        public string? Side { get; set; }

        [Column("TimeInForce")]
        public string? TimeInForce { get; set; }

        [Column("Expiry_Date")]
        public DateTime? ExpiryDate { get; set; }

        [Column("MaturityDate")]
        public string? MaturityDate { get; set; }

        [Column("OrderIdentifier")]
        public string? OrderIdentifier { get; set; }

        [Column("CDS_AC_No")]
        public string? CdsAccount { get; set; }

        [Column("Broker_Code")]
        public string? BrokerCode { get; set; }

        [Column("Trader")]
        public string? Trader { get; set; }

        [Column("OrderAttribute")]
        public string? OrderAttribute { get; set; }

        [Column("exchange_orderNumber")]
        public string? ExchangeOrderNumber { get; set; }

        [Column("leavesQuantity")]
        public string? LeavesQuantity { get; set; }

        [Column("MatchedPrice")]
        public decimal? MatchedPrice { get; set; }

        [Column("MatchedDate")]
        public DateTime? MatchedDate { get; set; }

        [Column("BrokerRef")]
        public string? BrokerRef { get; set; }
    }
}
