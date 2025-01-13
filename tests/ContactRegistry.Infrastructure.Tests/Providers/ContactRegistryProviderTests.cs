using AutoFixture;
using Infrastructure.Providers;
using Moq;
using Npgsql.EntityFrameworkCore.PostgreSQL.Query.ExpressionTranslators.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContactRegistry.Infrastructure.Tests.Providers
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
                .Build<Application.Models.ContactRegistry>()
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
                .Build<Application.Models.ContactRegistry>()
                .With(x => x.ContactFlagStatus, 1)
                .Create();
            await contactRegistryPRovider.UpdateContactAsync(contactRegistry);

            _httpClientFactory.Verify(x => x.CreateClient("RegistryApi"), Times.Once);
        }
    }
}
