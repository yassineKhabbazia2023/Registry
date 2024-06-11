using Application.Interfaces;
using Application.Models;
using Application.Services;
using Domain.Entities;
using FluentAssertions;
using Moq;
using System.Text.Json;
using CreRole = Application.Models.CreRole;

namespace ContactRegistry.Application.Tests.Services
{
    public class RoleServiceTest
    {
        [Fact]
        public async Task ProcessContactAsync_Adds_Account()
        {
            // Arrange
            var role = new RoleCsv(
                Id: Guid.NewGuid(),
                ContactId: Guid.NewGuid(),
                AccountId: Guid.NewGuid(),
                Onboarded: true
            );


            var roles = new List<RoleCsv>() { role };


            var expectedRoles = roles
                .Select(
                a => new AlxRole
                {
                    RoleId = a.Id,
                    AccountId = a.AccountId,
                    ContactId = a.ContactId,
                    Onboarded = a.Onboarded
                }).ToList();

            var roleRepository = new Mock<IRoleRepository>(MockBehavior.Strict);
            roleRepository.Setup(r => r.AddRolesAsync(It.IsAny<IEnumerable<AlxRole>>())).
                Callback<IEnumerable<AlxRole>>(data =>
                {
                    data.Should().BeEquivalentTo(expectedRoles);
                })
                .Returns(Task.CompletedTask);

            // Act
            var roleService = new RoleService(roleRepository.Object);
            await roleService.ProcessRoleAsync(roles);

            roleRepository.VerifyAll();
        }

        [Fact]
        public async Task StreamRolesJsonAsync_Writes_ExpectedData()
        {
            // Arrange
            var role1 = new Domain.Entities.CreRole
            {
                Id = Guid.Empty,
                AccountId = Guid.Empty,
                ContactId = Guid.Empty,
                Deleted = default
            };

            var expectedRole = new CreRole
            {
                ContactId = Guid.Empty,
                AccountId = Guid.Empty,
                Deleted = null,
                Id = Guid.Empty,
            };

            IEnumerable<Domain.Entities.CreRole> roles = new List<Domain.Entities.CreRole>() { role1 };
            IEnumerable<CreRole> expectedRoleList = new List<CreRole>() { expectedRole };

            var options = new JsonSerializerOptions { WriteIndented = true };
            var expectedJsonData = JsonSerializer.Serialize(expectedRoleList, options);

            var roleRepository = new Mock<IRoleRepository>();
            roleRepository.Setup(r => r.GetRolesAsync()).Returns(GetAsyncEnumerable(roles));

            var stream = new MemoryStream();
            var streamWriter = new StreamWriter(stream);

            var roleService = new RoleService(roleRepository.Object);

            // Act
            await roleService.StreamRolesJsonAsync(streamWriter);

            stream.Position = 0;
            var reader = new StreamReader(stream);
            var jsonData = await reader.ReadToEndAsync();

            // Assert
            roleRepository.Verify(c => c.GetRolesAsync(), Times.Once);
            jsonData.Should().BeEquivalentTo(expectedJsonData);
        }

        private async IAsyncEnumerable<Domain.Entities.CreRole> GetAsyncEnumerable(IEnumerable<Domain.Entities.CreRole> roles)
        {
            foreach (var role in roles)
            {
                yield return role;
            }
        }
    }
}
