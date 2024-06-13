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
    public class RoleControllerTest
    {
        [Fact]
        public async Task UploadAsync_InvalidFile_Returns_BadRequest()
        {
            // Arrange
            var fileName = "data.csv";
            var stream = new MemoryStream(new byte[0]);
            IFormFile file = new FormFile(stream, 0, stream.Length, "id_from_form", fileName);

            var roleService = new Mock<IRoleService>();

            // Act
            var roleController = new RoleController(roleService.Object);
            var response = await roleController.UploadAsync(file) as ObjectResult;

            // Assert
            roleService.Verify(s => s.ProcessRoleAsync(It.IsAny<IEnumerable<RoleCsv>>()), Times.Never());
            response!.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task UploadAsync_ValidFile_Returns_Ok()
        {
            var role = new RoleCsv()
            {
                RoleId = Guid.NewGuid(),
                ContactId = Guid.NewGuid(),
                AccountId = Guid.NewGuid(),
                Onboarded = true,
                IsFavorite = true,
                RoleDelegataireEmail = "delegataire@email.fr",
                RoleSignatory = true
            };

            var csvContent = new StringBuilder();
            csvContent.AppendLine("RoleId;ContactId;AccountId;Onboarded;IsFavorite;RoleDelegataireEmail;RoleSignatory");
            csvContent.AppendLine($"" +
                $"{role.RoleId};" +
                $"{role.ContactId};" +
                $"{role.AccountId};" +
                $"{role.Onboarded};" +
                $"{role.IsFavorite};" +
                $"{role.RoleDelegataireEmail};" +
                $"{role.RoleSignatory}");

            var stream = new MemoryStream(Encoding.GetEncoding("ISO-8859-1").GetBytes(csvContent.ToString()));
            IFormFile file = new FormFile(stream, 0, stream.Length, "id_from_form", "roles.csv");

            var roleService = new Mock<IRoleService>(MockBehavior.Strict);
            roleService.Setup(s => s.ProcessRoleAsync(It.IsAny<IEnumerable<RoleCsv>>()))
                .Callback<IEnumerable<RoleCsv>>(data =>
                {
                    data.First().Should().NotBeNull();
                    data.First().Should().BeEquivalentTo(role);
                })
                .Returns(Task.CompletedTask);

            // Act
            var roleController = new RoleController(roleService.Object);
            var response = await roleController.UploadAsync(file) as ObjectResult;

            // Assert
            roleService.Verify(s => s.ProcessRoleAsync(It.IsAny<IEnumerable<RoleCsv>>()), Times.Once());
            response!.StatusCode.Should().Be((int)HttpStatusCode.OK);
        }

        [Fact]
        public async Task ExportDataAsync_Returns_ExpectedFileResult()
        {
            // Arrange
            var expectedData = "{'Name': 'John', 'Age': 30}"; // Example JSON data
            var expectedFileName = "CreRoles.json";

            var roleServiceMock = new Mock<IRoleService>();
            roleServiceMock.Setup(x => x.StreamRolesJsonAsync(It.IsAny<StreamWriter>()))
                              .Callback<StreamWriter>(async writer => await writer.WriteAsync(expectedData));

            var controller = new RoleController(roleServiceMock.Object);

            // Act
            var result = await controller.ExportDataAsync();

            // Assert
            result.Should().BeOfType<FileStreamResult>();
            var fileContentResult = result as FileStreamResult;
            roleServiceMock.Verify(x => x.StreamRolesJsonAsync(It.IsAny<StreamWriter>()), Times.Once);
            fileContentResult!.FileDownloadName.Should().Be(expectedFileName);
        }
    }
}
