using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LuseGateway.Core.Data;
using LuseGateway.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace LuseGateway.Core.Services
{
    public class PartyService : IPartyService
    {
        private readonly List<ParaCompany> _temporaryParties = new();
        private readonly IDbContextFactory<LuseDbContext> _dbContextFactory;

        public PartyService(IDbContextFactory<LuseDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public Task AddTemporaryPartyAsync(ParaCompany party)
        {
            lock (_temporaryParties)
            {
                var existing = _temporaryParties.FirstOrDefault(p => p.Symbol == party.Symbol);
                if (existing != null)
                {
                    _temporaryParties.Remove(existing);
                }
                _temporaryParties.Add(party);
            }
            return Task.CompletedTask;
        }

        public IEnumerable<ParaCompany> GetTemporaryParties()
        {
            lock (_temporaryParties)
            {
                return _temporaryParties.ToList();
            }
        }

        public void ClearTemporaryParties()
        {
            lock (_temporaryParties)
            {
                _temporaryParties.Clear();
            }
        }

        public async Task SyncPartiesToDatabaseAsync()
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();
            var partiesToSync = GetTemporaryParties();

            foreach (var party in partiesToSync)
            {
                var existing = await context.ParaCompanies.FirstOrDefaultAsync(p => p.Symbol == party.Symbol);
                if (existing != null)
                {
                    existing.Company = party.Company;
                    existing.Fnam = party.Fnam;
                    existing.Exchange = party.Exchange;
                    existing.SecurityType = party.SecurityType;
                    existing.IsinNo = party.IsinNo;
                    existing.DateCreated = party.DateCreated ?? DateTime.Now;
                }
                else
                {
                    context.ParaCompanies.Add(party);
                }
            }

            await context.SaveChangesAsync();
            ClearTemporaryParties();
        }
    }
}
