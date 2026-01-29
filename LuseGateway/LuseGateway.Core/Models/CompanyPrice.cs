using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LuseGateway.Core.Models
{
    [Table("CompanyPrices")]
    public class CompanyPrice
    {
        [Key]
        [Column("Company")]
        [StringLength(50)]
        public string Company { get; set; }

        [Column("SecurityType")]
        [StringLength(20)]
        public string? SecurityType { get; set; }

        [Column("BestBid")]
        public double? BestBid { get; set; }

        [Column("BestAsk")]
        public double? BestAsk { get; set; }

        [Column("VwapPrice")]
        public double? VwapPrice { get; set; }

        [Column("OpeningPrice")]
        public double? OpeningPrice { get; set; }

        [Column("ClosingPrice")]
        public double? ClosingPrice { get; set; }

        [Column("Settlementprice")]
        public double? SettlementPrice { get; set; }

        [Column("HighestPrice")]
        public double? HighestPrice { get; set; }

        [Column("LowestPrice")]
        public double? LowestPrice { get; set; }

        [Column("ShareVOL")]
        public double? ShareVolume { get; set; }

        [Column("Openinterest")]
        public double? OpenInterest { get; set; }

        [Column("maturitydate")]
        [StringLength(50)]
        public string? MaturityDate { get; set; }

        [Column("previousdaysindex")]
        public long? PreviousDaysIndex { get; set; }

        [Column("weight")]
        public long? Weight { get; set; }
    }
}
