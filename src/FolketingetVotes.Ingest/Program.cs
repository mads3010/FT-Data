using FolketingetVotes.Data;
using Microsoft.Extensions.Configuration;
using FolketingetVotes.Ingest;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// Content root = the binaries folder so appsettings.json is found no matter where the tool is invoked from.
var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { Args = args, ContentRootPath = AppContext.BaseDirectory });
builder.Logging.AddSimpleConsole(o =>
{
    o.SingleLine = true;
    o.TimestampFormat = "HH:mm:ss ";
});
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
builder.Logging.AddFilter("System.Net.Http", LogLevel.Warning);
builder.Logging.AddFilter("Polly", LogLevel.Warning);

var connectionString = builder.Configuration.GetConnectionString("Folketinget")
    ?? throw new InvalidOperationException("Connection string 'Folketinget' is not configured.");
builder.Services.AddFolketingetData(connectionString);
builder.Services.AddFolketingetIngestion(builder.Configuration);
builder.Services.AddSingleton<CommandRunner>();

using var host = builder.Build();
return await host.Services.GetRequiredService<CommandRunner>().RunAsync(args);
