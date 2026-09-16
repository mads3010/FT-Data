using FolketingetVotes.Data.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace FolketingetVotes.Data.Tests;

/// <summary>
/// A PostgreSQL with the real migrations applied: the database named by <c>FOLKETINGET_TEST_CONNECTION</c>
/// when that variable is set (it is wiped by the tests), otherwise a throw-away Docker container.
/// Tests that need it are skipped (not failed) when neither is available.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private string? _connectionString;

    public bool IsAvailable { get; private set; }

    public string ConnectionString => _connectionString ?? throw new InvalidOperationException("Postgres is not running.");

    public async Task InitializeAsync()
    {
        try
        {
            var external = Environment.GetEnvironmentVariable("FOLKETINGET_TEST_CONNECTION");
            if (!string.IsNullOrWhiteSpace(external))
            {
                _connectionString = external;
            }
            else
            {
                _container = new PostgreSqlBuilder("postgres:17-alpine").Build();
                await _container.StartAsync();
                _connectionString = _container.GetConnectionString();
            }

            await using var db = CreateContext();
            await db.Database.MigrateAsync();
            IsAvailable = true;
        }
        catch (Exception)
        {
            IsAvailable = false;
        }
    }

    public FolketingetDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<FolketingetDbContext>();
        DataServiceCollectionExtensions.ConfigureDbContext(options, ConnectionString);
        return new FolketingetDbContext(options.Options);
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "postgres";
}
