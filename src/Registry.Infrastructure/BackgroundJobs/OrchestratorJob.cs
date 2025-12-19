using Application.Interfaces;
using Application.Options;
using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.BackgroundJobs
{
    public class OrchestratorJob(
        IAccountOrchestrator accountOrchestrator,
        IContactOrchestrator contactOrchestrator,
        IOptions<BackGroundJobOptions> options,
        IRoleOrchestrator roleOrchestrator,
        ILogger<OrchestratorJob> logger,
        IReviewService reviewService)
    {
        private readonly IAccountOrchestrator accountOrchestrator = accountOrchestrator;
        private readonly IContactOrchestrator contactOrchestrator = contactOrchestrator;
        private readonly IRoleOrchestrator roleOrchestrator = roleOrchestrator;
        private readonly IReviewService reviewService = reviewService;
        private readonly BackGroundJobOptions options = options.Value;
        private readonly ILogger<OrchestratorJob> logger = logger;

        public async Task ProcessOrder()
        {
            logger.LogInformation("ProcessOrder started at: {Date} - ProcessOrder", DateTime.UtcNow);

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
            logger.LogInformation("ProcessOrder finished at: {Date} - ProcessOrder", DateTime.UtcNow);
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
