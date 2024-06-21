using Application.Helpers;
using Application.Interfaces;
using Application.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ContactRegistry.WebApi.Controllers
{
    /// <summary>
    /// AccountController.
    /// </summary>
    [ApiController]
    [Route("api/account")]
    public class AccountController : ControllerBase
    {
        private readonly IAccountService accountService;

        /// <summary>
        /// AccountController.
        /// </summary>
        /// <param name="accountService"></param>
        public AccountController(IAccountService accountService)
        {
            this.accountService = accountService;
        }

        /// <summary>
        /// UploadAsync.
        /// </summary>
        /// <param name="file">csv file.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadAsync([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("Invalid file.");
            }
            var stream = file.OpenReadStream();
            var accounts = await CsvFileReader.ReadCsvAsync<AccountCsv>(stream);

            await accountService.ProcessAccountAsync(accounts);

            return Ok("File processed successfully.");
        }

        /// <summary>
        /// ExportDataAsync.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [HttpGet("export")]
        public async Task<IActionResult> ExportDataAsync()
        {
            var accountsStream = await JsonStreamHelper.CreateJsonStreamAsync(accountService.StreamAccountsJsonAsync);
            return File(accountsStream, "application/json", "CreAccounts.json");
        }
    }
}
