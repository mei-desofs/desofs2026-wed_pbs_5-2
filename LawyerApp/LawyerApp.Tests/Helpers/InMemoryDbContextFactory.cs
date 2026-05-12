using LawyerApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LawyerApp.Tests.Helpers;

public static class InMemoryDbContextFactory
{
    public static LawyerAppDbContext Create(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<LawyerAppDbContext>()
            .UseInMemoryDatabase(dbName ?? $"TestDb_{Guid.NewGuid()}")
            .Options;

        var context = new LawyerAppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}
