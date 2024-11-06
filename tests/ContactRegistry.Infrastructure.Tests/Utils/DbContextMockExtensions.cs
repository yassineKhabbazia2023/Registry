using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pulse.ContactRegistry.Infrastructure.Context;

namespace ContactRegistry.Infrastructure.Tests.Utils
{
    public static class DbContextMockExtensions
    {
        public static ApplicationDbContext CreateInMemoryDbContext()
        {
            var serviceProvider = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("InMemoryAppDb")
                .UseInternalServiceProvider(serviceProvider)
                .Options;

            var dbContext = new ApplicationDbContext(options);
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            return dbContext;
        }

        public static RefContext CreateInMemoryRefContext()
        {
            var serviceProvider = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();

            var options = new DbContextOptionsBuilder<RefContext>()
                .UseInMemoryDatabase("InMemoryAppDb")
                .UseInternalServiceProvider(serviceProvider)
                .Options;

            var dbContext = new RefContext(options);
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            return dbContext;
        }
    }
}
