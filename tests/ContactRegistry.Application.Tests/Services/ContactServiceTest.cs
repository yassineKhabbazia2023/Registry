using Application.Interfaces;
using Application.Models;
using Application.Services;
using Domain.Entities;
using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ContactRegistry.Application.Tests.Services
{
    public class ContactServiceTest
    {
        [Fact]
        public async Task ProcessContactAsync_Adds_Account()
        {
            // Arrange
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


            var contacts = new List<ContactCsv>() { contact };

            var expectedContacts = contacts
                .Select(
                    a => new AlxContact
                    {
                        Id = a.Id,
                        Email = a.Email,
                        FirstName = a.FirstName,
                        LastName = a.LastName,
                        IsActive = a.IsActive,
                        IsCustomer = a.IsCustomer,
                        MobilePhone = a.MobilePhone,
                        LandPhone = a.LandPhone,
                        JobDescription = a.JobDescription,
                        OfficeId = a.OfficeId,
                    }).ToList();

            var contacttRepository = new Mock<IContactRepository>(MockBehavior.Strict);
            contacttRepository.Setup(r => r.AddContactsAsync(It.IsAny<IEnumerable<AlxContact>>())).
                Callback<IEnumerable<AlxContact>>(data =>
                {
                    data.Should().BeEquivalentTo(expectedContacts);
                })
                .Returns(Task.CompletedTask);

            var processDeltaTriggerRepositoryMock = new Mock<IProcessDeltaTriggerRepository>(MockBehavior.Strict);
            processDeltaTriggerRepositoryMock.Setup(p => p.UpdateContactProcessAsync(true)).Returns(Task.CompletedTask);

            // Act
            var contactService = new ContactService(contacttRepository.Object, processDeltaTriggerRepositoryMock.Object);
            await contactService.ProcessContactAsync(contacts);

            contacttRepository.VerifyAll();
        }

        [Fact]
        public async Task StreamContactsJsonAsync_Writes_ExpectedData()
        {
            // Arrange
            var contact1 = new Domain.Entities.CreContact
            {
                Id = new Guid("ce0b7b12-a2ba-48ea-8014-92e9657a5bd5"),
                OfficeId = new Guid("72ce225c-c8b7-49eb-9e73-f641fce67811"),
                IsCustomer = true,
                IsActive = true,
                Updated = DateTime.UtcNow,
                Deleted = null,
                FirstName = "Test Contact 1",
                LastName = "Last Name 1",
                Email = "test1@example.com",
                LandPhone = "123-456-7890",
                MobilePhone = "987-654-3210",
                JobDescription = "Job Description 1",
                Source = "Source 1",
                Roles = new List<Domain.Entities.CreRole>
                {
                    new Domain.Entities.CreRole { RoleId = new Guid("36029043-76eb-4bbf-a452-8f53ec6c94e9"), AccountId = new Guid("ff05e5c7-22b1-4366-9a67-aaa51d6742a0")},
                }
            };

            IEnumerable<Domain.Entities.CreContact> contacts = new List<Domain.Entities.CreContact> { contact1 };

            var options = new JsonSerializerOptions { WriteIndented = true };
            var expectedJsonData = JsonSerializer.Serialize(contacts, options);

            var contactRepository = new Mock<IContactRepository>();
            contactRepository.Setup(r => r.GetContactsAsync()).Returns(GetAsyncEnumerable(contacts));

            var processDeltaTriggerRepositoryMock = new Mock<IProcessDeltaTriggerRepository>(MockBehavior.Strict);

            var stream = new MemoryStream();
            var streamWriter = new StreamWriter(stream);

            var contactService = new ContactService(contactRepository.Object, processDeltaTriggerRepositoryMock.Object);

            // Act
            await contactService.StreamContactsJsonAsync(streamWriter);

            stream.Position = 0;
            var reader = new StreamReader(stream);
            var jsonData = await reader.ReadToEndAsync();

            // Assert
            contactRepository.Verify(c => c.GetContactsAsync(), Times.Once);
        }

        private async IAsyncEnumerable<Domain.Entities.CreContact> GetAsyncEnumerable(IEnumerable<Domain.Entities.CreContact> contacts)
        {
            foreach (var contact in contacts)
            {
                yield return contact;
            }
        }
    }
}
