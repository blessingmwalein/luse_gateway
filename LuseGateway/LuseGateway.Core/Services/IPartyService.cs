using System.Collections.Generic;
using System.Threading.Tasks;
using LuseGateway.Core.Models;

namespace LuseGateway.Core.Services
{
    public interface IPartyService
    {
        Task AddTemporaryPartyAsync(ParaCompany party);
        IEnumerable<ParaCompany> GetTemporaryParties();
        void ClearTemporaryParties();
        Task SyncPartiesToDatabaseAsync();
    }
}
