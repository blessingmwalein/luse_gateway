using Microsoft.EntityFrameworkCore;
using LuseGateway.Core.Models;

namespace LuseGateway.Core.Data
{
    public class LuseDbContext : DbContext
    {
        public LuseDbContext(DbContextOptions<LuseDbContext> options)
            : base(options)
        {
        }

        public DbSet<PreOrderLive> PreOrders { get; set; }
        public DbSet<Trade> Trades { get; set; }
        public DbSet<CompanyPrice> CompanyPrices { get; set; }
        public DbSet<LiveOrder> LiveOrders { get; set; }
        public DbSet<CashTrans> CashTransactions { get; set; }
        public DbSet<ParaBilling> ParaBillings { get; set; }
        public DbSet<ParaCompany> ParaCompanies { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure CashTrans to point to cdsc database
            modelBuilder.Entity<CashTrans>().ToTable("CashTrans", "cdsc.dbo");
        }
    }
}
