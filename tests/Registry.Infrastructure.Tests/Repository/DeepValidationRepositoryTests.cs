using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Pulse.ContactRegistry.Domain.Context;
using Domain.Entities.Audits;
using Infrastructure.Repository;
using System;
using System.Threading.Tasks;
using AutoFixture;

namespace Infrastructure.Tests.Repository
{
    public class DeepValidationRepositoryTests
    {
        private readonly Fixture _fixture;
        private readonly ILogger<DeepValidationRepository> _logger;

        public DeepValidationRepositoryTests()
        {
            _fixture = new Fixture();
            _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
                .ForEach(b => _fixture.Behaviors.Remove(b));
            _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
            _logger = Mock.Of<ILogger<DeepValidationRepository>>();
        }

        private DbContextOptions<RefContext> GetDbOptions()
        {
            return new DbContextOptionsBuilder<RefContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
        }

        [Fact]
        public async Task AddDeepValidationAsync_ShouldReturnTrue_WhenValidationDoesNotExist()
        {
            // Arrange
            var deepValidation = _fixture.Create<DeepValidationEntity>();

            using (var context = new RefContext(GetDbOptions()))
            {
                var repository = new DeepValidationRepository(context, _logger);

                // Act
                var result = await repository.AddDeepValidationAsync(deepValidation);

                // Assert
                result.Should().BeTrue();
                var savedValidation = await context.Set<DeepValidationEntity>()
                    .FirstOrDefaultAsync(x => x.Id == deepValidation.Id);
                savedValidation.Should().NotBeNull();
                savedValidation.Should().BeEquivalentTo(deepValidation);
            }
        }

        [Fact]
        public async Task AddDeepValidationAsync_ShouldThrowArgumentException_WhenValidationAlreadyExists()
        {
            // Arrange
            var deepValidation = _fixture.Create<DeepValidationEntity>();

            using (var context = new RefContext(GetDbOptions()))
            {
                // Add the validation first
                context.Add(deepValidation);
                await context.SaveChangesAsync();

                var repository = new DeepValidationRepository(context, _logger);

                // Act
                var result = async () => await repository.AddDeepValidationAsync(deepValidation);

                // Assert
                await result.Should().ThrowAsync<ArgumentException>();
            }
        }



        [Fact]
        public async Task AddDeepValidationAsync_ShouldReturnFalse_WhenNullEntityProvided()
        {
            // Arrange
            DeepValidationEntity deepValidation = null;

            using (var context = new RefContext(GetDbOptions()))
            {
                var repository = new DeepValidationRepository(context, _logger);

                // Act
                var result = async () => await repository.AddDeepValidationAsync(deepValidation);

                // Assert
                await result.Should().ThrowAsync<ArgumentNullException>();
            }
        }
    }
}