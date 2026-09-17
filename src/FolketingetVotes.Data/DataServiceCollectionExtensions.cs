using FolketingetVotes.Core.Abstractions;
using FolketingetVotes.Core.Queries;
using FolketingetVotes.Data.Oda;
using FolketingetVotes.Data.PartyAccounts;
using FolketingetVotes.Data.Persistence;
using FolketingetVotes.Data.Queries;
using FolketingetVotes.Data.Stats;
using FolketingetVotes.Data.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

namespace FolketingetVotes.Data;

public static class DataServiceCollectionExtensions
{
    /// <summary>Registers the database and the read-side query services used by the web app.</summary>
    public static IServiceCollection AddFolketingetData(this IServiceCollection services, string connectionString)
    {
        services.AddPooledDbContextFactory<FolketingetDbContext>(options => ConfigureDbContext(options, connectionString));
        services.AddScoped(sp => sp.GetRequiredService<IDbContextFactory<FolketingetDbContext>>().CreateDbContext());

        services.AddScoped<IVoteQueries, VoteQueries>();
        services.AddScoped<IPoliticianQueries, PoliticianQueries>();
        services.AddScoped<IPartyQueries, PartyQueries>();
        services.AddScoped<ICaseQueries, CaseQueries>();
        services.AddScoped<ISiteQueries, SiteQueries>();
        services.AddScoped<ITopicQueries, TopicQueries>();
        services.AddScoped<ISessionQueries, SessionQueries>();
        services.AddScoped<IDonorQueries, DonorQueries>();
        services.AddScoped<IComparisonQueries, ComparisonQueries>();
        services.AddScoped<IQuestionQueries, QuestionQueries>();
        services.AddScoped<ICompositionQueries, CompositionQueries>();
        services.AddScoped<IDataQualityQueries, DataQualityQueries>();
        services.AddScoped<ISearchQueries, SearchQueries>();
        services.AddScoped<IExplorerQueries, ExplorerQueries>();

        // v1 ships without generated summaries; replace this registration to enable them (docs/roadmap.md).
        services.AddSingleton<ISummaryProvider, NullSummaryProvider>();
        return services;
    }

    /// <summary>Registers the oda.ft.dk client, synchronisation, statistics refresh and party-account import.</summary>
    public static IServiceCollection AddFolketingetIngestion(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        services.Configure<OdaOptions>(configuration.GetSection(OdaOptions.SectionName));

        services.AddHttpClient<OdaClient>(http =>
            {
                http.Timeout = TimeSpan.FromMinutes(5);
                http.DefaultRequestHeaders.UserAgent.ParseAdd("folketinget-votes/1.0 (+https://github.com/mads3010/folketinget-votes)");
                http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            })
            .AddStandardResilienceHandler(options =>
            {
                options.Retry.MaxRetryAttempts = 6;
                options.Retry.Delay = TimeSpan.FromSeconds(2);
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(90);
                options.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(5);
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromMinutes(3);
            });

        services.AddSingleton<IEntitySync, LookupSync>();
        services.AddSingleton<IEntitySync, PeriodSync>();
        services.AddSingleton<IEntitySync, ActorSync>();
        services.AddSingleton<IEntitySync, ActorRelationSync>();
        services.AddSingleton<IEntitySync, MeetingSync>();
        services.AddSingleton<IEntitySync, CaseSync>();
        services.AddSingleton<IEntitySync, CaseStepSync>();
        services.AddSingleton<IEntitySync, KeywordSync>();
        services.AddSingleton<IEntitySync, CaseKeywordSync>();
        services.AddSingleton<IEntitySync, CaseActorSync>();
        services.AddSingleton<IEntitySync, VoteSync>();
        services.AddSingleton<IEntitySync, BallotSync>();
        services.AddSingleton<SyncOrchestrator>();
        services.AddSingleton<StatsRefresher>();
        services.AddSingleton<PartyAccountImporter>();
        return services;
    }

    internal static void ConfigureDbContext(DbContextOptionsBuilder options, string connectionString)
    {
        options.UseNpgsql(connectionString, npgsql => npgsql.CommandTimeout(300))
            .UseSnakeCaseNamingConvention();
    }
}
