using Microsoft.Extensions.Configuration;
using Moq;
using DraftView.Application.Services;
using DraftView.Domain.Entities;
using DraftView.Domain.Enumerations;
using DraftView.Domain.Exceptions;
using DraftView.Domain.Interfaces.Repositories;
using DraftView.Domain.Interfaces.Services;

namespace DraftView.Application.Tests.Services;

public class UserServiceInvitationAcceptanceTests
{
    private readonly Mock<IUserRepository> UserRepo = new();
    private readonly Mock<IInvitationRepository> InviteRepo = new();
    private readonly Mock<IUserNotificationPreferencesRepository> PrefsRepo = new();
    private readonly Mock<IEmailSender> EmailSender = new();
    private readonly Mock<IUnitOfWork> UnitOfWork = new();
    private readonly Mock<IConfiguration> Config = new();
    private readonly Mock<IReaderAccessRepository> ReaderAccessRepo = new();

    private UserService CreateSut() => new(
        UserRepo.Object,
        InviteRepo.Object,
        PrefsRepo.Object,
        EmailSender.Object,
        UnitOfWork.Object,
        Config.Object,
        ReaderAccessRepo.Object);

    [Fact]
    public async Task AcceptInvitationAsync_ValidToken_PersistsEnteredDisplayName()
    {
        var user = User.Create("reader@example.com", "Pending", Role.BetaReader);
        var invitation = Invitation.CreateAlwaysOpen(user.Id);
        var sut = CreateSut();

        InviteRepo.Setup(r => r.GetByTokenAsync(invitation.Token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);
        UserRepo.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await sut.AcceptInvitationAsync(invitation.Token, "Reader Four", CancellationToken.None);

        Assert.Same(user, result);
        Assert.Equal("Reader Four", user.DisplayName);
        Assert.True(user.IsActive);
        Assert.Equal(InvitationStatus.Accepted, invitation.Status);

        UnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AcceptInvitationAsync_BlankDisplayName_ThrowsInvariantViolationException()
    {
        var user = User.Create("reader@example.com", "Pending", Role.BetaReader);
        var invitation = Invitation.CreateAlwaysOpen(user.Id);
        var sut = CreateSut();

        InviteRepo.Setup(r => r.GetByTokenAsync(invitation.Token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);
        UserRepo.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var ex = await Assert.ThrowsAsync<InvariantViolationException>(() =>
            sut.AcceptInvitationAsync(invitation.Token, "   ", CancellationToken.None));

        Assert.Equal("I-DISPLAYNAME", ex.InvariantCode);
    }
}
