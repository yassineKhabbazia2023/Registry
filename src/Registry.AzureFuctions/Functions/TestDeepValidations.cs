using Application.Interfaces;
using Infrastructure.Adapters;
using Infrastructure.BackgroundJobs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Registry.AzureFuctions.Functions
{
    public class TestDeepValidations
    {
        private IAccountDeepValidationService accountDeepValidationService;
        private IContactsDeepValidationsService contactsDeepValidationsService;
        private IRoleService roleService;
        private OrchestratorJob orchestratorJob;
        private IBackgroundJobEnqueuer backgroundJobEnqueuer;

        public TestDeepValidations(
            IAccountDeepValidationService accountDeepValidationService,
            IContactsDeepValidationsService contactsDeepValidationsService,
            IRoleService roleService,
            OrchestratorJob orchestratorJob,
            IBackgroundJobEnqueuer backgroundJobEnqueuer)
        {
            this.accountDeepValidationService = accountDeepValidationService;
            this.contactsDeepValidationsService = contactsDeepValidationsService;
            this.roleService = roleService;
            this.orchestratorJob = orchestratorJob;
            this.backgroundJobEnqueuer = backgroundJobEnqueuer;
        }

        [Function("TestDeepValidations")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
        {
            await this.contactsDeepValidationsService.CreateValidContactsOperationsAsync();
            await this.accountDeepValidationService.CreateValidAccountsOperationsAsync();
            await this.roleService.CreateValidRolesOperationsAsync();

            this.backgroundJobEnqueuer.Enqueue<OrchestratorJob>(x => x.ProcessOrder());
            return new OkObjectResult("TestDeepValidations Executed Successfully!");
        }
    }
}
