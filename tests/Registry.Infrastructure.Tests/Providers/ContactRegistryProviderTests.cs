// <copyright file="ContactRegistryProviderTests.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using AutoFixture;
using Application.Providers;
using Moq;
using Application.Models;

namespace Registry.Infrastructure.Tests.Providers
{
    public class ContactRegistryProviderTests
    {
        private readonly Mock<IHttpClientFactory> _httpClientFactory;
        private Fixture fixture;
        public ContactRegistryProviderTests()
        {
            _httpClientFactory = new Mock<IHttpClientFactory>();
            fixture = new Fixture();
        }

        [Fact]
        public async Task CreateContactAsync_Should_Generate_Factory()
        {

            var contactRegistryPRovider = new ContactRegistryProvider(_httpClientFactory.Object);
            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("https://www.tests.com");


            _httpClientFactory.Setup(x => x.CreateClient("RegistryApi")).Returns(httpClient);

            var contactRegistry = fixture
                .Build<ContactRegistry>()
                .With(x => x.ContactFlagStatus, 1)
                .Create();
            await contactRegistryPRovider.CreateContactAsync(contactRegistry);

            _httpClientFactory.Verify(x => x.CreateClient("RegistryApi"), Times.Once);
        }

        [Fact]
        public async Task UpdateContactAsync_Should_Generate_Factory()
        {

            var contactRegistryPRovider = new ContactRegistryProvider(_httpClientFactory.Object);
            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("https://www.tests.com");


            _httpClientFactory.Setup(x => x.CreateClient("RegistryApi")).Returns(httpClient);

            var contactRegistry = fixture
                .Build<ContactRegistry>()
                .With(x => x.ContactFlagStatus, 1)
                .Create();
            await contactRegistryPRovider.UpdateContactAsync(contactRegistry);

            _httpClientFactory.Verify(x => x.CreateClient("RegistryApi"), Times.Once);
        }
    }
}
