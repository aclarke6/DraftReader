using DraftReader.Domain.Enumerations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DraftReader.Application.Services;
using DraftReader.Domain.Interfaces.Repositories;
using DraftReader.Domain.Interfaces.Services;
using DraftReader.Infrastructure.Dropbox;
using DraftReader.Infrastructure.Parsing;
using DraftReader.Infrastructure.Sync;
using DraftReader.Infrastructure.Persistence;
using DraftReader.Infrastructure.Persistence.Repositories;
using DraftReader.Web;
using DraftReader.Web.Data;
using DraftReader.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Configuration
// ---------------------------------------------------------------------------
var scrivenerSettings = new DraftReaderSettings();
builder.Configuration.GetSection("DraftReader").Bind(scrivenerSettings);
builder.Services.AddSingleton(scrivenerSettings);

var emailSettings = new EmailSettings();
builder.Configuration.GetSection("Email").Bind(emailSettings);
builder.Services.AddSingleton(emailSettings);

var dropboxSettings = new DropboxClientSettings();
builder.Configuration.GetSection("Dropbox").Bind(dropboxSettings);
builder.Services.AddSingleton(dropboxSettings);

// ---------------------------------------------------------------------------
// Database
// ---------------------------------------------------------------------------

builder.Services.AddDbContext<DraftReaderDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ---------------------------------------------------------------------------
// ASP.NET Core Identity
// ---------------------------------------------------------------------------
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequireDigit           = true;
    options.Password.RequiredLength         = 8;
    options.Password.RequireUppercase       = false;
    options.Password.RequireNonAlphanumeric = false;
    options.SignIn.RequireConfirmedEmail     = false;
})
.AddEntityFrameworkStores<DraftReaderDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath         = "/Account/Login";
    options.LogoutPath        = "/Account/Logout";
    options.AccessDeniedPath  = "/Account/AccessDenied";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan    = TimeSpan.FromDays(14);
});

// ---------------------------------------------------------------------------
// MVC
// ---------------------------------------------------------------------------
builder.Services.AddControllersWithViews();

// ---------------------------------------------------------------------------
// Repositories
// ---------------------------------------------------------------------------
builder.Services.AddScoped<IUnitOfWork>(sp =>
    sp.GetRequiredService<DraftReaderDbContext>());
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IInvitationRepository, InvitationRepository>();
builder.Services.AddScoped<IScrivenerProjectRepository, ScrivenerProjectRepository>();
builder.Services.AddScoped<ISectionRepository, SectionRepository>();
builder.Services.AddScoped<ICommentRepository, CommentRepository>();
builder.Services.AddScoped<IReadEventRepository, ReadEventRepository>();
builder.Services.AddScoped<IUserNotificationPreferencesRepository, UserNotificationPreferencesRepository>();
builder.Services.AddScoped<IEmailDeliveryLogRepository, EmailDeliveryLogRepository>();

// ---------------------------------------------------------------------------
// Application services
// ---------------------------------------------------------------------------
builder.Services.AddScoped<ISyncService, SyncService>();
builder.Services.AddScoped<IPublicationService, PublicationService>();
builder.Services.AddSingleton<ISyncProgressTracker, SyncProgressTracker>();
builder.Services.AddScoped<IScrivenerProjectDiscoveryService, ScrivenerProjectDiscoveryService>();
builder.Services.AddSingleton(new DiscoveryServiceOptions
{
    LocalCachePath = builder.Configuration["Dropbox:LocalCachePath"] ?? string.Empty
});
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IReadingProgressService, ReadingProgressService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

// ---------------------------------------------------------------------------
// Parsing and Dropbox
// ---------------------------------------------------------------------------
builder.Services.AddSingleton<IScrivenerProjectParser, ScrivenerProjectParser>();
builder.Services.AddSingleton<IRtfConverter, RtfConverter>();

if (!string.IsNullOrWhiteSpace(dropboxSettings.AccessToken))
{
    builder.Services.AddSingleton<IDropboxClient>(
        new DraftReader.Infrastructure.Dropbox.DropboxClient(dropboxSettings));
}

// ---------------------------------------------------------------------------
// Email sender
// ---------------------------------------------------------------------------
if (emailSettings.Provider == "Console")
    builder.Services.AddScoped<IEmailSender, ConsoleEmailSender>();
else
    builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();

// ---------------------------------------------------------------------------
// Path resolver
// ---------------------------------------------------------------------------
builder.Services.AddSingleton<IDropboxConnectionChecker, DropboxConnectionChecker>();
builder.Services.AddScoped<ILocalPathResolver>(_ =>
    new LocalPathResolver(scrivenerSettings.ResolvedLocalCachePath));

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
    var db = scope.ServiceProvider.GetRequiredService<DraftReaderDbContext>();
    db.Database.Migrate();
}

// Seed initial data
var seedEmail    = builder.Configuration["Seed:AuthorEmail"]    ?? "author@scrivener-sync.local";
var seedPassword = builder.Configuration["Seed:AuthorPassword"] ?? "Password1!";
var seedName     = builder.Configuration["Seed:AuthorName"]     ?? "Author";
var seedPath     = builder.Configuration["Seed:TestProjectPath"] ?? "/Apps/Scrivener/Test.scriv";

await DatabaseSeeder.SeedAsync(app.Services, seedEmail, seedPassword, seedName, seedPath);

// Reset any projects stuck in Syncing state from a previous crashed sync
using (var startupScope = app.Services.CreateScope())
{
    var db = startupScope.ServiceProvider.GetRequiredService<DraftReaderDbContext>();
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

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();










