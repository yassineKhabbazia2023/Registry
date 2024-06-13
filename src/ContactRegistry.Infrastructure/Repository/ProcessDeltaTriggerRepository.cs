using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repository
{
    public class ProcessDeltaTriggerRepository : IProcessDeltaTriggerRepository
    {
        private readonly ApplicationDbContext context;

        public ProcessDeltaTriggerRepository(ApplicationDbContext context)
        {
            this.context = context;
        }

        public async Task<ProcessDeltaTrigger> GetProcessAsync()
        {
            return await this.context.ProcessDeltaTriggers.FirstAsync();
        }

        public async Task UpdateAccountProcessAsync(bool state)
        {
            var stateLine = await this.GetProcessAsync();
            stateLine.Account = state;
            context.ProcessDeltaTriggers.Update(stateLine);

            await context.SaveChangesAsync();
        }

        public async Task UpdateContactProcessAsync(bool state)
        {
            var stateLine = await this.GetProcessAsync();
            stateLine.Contact = state;
            context.ProcessDeltaTriggers.Update(stateLine);

            await context.SaveChangesAsync();
        }

        public async Task UpdateRoleProcessAsync(bool state)
        {
            var stateLine = await this.GetProcessAsync();
            stateLine.Role = state;
            context.ProcessDeltaTriggers.Update(stateLine);

            await context.SaveChangesAsync();
        }
    }
}
