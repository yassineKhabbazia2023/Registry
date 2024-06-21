using Application.Helpers;
using Application.Interfaces;
using Application.Models;
using Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace ContactRegistry.WebApi.Controllers
{
    /// <summary>
    /// RoleController.
    /// </summary>
    [ApiController]
    [Route("api/role")]
    public class RoleController : ControllerBase
    {
        private readonly IRoleService roleService;

        /// <summary>
        /// RoleController.
        /// </summary>
        /// <param name="roleService">roleService.</param>
        public RoleController(IRoleService roleService)
        {
            this.roleService = roleService;
        }

        /// <summary>
        /// UploadAsync.
        /// </summary>
        /// <param name="file">csv file.</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> UploadAsync([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("Invalid file.");
            }

            var stream = file.OpenReadStream();

            var roles = await CsvFileReader.ReadCsvAsync<RoleCsv>(stream);

            await roleService.ProcessRoleAsync(roles);

            return Ok("File processed successfully.");
        }

        /// <summary>
        /// ExportDataAsync.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [HttpGet("export")]
        public async Task<IActionResult> ExportDataAsync()
        {
            var accountsStream = await JsonStreamHelper.CreateJsonStreamAsync(roleService.StreamRolesJsonAsync);
            return File(accountsStream, "application/json", "CreRoles.json");
        }
    }
}
