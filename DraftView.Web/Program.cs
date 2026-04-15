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

EnsureTestingEmailProtectionKeys(builder);

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
// Web UI, session, parsing, Dropbox and background services
// ---------------------------------------------------------------------------
builder.Services.AddWebServices(builder.Configuration);

// ---------------------------------------------------------------------------
// Application services
// ---------------------------------------------------------------------------
builder.Services.AddApplicationServices(builder.Configuration);

// (Parsing, Dropbox, email sender, path resolver and background services
// are registered by AddWebServices above to keep Program.cs concise.)

// ---------------------------------------------------------------------------
// Build and configure pipeline
// ---------------------------------------------------------------------------
var app = builder.Build();

// Keep startup side effects out of WebApplicationFactory-based tests.
// Tests should boot the real app in the "Testing" environment and take
// ownership of database setup so login, rendering, and auth scenarios can be
// arranged deterministically.
if (!app.Environment.IsEnvironment("Testing"))
{
    await app.MigrateDatabaseAsync();
    await app.SeedDatabaseAsync();
    await app.ResetStaleSyncProjectsAsync();
}

app.UseExceptionHandler("/Home/Error");

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/Home/NotFoundPage");

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

static void EnsureTestingEmailProtectionKeys(WebApplicationBuilder builder)
{
    if (!builder.Environment.IsEnvironment("Testing"))
        return;

    const string encryptionKeyPath = "EmailProtection:EncryptionKey";
    const string lookupHmacKeyPath = "EmailProtection:LookupHmacKey";

    var hasEncryptionKey = !string.IsNullOrWhiteSpace(builder.Configuration[encryptionKeyPath]);
    var hasLookupHmacKey = !string.IsNullOrWhiteSpace(builder.Configuration[lookupHmacKeyPath]);

    if (hasEncryptionKey && hasLookupHmacKey)
        return;

    builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
    {
        [encryptionKeyPath] = "MDEyMzQ1Njc4OUFCQ0RFRjAxMjM0NTY3ODlBQkNERUY=",
        [lookupHmacKeyPath] = "RkVEQ0JBOTg3NjU0MzIxMEZFRENCQTk4NzY1NDMyMTA="
    });
}

// Expose Program so WebApplicationFactory<Program> can host the real app in tests.
public partial class Program;





