using Fruitables.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Fruitables.Tests;

public static class TestDbContextFactory
{
    // Tạo DbContextOptions SQLite in-memory. Truyền interceptor để bắt SQL cho guardrail N+1.
    public static DbContextOptions<ApplicationDbContext> CreateSqliteOptions(
        params DbCommandInterceptor[] interceptors)
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var builder = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));

        if (interceptors != null)
        {
            builder.AddInterceptors(interceptors);
        }

        var options = builder.Options;

        using var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();

        return options;
    }


    public static DbContextOptions<ApplicationDbContext> CreateInMemoryOptions()
    {
        return new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }
}
