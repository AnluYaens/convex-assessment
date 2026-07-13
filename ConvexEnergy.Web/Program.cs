using ConvexEnergy.Web;
using ConvexEnergy.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Static web assets (framework JS, scoped CSS) are only auto-enabled in the
// Development environment when running from source. We run as Production with
// no launchSettings, so load the assets manifest explicitly. Safe when published.
builder.WebHost.UseStaticWebAssets();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpClient<DayAheadApiClient>(c =>
    c.BaseAddress = new Uri(builder.Configuration["Api:BaseUrl"]
        ?? throw new InvalidOperationException("Api:BaseUrl is not configured")));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler("/Error", createScopeForErrors: true);

app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();