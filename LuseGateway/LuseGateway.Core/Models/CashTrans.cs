using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LuseGateway.Core.Models
{
    // Note: This table resides in the [cds] database
    [Table("CashTrans", Schema = "cds.dbo")]
    public class CashTrans
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

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
