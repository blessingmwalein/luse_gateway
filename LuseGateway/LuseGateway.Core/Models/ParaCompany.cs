using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LuseGateway.Core.Models
{
    [Table("para_company")]
    public class ParaCompany
    {
        [Key]
        [Column("Symbol")]
        public string? Symbol { get; set; }

        [Column("Company")]
        public string? Company { get; set; }

        [Column("Fnam")]
        public string? Fnam { get; set; }

        [Column("exchange")]
        public string? Exchange { get; set; }

        [Column("SecurityType")]
        public string? SecurityType { get; set; }

        [Column("Date_created")]
        public DateTime? DateCreated { get; set; }

        [Column("ISIN_No")]
        public string? IsinNo { get; set; }
    }
}
