using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LuseGateway.Core.Models
{
    [Table("para_Billing")]
    public class ParaBilling
    {
        [Key]
        [Column("ChargeName")]
        public string ChargeName { get; set; }

        [Column("percentageorvalue")]
        public decimal PercentageOrValue { get; set; }

        [Column("Amount")]
        public decimal? Amount { get; set; }
    }
}
