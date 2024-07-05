// <copyright file="ContactController.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Helpers;
using Application.Interfaces;
using Application.Models;
using Application.Services;
using Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ContactRegistry.WebApi.Controllers
{
    /// <summary>
    /// ContactController.
    /// </summary>
    [ApiController]
    [Route("api/contact")]
    public class ContactController : ControllerBase
    {
        private readonly IContactService _contactService;

        /// <summary>
        /// ContactController.
        /// </summary>
        /// <param name="contactService">contactService.</param>
        public ContactController(IContactService contactService)
        {
            _contactService = contactService;
        }

        /// <summary>
        /// UploadAsync.
        /// </summary>
        /// <param name="file">csv file</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        [HttpPost]
        public async Task<IActionResult> UploadAsync([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Invalid file.");

            var stream = file.OpenReadStream();

            var contacts = CsvFileReader.ReadStreamAsync<ContactCsv>(stream);

            await _contactService.ProcessContactAsync(contacts);

            return Ok("File processed successfully.");
        }

        /// <summary>
        /// ExportDataAsync.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [HttpGet("export")]
        public async Task<IActionResult> ExportDataAsync()
        {
            var accountsStream = await JsonStreamHelper.CreateJsonStreamAsync(_contactService.StreamContactsJsonAsync);
            return File(accountsStream, "application/json", "CreContacts.json");
        }
    }
}
