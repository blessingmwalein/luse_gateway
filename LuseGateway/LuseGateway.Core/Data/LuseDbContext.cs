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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Additional configurations if needed
            modelBuilder.Entity<PreOrderLive>()
                .Property(p => p.OrderNo)
                .ValueGeneratedOnAdd();
        }
    }
}
