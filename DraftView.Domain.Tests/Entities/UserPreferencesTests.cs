using DraftView.Domain.Entities;
using DraftView.Domain.Enumerations;
using DraftView.Domain.Exceptions;

namespace DraftView.Domain.Tests.Entities;

public class UserPreferencesTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    // ---------------------------------------------------------------------------
    // CreateForBetaReader
    // ---------------------------------------------------------------------------

    [Fact]
    public void CreateForBetaReader_ReturnsPrefsWithSensibleDefaults()
    {
        var prefs = UserPreferences.CreateForBetaReader(UserId);

        Assert.NotEqual(Guid.Empty, prefs.Id);
        Assert.Equal(UserId, prefs.UserId);
        Assert.True(prefs.NotifyOnNewSection);
        Assert.False(prefs.NotifyOnSectionChanged);
        Assert.Equal(NotifyOnReply.AuthorOnly, prefs.NotifyOnReply);
        Assert.Null(prefs.AuthorDigestMode);
        Assert.Null(prefs.AuthorDigestIntervalHours);
        Assert.Null(prefs.AuthorTimezone);
        Assert.Equal(DisplayTheme.Light, prefs.DisplayTheme);
    }

    // ---------------------------------------------------------------------------
    // CreateForAuthor
    // ---------------------------------------------------------------------------

    [Fact]
    public void CreateForAuthor_ReturnsPrefsWithAuthorFields()
    {
        var prefs = UserPreferences.CreateForAuthor(UserId, AuthorDigestMode.Immediate, null, "Europe/London");

        Assert.Equal(UserId, prefs.UserId);
        Assert.Equal(AuthorDigestMode.Immediate, prefs.AuthorDigestMode);
        Assert.Null(prefs.AuthorDigestIntervalHours);
        Assert.Equal("Europe/London", prefs.AuthorTimezone);
    }

    [Fact]
    public void CreateForAuthor_DigestMode_RequiresInterval()
    {
        var ex = Assert.Throws<InvariantViolationException>(
            () => UserPreferences.CreateForAuthor(UserId, AuthorDigestMode.Digest, null, "Europe/London"));

        Assert.Equal("I-19-INTERVAL", ex.InvariantCode);
    }

    [Fact]
    public void CreateForAuthor_DigestMode_WithInterval_Succeeds()
    {
        var prefs = UserPreferences.CreateForAuthor(UserId, AuthorDigestMode.Digest, 4, "Europe/London");

        Assert.Equal(4, prefs.AuthorDigestIntervalHours);
    }

    // ---------------------------------------------------------------------------
    // UpdateBetaReaderPreferences
    // ---------------------------------------------------------------------------

    [Fact]
    public void UpdateBetaReaderPreferences_UpdatesAllFields()
    {
        var prefs = UserPreferences.CreateForBetaReader(UserId);

        prefs.UpdateBetaReaderPreferences(false, true, NotifyOnReply.AnyParticipant);

        Assert.False(prefs.NotifyOnNewSection);
        Assert.True(prefs.NotifyOnSectionChanged);
        Assert.Equal(NotifyOnReply.AnyParticipant, prefs.NotifyOnReply);
    }

    // ---------------------------------------------------------------------------
    // UpdateAuthorPreferences
    // ---------------------------------------------------------------------------

    [Fact]
    public void UpdateAuthorPreferences_ImmediateMode_ClearsInterval()
    {
        var prefs = UserPreferences.CreateForAuthor(UserId, AuthorDigestMode.Digest, 4, "Europe/London");

        prefs.UpdateAuthorPreferences(AuthorDigestMode.Immediate, null, "Europe/London");

        Assert.Equal(AuthorDigestMode.Immediate, prefs.AuthorDigestMode);
        Assert.Null(prefs.AuthorDigestIntervalHours);
    }

    [Fact]
    public void UpdateAuthorPreferences_DigestMode_WithoutInterval_Throws()
    {
        var prefs = UserPreferences.CreateForAuthor(UserId, AuthorDigestMode.Immediate, null, "Europe/London");

        var ex = Assert.Throws<InvariantViolationException>(
            () => prefs.UpdateAuthorPreferences(AuthorDigestMode.Digest, null, "Europe/London"));

        Assert.Equal("I-19-INTERVAL", ex.InvariantCode);
    }
}
