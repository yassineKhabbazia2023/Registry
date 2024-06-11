using Application.Interfaces;
using Application.Models;
using ContactRegistry.WebApi.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Net;
using System.Text;

namespace ContactRegistry.WebApi.Tests.Controllers
{
    public class ContactControllerTest
    {
        [Fact]
        public async Task UploadAsync_InvalidFile_Retuns_BadRequest()
        {
            // Arrnge
            var fileName = "data.csv";
            var stream = new MemoryStream(new byte[0]);

            IFormFile file = new FormFile(stream, 0, stream.Length, "id_from_form", fileName);

            var contactService = new Mock<IContactService>();

            // Act
            var contactController = new ContactController(contactService.Object);
            var response = await contactController.UploadAsync(file) as ObjectResult;

            // Assert
            contactService.Verify(s => s.ProcessContactAsync(It.IsAny<IEnumerable<ContactCsv>>()), Times.Never());
            response!.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task UploadAsync_ValidFile_Retuns_Ok()
        {
            var contact = new ContactCsv(
                Id: new Guid(),
                Email: "john.doe@example.com",
                FirstName: "John",
                LastName: "Doe",
                IsCustomer: true,
                IsActive: true,
                LandPhone: "1234567890",
                MobilePhone: "0987654321",
                JobDescription: "Developer",
                OfficeId: new Guid()
            );

            var csvContent = new StringBuilder();
            csvContent.AppendLine("Id;Email;FirstName;LastName;IsCustomer;IsActive;LandPhone;MobilePhone;JobDescription;OfficeId");
            csvContent.AppendLine($"" +
                $"{contact.Id};" +
                $"{contact.Email};" +
                $"{contact.FirstName};" +
                $"{contact.LastName};" +
                $"{contact.IsCustomer};" +
                $"{contact.IsActive};" +
                $"{contact.LandPhone};" +
                $"{contact.MobilePhone};" +
                $"{contact.JobDescription};" +
                $"{contact.OfficeId}");

            var stream = new MemoryStream(Encoding.GetEncoding("ISO-8859-1").GetBytes(csvContent.ToString()));
            IFormFile file = new FormFile(stream, 0, stream.Length, "id_from_form", "contacts.csv");


            var contactService = new Mock<IContactService>(MockBehavior.Strict);
            contactService.Setup(s => s.ProcessContactAsync(It.IsAny<IEnumerable<ContactCsv>>()))
                .Callback<IEnumerable<ContactCsv>>(data =>
                {
                    data.First().Should().NotBeNull();
                    data.First().Should().Be(contact);
                })
                .Returns(Task.CompletedTask);

            // Act
            var contactController = new ContactController(contactService.Object);
            var response = await contactController.UploadAsync(file) as ObjectResult;

            // Assert
            contactService.Verify(s => s.ProcessContactAsync(It.IsAny<IEnumerable<ContactCsv>>()), Times.Once());
            response!.StatusCode.Should().Be((int)HttpStatusCode.OK);
        }

        [Fact]
        public async Task ExportDataAsync_Returns_ExpectedFileResult()
        {
            // Arrange
            var expectedData = "{'Name': 'John', 'Age': 30}"; // Example JSON data
            var expectedFileName = "CreContacts.json";

            var contactServiceMock = new Mock<IContactService>();
            contactServiceMock.Setup(x => x.StreamContactsJsonAsync(It.IsAny<StreamWriter>()))
                              .Callback<StreamWriter>(async writer => await writer.WriteAsync(expectedData));

            var controller = new ContactController(contactServiceMock.Object);

            // Act
            var result = await controller.ExportDataAsync();

            // Assert
            result.Should().BeOfType<FileStreamResult>();
            var fileContentResult = result as FileStreamResult;
            contactServiceMock.Verify(x => x.StreamContactsJsonAsync(It.IsAny<StreamWriter>()), Times.Once);
            fileContentResult!.FileDownloadName.Should().Be(expectedFileName);
        }
    }
}
