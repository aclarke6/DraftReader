using DraftView.Domain.Entities;
using DraftView.Infrastructure.Persistence;
using DraftView.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DraftView.Infrastructure.Tests.Persistence;

public class ReaderAccessRepositoryTests
{
    private static DraftViewDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<DraftViewDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new DraftViewDbContext(options);
    }

    [Fact]
    public async Task RevokeAllForReaderAsync_RevokesActiveRecordsForAuthor()
    {
        using var db = CreateDb();
        var readerId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        var access = ReaderAccess.Grant(readerId, authorId, projectId);
        db.ReaderAccess.Add(access);
        await db.SaveChangesAsync();

        var sut = new ReaderAccessRepository(db);
        await sut.RevokeAllForReaderAsync(readerId, authorId);
        await db.SaveChangesAsync();

        var record = await db.ReaderAccess.FirstAsync();
        Assert.NotNull(record.RevokedAt);
    }

    [Fact]
    public async Task RevokeAllForReaderAsync_DoesNotRevokeRecordsForDifferentAuthor()
    {
        using var db = CreateDb();
        var readerId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var otherAuthorId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        var access = ReaderAccess.Grant(readerId, authorId, projectId);
        db.ReaderAccess.Add(access);
        await db.SaveChangesAsync();

        var sut = new ReaderAccessRepository(db);
        await sut.RevokeAllForReaderAsync(readerId, otherAuthorId);
        await db.SaveChangesAsync();

        var record = await db.ReaderAccess.FirstAsync();
        Assert.Null(record.RevokedAt);
    }

    [Fact]
    public async Task RevokeAllForReaderAsync_IgnoresAlreadyRevokedRecords()
    {
        using var db = CreateDb();
        var readerId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        var access = ReaderAccess.Grant(readerId, authorId, projectId);
        access.Revoke();
        var revokedAt = access.RevokedAt;
        db.ReaderAccess.Add(access);
        await db.SaveChangesAsync();

        var sut = new ReaderAccessRepository(db);
        await sut.RevokeAllForReaderAsync(readerId, authorId);
        await db.SaveChangesAsync();

        var record = await db.ReaderAccess.FirstAsync();
        Assert.Equal(revokedAt, record.RevokedAt);
    }
}