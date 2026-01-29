using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using LuseGateway.Core.Data;
using LuseGateway.Core.Models;

namespace LuseGateway.Core.Services
{
    public class BillingService : IBillingService
    {
        private readonly LuseDbContext _dbContext;

        public BillingService(LuseDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<decimal> CalculateLuseChargesAsync(decimal amount)
        {
            var billing = await _dbContext.ParaBillings
                .FirstOrDefaultAsync(b => b.ChargeName == "Luse Charges");
            
            if (billing != null)
            {
                return amount * (decimal)billing.PercentageOrValue;
            }

            // Fallback to 1.5% if not found in DB (based on legacy logic observation)
            return amount * 0.015m;
        }

        public async Task ProcessRefundAsync(string shareholder, string reference, decimal amount)
        {
            var refund = new CashTrans
            {
                Description = "Cash Refund",
                TransType = "Refund",
                Amount = amount,
                DateCreated = DateTime.Now,
                CdsNumber = shareholder,
                Reference = reference
            };

            _dbContext.CashTransactions.Add(refund);
            await _dbContext.SaveChangesAsync();
        }

        public async Task RecordTransactionChargeAsync(string transactionCode, string chargeName, decimal amount)
        {
            // Placeholder for record transaction charge logic if needed
            // Based on legacy logic for TransactionCharges table
            await Task.CompletedTask;
        }
    }
}
