using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using System.Text.Json.Serialization;
using FolketingetVotes.Data;
using FolketingetVotes.Web.Api;
using FolketingetVotes.Web.Components;

var culture = CultureInfo.GetCultureInfo("da-DK");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Folketinget")
    ?? throw new InvalidOperationException("Connection string 'Folketinget' is not configured.");
builder.Services.AddFolketingetData(connectionString);
builder.Services.AddRazorComponents();
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddHealthChecks();
// Emit æ/ø/å as characters, not numeric entities (smaller pages, readable source). Blazor resolves HtmlEncoder from DI.
builder.Services.AddSingleton(HtmlEncoder.Create(UnicodeRanges.All));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/fejl", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapHealthChecks("/health");
app.MapFolketingetApi();
app.MapRazorComponents<App>();

app.Run();

/// <summary>Marker for WebApplicationFactory in tests.</summary>
public partial class Program;
