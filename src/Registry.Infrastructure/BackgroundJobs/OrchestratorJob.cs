using Application.Consts;
using Application.Interfaces;
using Application.Options;
using EFCore.BulkExtensions;
using Hangfire;
using Microsoft.Extensions.Options;

namespace Infrastructure.BackgroundJobs
{
    public class OrchestratorJob
    {
        private readonly IAccountOrchestrator accountOrchestrator;
        private readonly IContactOrchestrator contactOrchestrator;
        private readonly IRoleOrchestrator roleOrchestrator;
        private readonly IReviewService reviewService;
        private readonly BackGroundJobOptions options;

        public OrchestratorJob(
            IRoleOrchestrator roleOrchestrator,
            IAccountOrchestrator accountOrchestrator,
            IContactOrchestrator contactOrchestrator,
            IReviewService reviewService,
            IOptions<BackGroundJobOptions> options
            )
        {
            this.roleOrchestrator = roleOrchestrator;
            this.contactOrchestrator = contactOrchestrator;
            this.accountOrchestrator = accountOrchestrator;
            this.reviewService = reviewService;
            this.options = options.Value;
        }
        public async Task ProcessOrder()
        {
            var ruleValidator = BackgroundJob.Enqueue(() => this.reviewService.ReviewChangeEmailAsync());

            var insertContactJob = BackgroundJob.ContinueJobWith(ruleValidator, () => this.contactOrchestrator.ProcessContactPublishAsync("INSERT"));
            var insertAccountJob = BackgroundJob.ContinueJobWith(insertContactJob, () => this.accountOrchestrator.ProcessAccountPublishAsync("INSERT"));

            var waitJob = BackgroundJob.ContinueJobWith(insertAccountJob, () => WaitFor(options.TimeToWaitBeforeEachStep));
            var insertRoleJob = BackgroundJob.ContinueJobWith(waitJob, () => this.roleOrchestrator.ProcessRolePublishAsync("INSERT",false));

            var updateContactJob = BackgroundJob.ContinueJobWith(insertRoleJob, () => this.contactOrchestrator.ProcessContactPublishAsync("UPDATE"));
            var updateAccountJob = BackgroundJob.ContinueJobWith(updateContactJob, () => this.accountOrchestrator.ProcessAccountPublishAsync("UPDATE"));
            
            var deletePennylaneRolesJob = BackgroundJob.ContinueJobWith(updateAccountJob, () => this.roleOrchestrator.ProcessRolePublishAsync("DELETE",true));

            if (this.options.ShouldTriggerEvents)
            {
                var deleteRolJob = BackgroundJob.ContinueJobWith(deletePennylaneRolesJob, () => this.roleOrchestrator.ProcessRolePublishAsync("DELETE", false));
                var deleteContactJob = BackgroundJob.ContinueJobWith(deleteRolJob, () => this.contactOrchestrator.ProcessContactPublishAsync("DELETE"));
                var deleteAccountJob = BackgroundJob.ContinueJobWith(deleteContactJob, () => this.accountOrchestrator.ProcessAccountPublishAsync("DELETE"));

                await Task.CompletedTask;
            }

            await Task.CompletedTask;
        }

        public async Task WaitFor(int milliseconds)
        {
            if (milliseconds == 0)
            {
                milliseconds = 300000;
            }
            await Task.Delay(milliseconds);
        }


    }
}
