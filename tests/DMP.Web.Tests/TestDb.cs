using DMP.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace DMP.Web.Tests;

/// <summary>Builds an isolated in-memory ApplicationDbContext per test.</summary>
public static class TestDb
{
    public static ApplicationDbContext Create()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .EnableSensitiveDataLogging()
            .Options;
        return new ApplicationDbContext(options);
    }
}
