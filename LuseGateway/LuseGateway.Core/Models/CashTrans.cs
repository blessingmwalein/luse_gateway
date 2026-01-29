using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LuseGateway.Core.Models
{
    // Note: This table resides in the [cdsc] database
    [Table("CashTrans", Schema = "cdsc.dbo")]
    public class CashTrans
    {
        [Key]
        public long Id { get; set; }

        [Column("Description")]
        public string Description { get; set; }

        [Column("TransType")]
        public string TransType { get; set; }

        [Column("Amount")]
        public decimal Amount { get; set; }

        [Column("DateCreated")]
        public DateTime DateCreated { get; set; }

        [Column("CDS_Number")]
        public string CdsNumber { get; set; }

        [Column("Reference")]
        public string Reference { get; set; }
    }
}
