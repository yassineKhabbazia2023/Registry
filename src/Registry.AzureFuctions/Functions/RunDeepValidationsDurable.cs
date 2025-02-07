using Application.Interfaces;
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

        public RunDeepValidationsDurable(
            ILoggerFactory loggerFactory,
            IAccountDeepValidationService accountDeepValidationService,
            IContactsDeepValidationsService contactsDeepValidationsService,
            IRoleService roleService)
        {
            this.log = loggerFactory.CreateLogger<RunDeepValidationsDurable>();
            this.accountDeepValidationService = accountDeepValidationService;
            this.contactsDeepValidationsService = contactsDeepValidationsService;
            this.roleService = roleService;
        }

        [Function("RunDeepValidationsDurable")]
        public async Task RunOrchestrator([OrchestrationTrigger] TaskOrchestrationContext context)
        {
            if (context != null)
            {
                await context.CallActivityAsync<Task>(nameof(this.RunContactsDeepValidations), string.Empty);
                await context.CallActivityAsync<Task>(nameof(this.RunAccountsDeepValidations), string.Empty);
                await context.CallActivityAsync<Task>(nameof(this.RunRolesDeepValidations), string.Empty);
            }

            this.log.LogInformation("Done");
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
