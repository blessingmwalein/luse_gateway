using System.Threading.Tasks;

namespace LuseGateway.Core.Services
{
    public interface IBillingService
    {
        Task<decimal> CalculateLuseChargesAsync(decimal amount);
        Task ProcessRefundAsync(string shareholder, string reference, decimal amount);
        Task RecordTransactionChargeAsync(string transactionCode, string chargeName, decimal amount);
    }
}
