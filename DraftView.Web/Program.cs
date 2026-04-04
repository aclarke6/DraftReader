using Microsoft.AspNetCore.HttpOverrides;
using DraftView.Domain.Enumerations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DraftView.Application.Services;
using DraftView.Domain.Interfaces.Repositories;
using DraftView.Domain.Interfaces.Services;
using DraftView.Infrastructure.Dropbox;
using DraftView.Infrastructure.Parsing;
using DraftView.Infrastructure.Sync;
using DraftView.Infrastructure.Persistence;
using DraftView.Infrastructure.Persistence.Repositories;
using DraftView.Web;
using DraftView.Web.Extensions;
using DraftView.Web.Data;
using DraftView.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Configuration
// ---------------------------------------------------------------------------
// Configure settings (Options pattern + singletons for legacy resolution)
builder.Services.AddConfiguredSettings(builder.Configuration);

// Note: avoid calling BuildServiceProvider here (creates duplicate singletons).
// Read configuration values directly for decisions that must be made at startup
// and resolve services from the provider when registering services that need them.

// ---------------------------------------------------------------------------
// Persistence (database + repositories)
// ---------------------------------------------------------------------------
builder.Services.AddPersistenceServices(builder.Configuration);

// ---------------------------------------------------------------------------
// ASP.NET Core Identity
// ---------------------------------------------------------------------------
builder.Services.AddIdentityServices();

// ---------------------------------------------------------------------------
// MVC
// ---------------------------------------------------------------------------
builder.Services.AddControllersWithViews();

// ---------------------------------------------------------------------------
// Session (required for OAuth state)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(10);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// (moved to AddPersistenceServices extension)

// ---------------------------------------------------------------------------
// Application services
// ---------------------------------------------------------------------------
builder.Services.AddApplicationServices();

// ---------------------------------------------------------------------------
// Parsing and Dropbox
// ---------------------------------------------------------------------------
builder.Services.AddSingleton<IScrivenerProjectParser, ScrivenerProjectParser>();
builder.Services.AddSingleton<IRtfConverter, RtfConverter>();
builder.Services.AddScoped<IDropboxConnectionChecker, DropboxConnectionChecker>();
builder.Services.AddScoped<IDropboxClientFactory, DropboxClientFactory>();
builder.Services.AddScoped<IDropboxFileDownloader, DropboxFileDownloader>();

// ---------------------------------------------------------------------------
// Email sender
// ---------------------------------------------------------------------------
var emailProvider = builder.Configuration["Email:Provider"] ?? string.Empty;
if (emailProvider == "Console")
    builder.Services.AddScoped<IEmailSender, ConsoleEmailSender>();
else
    builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();

// ---------------------------------------------------------------------------
// Path resolver
// ---------------------------------------------------------------------------
// Resolve DraftViewSettings from DI so we don't need to build a service provider here.
builder.Services.AddScoped<ILocalPathResolver>(sp =>
{
    var settings = sp.GetRequiredService<DraftViewSettings>();
    return new LocalPathResolver(settings.ResolvedLocalCachePath);
});

// ---------------------------------------------------------------------------
// Background sync service
// ---------------------------------------------------------------------------
builder.Services.AddHostedService<SyncBackgroundService>();

// ---------------------------------------------------------------------------
// Build and configure pipeline
// ---------------------------------------------------------------------------
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DraftViewDbContext>();
    db.Database.Migrate();
}

var seedEmail = builder.Configuration["Seed:AuthorEmail"] ?? "author@draftview.local";
var seedPassword = builder.Configuration["Seed:AuthorPassword"] ?? "Password1!";
var seedName = builder.Configuration["Seed:AuthorName"] ?? "Author";
var seedPath = builder.Configuration["Seed:TestProjectPath"] ?? "/Apps/Scrivener/Test.scriv";
var supportEmail = builder.Configuration["Seed:SupportEmail"] ?? "support@draftview.co.uk";
var supportPassword = builder.Configuration["Seed:SupportPassword"] ?? "Password1!";
var supportDisplayName = builder.Configuration["Seed:SupportName"] ?? "DraftView Support";

await DatabaseSeeder.SeedAsync(
    app.Services,
    seedEmail,
    seedPassword,
    seedName,
    seedPath,
    supportEmail,
    supportPassword,
    supportDisplayName);

// Reset any projects stuck in Syncing state from a previous crashed sync
using (var startupScope = app.Services.CreateScope())
{
    var db = startupScope.ServiceProvider.GetRequiredService<DraftViewDbContext>();
    var stuckProjects = db.Projects
        .Where(p => p.SyncStatus == SyncStatus.Syncing)
        .ToList();
    foreach (var p in stuckProjects)
        p.UpdateSyncStatus(SyncStatus.Stale, DateTime.UtcNow, null);
    if (stuckProjects.Any())
        await db.SaveChangesAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseForwardedHeaders(new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto });
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();





