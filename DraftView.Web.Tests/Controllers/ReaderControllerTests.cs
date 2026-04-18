using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using DraftView.Domain.Entities;
using DraftView.Domain.Enumerations;
using DraftView.Domain.Interfaces.Repositories;
using DraftView.Domain.Interfaces.Services;
using DraftView.Domain.Notifications;
using DraftView.Web;
using DraftView.Web.Controllers;
using DraftView.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace DraftView.Web.Tests.Controllers;

public class ReaderControllerTests
{
    private readonly Mock<IProjectRepository> projectRepo = new();
    private readonly Mock<ISectionRepository> sectionRepo = new();
    private readonly Mock<ICommentService> commentService = new();
    private readonly Mock<IReadingProgressService> progressService = new();
    private readonly Mock<IUserRepository> userRepo = new();
    private readonly Mock<IUserPreferencesRepository> prefsRepo = new();
    private readonly Mock<IReaderAccessRepository> readerAccessRepo = new();
    private readonly Mock<ISectionVersionRepository> sectionVersionRepo = new();
    private readonly Mock<IReadEventRepository> readEventRepo = new();
    private readonly Mock<ISectionDiffService> sectionDiffService = new();
    private readonly Mock<ILogger<ReaderController>> logger = new();

    [Fact]
    public async Task Read_DesktopRead_PopulatesModelWithStoredProsePreferences()
    {
        var user = User.Create("reader@example.test", "Reader", Role.BetaReader);
        user.Activate();

        var project = Project.Create("Project 1", "/Apps/Scrivener/Project1", user.Id, "project-root");

        var chapter = Section.CreateFolder(project.Id, "chapter-uuid", "Chapter 1", null, 1);
        chapter.MarkAsPublishedContainer();

        var scene = Section.CreateDocument(project.Id, "scene-uuid", "Scene 1", chapter.Id, 1, "<p>Hello</p>", "scene-hash", "Draft");
        scene.PublishAsPartOfChapter("scene-hash");

        var prefs = UserPreferences.CreateForBetaReader(user.Id);
        prefs.UpdateProseFontPreferences(ProseFont.Humanist, ProseFontSize.Large);

        var sut = CreateSut(user, userAgent: "Mozilla/5.0");

        userRepo.Setup(r => r.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        sectionRepo.Setup(r => r.GetByIdAsync(chapter.Id, It.IsAny<CancellationToken>())).ReturnsAsync(chapter);
        sectionRepo.Setup(r => r.GetByProjectIdAsync(project.Id, It.IsAny<CancellationToken>())).ReturnsAsync([chapter, scene]);
        projectRepo.Setup(r => r.GetByIdAsync(project.Id, It.IsAny<CancellationToken>())).ReturnsAsync(project);
        progressService.Setup(r => r.RecordOpenAsync(It.IsAny<Guid>(), user.Id, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        commentService.Setup(r => r.GetThreadsForSectionAsync(It.IsAny<Guid>(), user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Comment>());
        prefsRepo.Setup(r => r.GetByUserIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(prefs);

        var result = await sut.Read(chapter.Id);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("DesktopRead", view.ViewName);

        var model = Assert.IsType<DesktopChapterReadViewModel>(view.Model);
        Assert.Equal(ProseFont.Humanist, model.ProseFont);
        Assert.Equal(ProseFontSize.Large, model.ProseFontSize);
    }

    [Fact]
    public async Task Read_MobileRead_PopulatesModelWithStoredProsePreferences()
    {
        var user = User.Create("reader@example.test", "Reader", Role.BetaReader);
        user.Activate();

        var project = Project.Create("Project 1", "/Apps/Scrivener/Project1", user.Id, "project-root");

        var chapter = Section.CreateFolder(project.Id, "chapter-uuid", "Chapter 1", null, 1);
        chapter.MarkAsPublishedContainer();

        var scene = Section.CreateDocument(project.Id, "scene-uuid", "Scene 1", chapter.Id, 1, "<p>Hello</p>", "scene-hash", "Draft");
        scene.PublishAsPartOfChapter("scene-hash");

        var prefs = UserPreferences.CreateForBetaReader(user.Id);
        prefs.UpdateProseFontPreferences(ProseFont.Classic, ProseFontSize.ExtraLarge);

        var sut = CreateSut(user, userAgent: "Mozilla/5.0 (iPhone)");

        userRepo.Setup(r => r.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        sectionRepo.Setup(r => r.GetByIdAsync(scene.Id, It.IsAny<CancellationToken>())).ReturnsAsync(scene);
        sectionRepo.Setup(r => r.GetByIdAsync(chapter.Id, It.IsAny<CancellationToken>())).ReturnsAsync(chapter);
        sectionRepo.Setup(r => r.GetByProjectIdAsync(project.Id, It.IsAny<CancellationToken>())).ReturnsAsync([chapter, scene]);
        projectRepo.Setup(r => r.GetByIdAsync(project.Id, It.IsAny<CancellationToken>())).ReturnsAsync(project);
        progressService.Setup(r => r.RecordOpenAsync(It.IsAny<Guid>(), user.Id, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        commentService.Setup(r => r.GetThreadsForSectionAsync(It.IsAny<Guid>(), user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Comment>());
        prefsRepo.Setup(r => r.GetByUserIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(prefs);

        var result = await sut.Read(scene.Id);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("MobileRead", view.ViewName);

        var model = Assert.IsType<MobileReadViewModel>(view.Model);
        Assert.Equal(ProseFont.Classic, model.ProseFont);
        Assert.Equal(ProseFontSize.ExtraLarge, model.ProseFontSize);
    }

    private ReaderController CreateSut(User user, string userAgent)
    {
        var controller = new ReaderController(
            projectRepo.Object,
            sectionRepo.Object,
            commentService.Object,
            progressService.Object,
            userRepo.Object,
            prefsRepo.Object,
            readerAccessRepo.Object,
            sectionVersionRepo.Object,
            readEventRepo.Object,
            sectionDiffService.Object,
            logger.Object);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
            {
                User = new ClaimsPrincipal(
                    new ClaimsIdentity(
                        [new Claim(ClaimTypes.Name, user.Email)],
                        "TestAuth"))
            }
        };

        controller.ControllerContext.HttpContext.Request.Headers.UserAgent = userAgent;

        return controller;
    }
}

public class ReaderReadRenderingRegressionTests : IClassFixture<ReaderReadRenderingRegressionTests.ReaderReadFactory>
{
    private readonly ReaderReadFactory factory;

    public ReaderReadRenderingRegressionTests(ReaderReadFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Read_Desktop_RendersModelDrivenProseDataAttributes()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = true,
            BaseAddress = new Uri("https://localhost")
        });

        client.DefaultRequestHeaders.Add(TestAuthHandler.HeaderName, TestAuthHandler.ReaderMode);

        var response = await client.GetAsync($"/Reader/Read/{factory.ChapterId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();

        Assert.Matches(
            new Regex("<div\\s+class=\\\"reader-page\\\"[^>]*data-prose-font=\\\"Humanist\\\"[^>]*data-prose-font-size=\\\"Large\\\"", RegexOptions.IgnoreCase),
            html);
    }

    public sealed class ReaderReadFactory : WebApplicationFactory<Program>
    {
        public static readonly Guid ReaderId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        private readonly User reader;
        private readonly Project project;
        private readonly Section chapter;
        private readonly Section scene;
        private readonly UserPreferences prefs;

        public Guid ChapterId => chapter.Id;

        public ReaderReadFactory()
        {
            reader = User.Create("reader.render@example.test", "Reader", Role.BetaReader);
            reader.Activate();

            project = Project.Create("Project 1", "/Apps/Scrivener/Project1", reader.Id, "project-root");

            chapter = Section.CreateFolder(project.Id, "chapter-uuid", "Chapter 1", null, 1);
            chapter.MarkAsPublishedContainer();

            scene = Section.CreateDocument(project.Id, "scene-uuid", "Scene 1", chapter.Id, 1, "<p>Hello</p>", "scene-hash", "Draft");
            scene.PublishAsPartOfChapter("scene-hash");

            prefs = UserPreferences.CreateForBetaReader(reader.Id);
            prefs.UpdateProseFontPreferences(ProseFont.Humanist, ProseFontSize.Large);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=draftview_tests;Username=test;Password=test",
                    ["EmailProtection:EncryptionKey"] = "MDEyMzQ1Njc4OUFCQ0RFRjAxMjM0NTY3ODlBQkNERUY=",
                    ["EmailProtection:LookupHmacKey"] = "RkVEQ0JBOTg3NjU0MzIxMEZFRENCQTk4NzY1NDMyMTA=",
                    ["Email:Provider"] = "Console"
                });
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IHostedService>();

                services.AddAuthentication(TestAuthHandler.SchemeName)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                        TestAuthHandler.SchemeName,
                        _ => { });

                services.PostConfigureAll<AuthenticationOptions>(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                    options.DefaultScheme = TestAuthHandler.SchemeName;
                });

                var userRepo = new Mock<IUserRepository>();
                userRepo.Setup(r => r.GetByIdAsync(ReaderId, It.IsAny<CancellationToken>())).ReturnsAsync(reader);
                userRepo.Setup(r => r.GetByIdAsync(reader.Id, It.IsAny<CancellationToken>())).ReturnsAsync(reader);
                userRepo.Setup(r => r.GetByEmailAsync("reader.render@example.test", It.IsAny<CancellationToken>())).ReturnsAsync(reader);

                var prefsRepo = new Mock<IUserPreferencesRepository>();
                prefsRepo.Setup(r => r.GetByUserIdAsync(reader.Id, It.IsAny<CancellationToken>())).ReturnsAsync(prefs);

                var projectRepo = new Mock<IProjectRepository>();
                projectRepo.Setup(r => r.GetByIdAsync(project.Id, It.IsAny<CancellationToken>())).ReturnsAsync(project);

                var sectionRepo = new Mock<ISectionRepository>();
                sectionRepo.Setup(r => r.GetByIdAsync(chapter.Id, It.IsAny<CancellationToken>())).ReturnsAsync(chapter);
                sectionRepo.Setup(r => r.GetByIdAsync(scene.Id, It.IsAny<CancellationToken>())).ReturnsAsync(scene);
                sectionRepo.Setup(r => r.GetByProjectIdAsync(project.Id, It.IsAny<CancellationToken>())).ReturnsAsync([chapter, scene]);

                var commentService = new Mock<ICommentService>();
                commentService.Setup(r => r.GetThreadsForSectionAsync(It.IsAny<Guid>(), reader.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Array.Empty<Comment>());

                var progressService = new Mock<IReadingProgressService>();
                progressService.Setup(r => r.RecordOpenAsync(It.IsAny<Guid>(), reader.Id, It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

                var readEventRepo = new Mock<IReadEventRepository>();
                readEventRepo.Setup(r => r.GetAsync(It.IsAny<Guid>(), reader.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((ReadEvent?)null);

                var sectionDiffService = new Mock<ISectionDiffService>();
                sectionDiffService.Setup(s => s.GetDiffForReaderAsync(It.IsAny<Guid>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Domain.Contracts.SectionDiffResult?)null);

                var systemStateMessageService = new Mock<ISystemStateMessageService>();
                systemStateMessageService.Setup(s => s.GetActiveMessageAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync((SystemStateMessage?)null);

                services.RemoveAll<IUserRepository>();
                services.RemoveAll<IUserPreferencesRepository>();
                services.RemoveAll<IProjectRepository>();
                services.RemoveAll<ISectionRepository>();
                services.RemoveAll<ICommentService>();
                services.RemoveAll<IReadingProgressService>();
                services.RemoveAll<IReaderAccessRepository>();
                services.RemoveAll<ISectionVersionRepository>();
                services.RemoveAll<IReadEventRepository>();
                services.RemoveAll<ISectionDiffService>();
                services.RemoveAll<ISystemStateMessageService>();

                services.AddSingleton(userRepo.Object);
                services.AddSingleton(prefsRepo.Object);
                services.AddSingleton(projectRepo.Object);
                services.AddSingleton(sectionRepo.Object);
                services.AddSingleton(commentService.Object);
                services.AddSingleton(progressService.Object);
                services.AddSingleton(Mock.Of<IReaderAccessRepository>());
                services.AddSingleton(Mock.Of<ISectionVersionRepository>());
                services.AddSingleton(readEventRepo.Object);
                services.AddSingleton(sectionDiffService.Object);
                services.AddSingleton(systemStateMessageService.Object);
            });
        }
    }

    private sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string SchemeName = "ReaderReadTestAuth";
        public const string HeaderName = "X-Test-Auth";
        public const string ReaderMode = "Reader";

        public TestAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(HeaderName, out var mode))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            if (!string.Equals(mode, ReaderMode, StringComparison.Ordinal))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, ReaderReadFactory.ReaderId.ToString()),
                new Claim(ClaimTypes.Name, "reader.render@example.test"),
                new Claim(ClaimTypes.Role, Role.BetaReader.ToString())
            };

            var identity = new ClaimsIdentity(claims, SchemeName);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, SchemeName);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
