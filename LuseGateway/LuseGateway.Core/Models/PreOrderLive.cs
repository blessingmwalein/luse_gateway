using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LuseGateway.Core.Models
{
    [Table("Pre_Order_Live")]
    public class PreOrderLive
    {
        [Key]
        [Column("orderno")]
        public long OrderNo { get; set; }

        [Column("OrderNumber")]
        [StringLength(50)]
        public string OrderNumber { get; set; }

        [Column("Company")]
        [StringLength(50)]
        public string Company { get; set; }

        [Column("Symbol")]
        [StringLength(50)]
        public string Symbol { get; set; }

        [Column("SecurityType")]
        [StringLength(20)]
        public string SecurityType { get; set; }

        [Column("CDS_AC_No")]
        [StringLength(50)]
        public string CdsAccount { get; set; }

        [Column("Broker_Code")]
        [StringLength(50)]
        public string BrokerCode { get; set; }

        [Column("Trader")]
        [StringLength(50)]
        public string Trader { get; set; }

        [Column("Quantity")]
        public int Quantity { get; set; }

        [Column("BasePrice")]
        public double BasePrice { get; set; }

        [Column("Side")]
        [StringLength(10)]
        public string Side { get; set; }

        [Column("OrderStatus")]
        [StringLength(50)]
        public string OrderStatus { get; set; }

        [Column("Create_date")]
        public DateTime? CreateDate { get; set; }

        [Column("Expiry_Date")]
        public string ExpiryDate { get; set; }

        [Column("TimeInForce")]
        [StringLength(50)]
        public string TimeInForce { get; set; }

        [Column("OrderCapacity")]
        [StringLength(20)]
        public string OrderCapacity { get; set; }

        [Column("exchange_orderNumber")]
        [StringLength(50)]
        public string ExchangeOrderNumber { get; set; }

        [Column("BrokerRef")]
        [StringLength(100)]
        public string BrokerRef { get; set; }

        [Column("OrderAttribute")]
        public string OrderAttribute { get; set; }

        [Column("MatchedDate")]
        public DateTime? MatchedDate { get; set; }

        [Column("MatchedPrice")]
        public decimal? MatchedPrice { get; set; }
        
        [Column("leavesQuantity")]
        public string? LeavesQuantity { get; set; }
        
        [Column("RejectionReason")]
        public string? RejectionReason { get; set; }
    }
}
