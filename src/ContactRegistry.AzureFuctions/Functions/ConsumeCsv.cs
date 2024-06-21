using System.IO;
using System.Threading.Tasks;
using Application.Helpers;
using Application.Interfaces;
using Application.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ContactRegistry.AzureFuctions.Functions
{
    public class ConsumeCsv
    {
        private readonly ILogger<ConsumeCsv> _logger;
        private readonly IAccountService accountService;
        private readonly IContactService contactService;
        private readonly IRoleService roleService;

        public ConsumeCsv(ILogger<ConsumeCsv> logger, IAccountService accountService,IContactService contactService, IRoleService roleService)
        {
            _logger = logger;
            this.accountService = accountService;
            this.contactService = contactService;
            this.roleService = roleService;
        }

        [Function(nameof(ConsumeCsv))]
        public async Task Run([BlobTrigger("csvdata/{folder}/{name}", Connection = "csvBlobConnectionString")] Stream stream, string folder , string name)
        {
            _logger.LogInformation("Start process csv in {folder} with name {name}", folder, name);
            switch (folder)
            {
                case "Accounts":
                    await this.ProcessAccountAsync(stream);
                    break;
                case "Contacts":
                    await this.ProcessContactAsync(stream);
                    break;
                case "Roles":
                    await this.ProcessRolesAsync(stream);
                    break;
                default:
                    this._logger.LogError($"Unknown folder: {folder}");
                    break;
            }

            _logger.LogInformation("End process csv in {folder} with name {name}", folder, name);
        }

        private async Task ProcessAccountAsync(Stream stream)
        {
            var accounts = await CsvFileReader.ReadCsvAsync<AccountCsv>(stream);
            await this.accountService.ProcessAccountAsync(accounts);
        }

        private async Task ProcessContactAsync(Stream stream)
        {
            var contacts = await CsvFileReader.ReadCsvAsync<ContactCsv>(stream);
            await this.contactService.ProcessContactAsync(contacts);
        }

        private async Task ProcessRolesAsync(Stream stream)
        {
            var roles = await CsvFileReader.ReadCsvAsync<RoleCsv>(stream);
            await this.roleService.ProcessRoleAsync(roles);
        }
    }
}
