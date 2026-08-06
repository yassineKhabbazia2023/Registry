using Application.Consts;
using Application.Interfaces;
using Hangfire;
using Infrastructure.Adapters;
using Infrastructure.BackgroundJobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Registry.AzureFuctions.Functions
{
    public class RunDeepValidationsDurable
    {
        private readonly ILogger<RunDeepValidationsDurable> log;
        private readonly IAccountDeepValidationService accountDeepValidationService;
        private readonly IContactsDeepValidationsService contactsDeepValidationsService;
        private readonly IRoleService roleService;
        private readonly IBackgroundJobEnqueuer backgroundJobEnqueuer;
        private readonly IAkuiteoContactSyncOperationService akuiteoContactSyncOperationService;
        private readonly IFeatureFlagService featureFlagService;

        public RunDeepValidationsDurable(
            ILoggerFactory loggerFactory,
            IAccountDeepValidationService accountDeepValidationService,
            IContactsDeepValidationsService contactsDeepValidationsService,
            IRoleService roleService,
            IBackgroundJobEnqueuer backgroundJobEnqueuer,
            IAkuiteoContactSyncOperationService akuiteoContactSyncOperationService,
            IFeatureFlagService featureFlagService)
        {
            this.log = loggerFactory.CreateLogger<RunDeepValidationsDurable>();
            this.accountDeepValidationService = accountDeepValidationService;
            this.contactsDeepValidationsService = contactsDeepValidationsService;
            this.roleService = roleService;
            this.backgroundJobEnqueuer = backgroundJobEnqueuer;
            this.akuiteoContactSyncOperationService = akuiteoContactSyncOperationService;
            this.featureFlagService = featureFlagService;
        }

        [Function("RunDeepValidationsDurable")]
        public async Task RunOrchestrator([OrchestrationTrigger] TaskOrchestrationContext context)
        {
            if (context != null)
            {
                try
                {
                    await context.CallActivityAsync<Task>(nameof(this.ProcessAkuiteoContactSyncOperations), string.Empty);
                }
                catch (TaskFailedException exception)
                {
                    this.log.LogError(exception, "Akuiteo contact synchronization activity failed. Existing deep validation activities will continue.");
                }

                await context.CallActivityAsync<Task>(nameof(this.RunContactsDeepValidations), string.Empty);
                await context.CallActivityAsync<Task>(nameof(this.RunAccountsDeepValidations), string.Empty);
                await context.CallActivityAsync<Task>(nameof(this.RunRolesDeepValidations), string.Empty);
                await context.CallActivityAsync<Task>(nameof(this.TriggerOrchestrationProcess), string.Empty);
            }

            this.log.LogInformation("Done");
        }

        [Function(nameof(ProcessAkuiteoContactSyncOperations))]
        public async Task ProcessAkuiteoContactSyncOperations([ActivityTrigger] string input)
        {
            if (!this.featureFlagService.IsEnabled(FeatureFlagKeys.IsContactAkuiteoSynchronizationEnabled))
            {
                this.log.LogInformation("Skipping pending Akuiteo contact synchronization operations because the feature flag is disabled.");
                return;
            }

            await this.akuiteoContactSyncOperationService.ProcessPendingOperationsAsync();
        }

        [Function(nameof(RunContactsDeepValidations))]
        public async Task RunContactsDeepValidations([ActivityTrigger] string input)
        {
            await this.contactsDeepValidationsService.CreateValidContactsOperationsAsync();
        }

        [Function(nameof(RunAccountsDeepValidations))]
        public async Task RunAccountsDeepValidations([ActivityTrigger] string input)
        {
            await this.accountDeepValidationService.CreateValidAccountsOperationsAsync();
        }

        [Function(nameof(RunRolesDeepValidations))]
        public async Task RunRolesDeepValidations([ActivityTrigger] string input)
        {
            await this.roleService.CreateValidRolesOperationsAsync();
        }

        [Function(nameof(TriggerOrchestrationProcess))]
        public async Task TriggerOrchestrationProcess([ActivityTrigger] string input)
        {
            await Task.FromResult(this.backgroundJobEnqueuer.Enqueue<OrchestratorJob>(x => x.ProcessOrder()));
        }

        [Function("RunDeepValidationsDurable_TimerTriggerStartClient")]
        public async Task TimerTriggerStart(
             [TimerTrigger("%RunDeepValidationsDurable_FREQUENCY%")] TimerInfo timerInfo,
             [DurableClient] DurableTaskClient client)
        {
            string instanceId = await client!.ScheduleNewOrchestrationInstanceAsync(
                "RunDeepValidationsDurable",
                string.Empty);
            this.log.LogDebug($"Started RunDeepValidationsDurable orchestration with ID = '{instanceId}'.");
        }
    }
}
