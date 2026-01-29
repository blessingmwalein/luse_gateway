using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LuseGateway.Core.Models
{
    [Table("Trades")]
    public class Trade
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("SecurityID")]
        [StringLength(50)]
        public string SecurityId { get; set; }

        [Column("MatchedQuantity")]
        public string MatchedQuantity { get; set; }

        [Column("MatchedPrice")]
        public string MatchedPrice { get; set; }

        [Column("GrossTradeAmount")]
        public string GrossTradeAmount { get; set; }

        [Column("MatchedDate")]
        public DateTime MatchedDate { get; set; }

        [Column("OrderNumber")]
        [StringLength(50)]
        public string OrderNumber { get; set; }

        [Column("TradingAccount")]
        [StringLength(50)]
        public string TradingAccount { get; set; }

        [Column("SettlementDate")]
        [StringLength(50)]
        public string SettlementDate { get; set; }

        [Column("SIDE")]
        [StringLength(10)]
        public string Side { get; set; }
    }
}
