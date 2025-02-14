// <copyright file="RoleRegistryProviderTests.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using AutoFixture;
using Application.Providers;
using Moq;
using Application.Models;

namespace Registry.Infrastructure.Tests.Providers
{
    public class RoleRegistryProviderTests
    {
        private readonly Mock<IHttpClientFactory> _httpClientFactory;
        private Fixture fixture;
        public RoleRegistryProviderTests()
        {
            _httpClientFactory = new Mock<IHttpClientFactory>();
            fixture = new Fixture();
        }

        [Fact]
        public async Task CreateRoleAsync_Should_Generate_Factory()
        {

            var registryProvider = new RoleRegistryProvider(_httpClientFactory.Object);
            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("https://www.tests.com");


            _httpClientFactory.Setup(x => x.CreateClient("RegistryApi")).Returns(httpClient);

            var roleRegistry = fixture
                .Build<RoleRegistry>()
                .With(x => x.RoleFlagStatus, 1)
                .Create();
            await registryProvider.CreateRoleAsync(roleRegistry);

            _httpClientFactory.Verify(x => x.CreateClient("RegistryApi"), Times.Once);
        }

        [Fact]
        public async Task UpdateRoleAsync_Should_Generate_Factory()
        {

            var registryProvider = new RoleRegistryProvider(_httpClientFactory.Object);
            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("https://www.tests.com");


            _httpClientFactory.Setup(x => x.CreateClient("RegistryApi")).Returns(httpClient);

            var roleRegistry = fixture
                .Build<RoleRegistry>()
                .With(x => x.RoleFlagStatus, 1)
                .Create();
            await registryProvider.UpdateRoleAsync(roleRegistry);

            _httpClientFactory.Verify(x => x.CreateClient("RegistryApi"), Times.Once);
        }
    }
}
