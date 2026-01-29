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
        public string SecurityType { get; set; }

        [Column("BestBid")]
        public decimal? BestBid { get; set; }

        [Column("BestAsk")]
        public decimal? BestAsk { get; set; }

        [Column("VwapPrice")]
        public decimal? VwapPrice { get; set; }

        [Column("OpeningPrice")]
        public decimal? OpeningPrice { get; set; }

        [Column("ClosingPrice")]
        public decimal? ClosingPrice { get; set; }

        [Column("Settlementprice")]
        public decimal? SettlementPrice { get; set; }

        [Column("HighestPrice")]
        public decimal? HighestPrice { get; set; }

        [Column("LowestPrice")]
        public decimal? LowestPrice { get; set; }

        [Column("ShareVOL")]
        public string ShareVolume { get; set; }

        [Column("Openinterest")]
        public string OpenInterest { get; set; }

        [Column("maturitydate")]
        [StringLength(50)]
        public string MaturityDate { get; set; }
    }
}
