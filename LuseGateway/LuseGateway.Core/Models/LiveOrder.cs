using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LuseGateway.Core.Models
{
    [Table("Live_Orders")]
    public class LiveOrder
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("Company")]
        [StringLength(50)]
        public string Company { get; set; }

        [Column("SecurityType")]
        [StringLength(50)]
        public string SecurityType { get; set; }

        [Column("Create_date")]
        public DateTime CreateDate { get; set; }

        [Column("OrderStatus")]
        [StringLength(50)]
        public string OrderStatus { get; set; }

        [Column("Quantity")]
        public decimal Quantity { get; set; }

        [Column("BasePrice")]
        public decimal BasePrice { get; set; }

        [Column("TimeInForce")]
        [StringLength(50)]
        public string TimeInForce { get; set; }

        [Column("MaturityDate")]
        [StringLength(50)]
        public string MaturityDate { get; set; }

        [Column("Side")]
        [StringLength(10)]
        public string Side { get; set; }

        [Column("OrderIdentifier")]
        [StringLength(100)]
        public string OrderIdentifier { get; set; }

        [Column("CDS_AC_No")]
        [StringLength(50)]
        public string CdsAccount { get; set; }

        [Column("Broker_code")]
        [StringLength(50)]
        public string BrokerCode { get; set; }

        [Column("Trader")]
        [StringLength(50)]
        public string Trader { get; set; }

        [Column("OrderAttribute")]
        public string OrderAttribute { get; set; }
    }
}
