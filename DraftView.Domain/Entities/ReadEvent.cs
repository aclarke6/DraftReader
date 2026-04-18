using DraftView.Domain.Exceptions;

namespace DraftView.Domain.Entities;

public sealed class ReadEvent
{
    // ---------------------------------------------------------------------------
    // Properties
    // ---------------------------------------------------------------------------

    public Guid Id { get; private set; }
    public Guid SectionId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime FirstOpenedAt { get; private set; }
    public DateTime LastOpenedAt { get; private set; }
    public int OpenCount { get; private set; }
    public int? LastReadVersionNumber { get; private set; }

    /// <summary>
    /// The most recent time the reader completed reading this section.
    /// Null until the reader has read the section with a current SectionVersion.
    /// Set alongside LastReadVersionNumber when a reader opens a versioned section.
    /// </summary>
    public DateTime? LastReadAt { get; private set; }

    // ---------------------------------------------------------------------------
    // Constructor
    // ---------------------------------------------------------------------------

    private ReadEvent() { }

    // ---------------------------------------------------------------------------
    // Factory
    // ---------------------------------------------------------------------------

    public static ReadEvent Create(Guid sectionId, Guid userId)
    {
        var now = DateTime.UtcNow;

        return new ReadEvent
        {
            Id            = Guid.NewGuid(),
            SectionId     = sectionId,
            UserId        = userId,
            FirstOpenedAt = now,
            LastOpenedAt  = now,
            OpenCount     = 1
        };
    }

    // ---------------------------------------------------------------------------
    // Behaviour
    // ---------------------------------------------------------------------------

    public void RecordOpen()
    {
        // I-12: FirstOpenedAt is never modified after creation
        LastOpenedAt = DateTime.UtcNow;
        OpenCount++;
    }

    /// <summary>
    /// Records the version number most recently read by this reader.
    /// Called when a reader opens a section that has a current SectionVersion.
    /// </summary>
    /// <param name="versionNumber">The version number (must be >= 1).</param>
    /// <exception cref="InvariantViolationException">Thrown when version number is less than 1.</exception>
    public void UpdateLastReadVersion(int versionNumber)
    {
        if (versionNumber < 1)
            throw new InvariantViolationException("I-READ-VER",
                "Version number must be 1 or greater.");

        LastReadVersionNumber = versionNumber;
    }

    /// <summary>
    /// Records that the reader has read this section at the current version.
    /// Sets LastReadAt to the current UTC time.
    /// Called when a reader opens a section that has a current SectionVersion.
    /// </summary>
    public void RecordRead()
    {
        LastReadAt = DateTime.UtcNow;
    }
}
