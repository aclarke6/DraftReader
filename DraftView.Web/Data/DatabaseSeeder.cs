using Microsoft.AspNetCore.Identity;
using DraftView.Domain.Entities;
using DraftView.Domain.Enumerations;
using DraftView.Infrastructure.Dropbox;
using DraftView.Infrastructure.Persistence;

namespace DraftView.Web.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(
        IServiceProvider services,
        string authorEmail,
        string authorPassword,
        string authorDisplayName,
        string scrivTestProjectDropboxPath)
    {
        using var scope        = services.CreateScope();
        var db                 = scope.ServiceProvider.GetRequiredService<DraftViewDbContext>();
        var userManager        = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var logger             = scope.ServiceProvider.GetRequiredService<ILogger<DraftViewDbContext>>();
        var dropboxSettings    = scope.ServiceProvider.GetRequiredService<DropboxClientSettings>();

        // ---------------------------------------------------------------------------
        // Seed Author IdentityUser (for login)
        // ---------------------------------------------------------------------------
        var existingIdentityUser = await userManager.FindByEmailAsync(authorEmail);
        if (existingIdentityUser is null)
        {
            var identityUser = new IdentityUser
            {
                UserName       = authorEmail,
                Email          = authorEmail,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(identityUser, authorPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to create author Identity user: {errors}");
            }

            logger.LogInformation("Author Identity user created: {Email}", authorEmail);
        }

        // ---------------------------------------------------------------------------
        // Seed Author domain User
        // ---------------------------------------------------------------------------
        var existingDomainUser = db.AppUsers.FirstOrDefault(u => u.Email == authorEmail);
        if (existingDomainUser is null)
        {
            var author = User.Create(authorEmail, authorDisplayName, Role.Author);
            author.Activate();
            db.AppUsers.Add(author);

            var prefs = UserNotificationPreferences.CreateForAuthor(
                author.Id, AuthorDigestMode.Immediate, null, "Europe/London");
            db.NotificationPreferences.Add(prefs);

            await db.SaveChangesAsync();
            logger.LogInformation("Author domain user created: {Email}", authorEmail);
        }

        // ---------------------------------------------------------------------------
        // Seed DropboxConnection stub for author
        // If a legacy access token exists in config, seed it as connected.
        // The author should reconnect via OAuth to get a proper refresh token.
        // ---------------------------------------------------------------------------
        var authorUser = db.AppUsers.First(u => u.Email == authorEmail);
        var existingConnection = db.DropboxConnections.FirstOrDefault(d => d.UserId == authorUser.Id);
        if (existingConnection is null)
        {
            var connection = DropboxConnection.CreateStub(authorUser.Id);

            if (!string.IsNullOrWhiteSpace(dropboxSettings.AccessToken))
            {
                // Seed legacy token â€” no refresh token available, will need reconnect when expired
                connection.Authorise(
                    dropboxSettings.AccessToken,
                    "legacy-no-refresh-token",
                    DateTime.UtcNow.AddDays(1)); // conservative expiry â€” prompt reconnect soon
                logger.LogWarning(
                    "Seeded legacy Dropbox access token for {Email}. " +
                    "Please reconnect via /dropbox/settings to get a proper refresh token.",
                    authorEmail);
            }

            db.DropboxConnections.Add(connection);
            await db.SaveChangesAsync();
            logger.LogInformation("DropboxConnection created for author {Email}", authorEmail);
        }

        // ---------------------------------------------------------------------------
        // Seed Test.scriv > Book 1 project
        // ---------------------------------------------------------------------------
        var existingProject = db.Projects.FirstOrDefault(p => p.Name == "Test - Book 1");
        if (existingProject is null)
        {
            var project = ScrivenerProject.Create(
                "Test - Book 1",
                scrivTestProjectDropboxPath,
                authorUser.Id,
                "DF1031AB-818A-41EB-AD49-F26D5C44F3D4");
            db.Projects.Add(project);
            await db.SaveChangesAsync();
            logger.LogInformation("Test - Book 1 project created: {Path}", scrivTestProjectDropboxPath);
        }
    }
}
