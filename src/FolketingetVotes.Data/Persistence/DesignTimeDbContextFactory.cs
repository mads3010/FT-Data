using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FolketingetVotes.Data.Persistence;

/// <summary>Lets <c>dotnet ef</c> build the context without the host; the connection string is only used for migrations tooling.</summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<FolketingetDbContext>
{
    public FolketingetDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("FOLKETINGET_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=folketinget;Username=folketinget;Password=folketinget";
        var options = new DbContextOptionsBuilder<FolketingetDbContext>();
        DataServiceCollectionExtensions.ConfigureDbContext(options, connectionString);
        return new FolketingetDbContext(options.Options);
    }
}
