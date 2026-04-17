---
mode: agent
description: V-Sprint 1 Phase 1 — Domain and Infrastructure Foundation
---

# V-Sprint 1 / Phase 1 — Domain + Infrastructure Foundation

## Agent Instructions

Use **Claude Sonnet 4.5** as the model for this session.
Operate in **agent mode** — read existing files, create new files, run terminal commands.

Before writing any code:
1. Read `Publishing And Versioning Architecture.md` sections 4 and V-Sprint 1 Phase 1
2. Read `REFACTORING.md` sections 2, 5, 6, and 9
3. Read `.github/copilot-instructions.md`
4. Read `.github/instructions/versioning.instructions.md`
5. Confirm the active branch is `vsprint-1/phase-1-domain-infrastructure`
   - If not on this branch, stop and report — do not create the branch automatically
6. Run `dotnet test --nologo` and record the baseline passing count before touching any code

---

## Goal

Establish the versioning data model and manual upload domain additions.
No behaviour change visible in production after this phase.
Migration applies cleanly. All existing tests remain green.

---

## TDD Sequence — Mandatory for Every Deliverable

1. Create the stub with `throw new NotImplementedException()` or empty property
2. Write all failing tests for that stub
3. Confirm tests are red
4. Implement to make tests green
5. Run full test suite — zero regressions before proceeding to the next deliverable
6. Commit with a `domain:` or `infra:` prefix before moving on

---

## Existing Patterns — Follow These Exactly

- Entity IDs are `Guid`, not `int`
- Private parameterless constructor on every entity
- Factory methods are `public static`, named `Create` (or `CreateRoot`, `CreateReply` etc. for variants)
- Invariant violations throw `InvariantViolationException("I-CODE", "message")`
- `InvariantViolationException` is in `DraftView.Domain.Exceptions`
- `DraftView.Domain.Enumerations` namespace for all enums
- Repository pattern: interface in `DraftView.Domain.Interfaces.Repositories`, implementation in `DraftView.Infrastructure.Persistence.Repositories`
- EF configuration classes in `DraftView.Infrastructure.Persistence.Configurations`
- All entities registered in `DraftViewDbContext`
- All repositories registered in DI in `DraftView.Web`
- XML summary comments required on every class and every method over 5 lines (see `REFACTORING.md` section 9)

---

## Deliverable 1 — `ProjectType` Enum

**File:** `DraftView.Domain/Enumerations/ProjectType.cs`

```csharp
/// <summary>
/// Determines the ingestion source for a project.
/// ScrivenerDropbox projects sync via Dropbox. Manual projects receive
/// content only via author-initiated file import.
/// </summary>
public enum ProjectType
{
    ScrivenerDropbox = 0,
    Manual = 1
}
```

No domain tests required for a plain enum.

---

## Deliverable 2 — `Project.ProjectType` Property

**File:** `DraftView.Domain/Entities/Project.cs`

Add `ProjectType ProjectType` property with `private set`.

Add a second factory overload for manual projects:

```csharp
public static Project CreateManual(string name, Guid authorId)
```

- `name` must not be null or whitespace — throw `InvariantViolationException("I-PROJ-NAME", ...)`
- `authorId` must not be `Guid.Empty` — throw `InvariantViolationException("I-PROJ-AUTHOR", ...)`
- `DropboxPath` is `string.Empty` for manual projects (field is non-nullable — do not change its type)
- `SyncRootId` is null
- `ProjectType = ProjectType.Manual`
- `SyncStatus = SyncStatus.Stale`
- `IsReaderActive = false`

Existing `Create` factory sets `ProjectType = ProjectType.ScrivenerDropbox`.

### Tests — add to `DraftView.Domain.Tests/Entities/ProjectTests.cs`

Write failing tests first:

```
CreateManual_WithValidData_ReturnsProject
CreateManual_SetsProjectTypeToManual
CreateManual_HasNullSyncRootId
CreateManual_WithNullName_ThrowsInvariantViolation
CreateManual_WithEmptyGuidAuthorId_ThrowsInvariantViolation
Create_ExistingFactory_SetsProjectTypeToScrivenerDropbox
```

Run full test suite. Zero regressions before proceeding.
Commit: `domain: add ProjectType enum and Project.CreateManual factory`

---

## Deliverable 3 — `SectionVersion` Entity

**File:** `DraftView.Domain/Entities/SectionVersion.cs`

```csharp
/// <summary>
/// An immutable snapshot of a Section's prose content at the moment of a
/// Republish action. Readers always see the latest SectionVersion.
/// HtmlContent and ContentHash are immutable after creation.
/// </summary>
public sealed class SectionVersion
{
    public Guid Id { get; private set; }
    public Guid SectionId { get; private set; }
    public Guid AuthorId { get; private set; }
    public int VersionNumber { get; private set; }
    public string HtmlContent { get; private set; } = default!;
    public string ContentHash { get; private set; } = default!;
    public ChangeClassification? ChangeClassification { get; private set; }
    public string? AiSummary { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private SectionVersion() { }

    public static SectionVersion Create(Section section, Guid authorId, int nextVersionNumber)
    { ... }
}
```

**`ChangeClassification` enum** — create before implementing `SectionVersion`:

**File:** `DraftView.Domain/Enumerations/ChangeClassification.cs`

```csharp
/// <summary>
/// Classifies the nature of changes between two SectionVersion snapshots.
/// Populated by IChangeClassificationService in V-Sprint 4.
/// </summary>
public enum ChangeClassification
{
    Polish = 0,
    Revision = 1,
    Rewrite = 2
}
```

**`Create` factory invariants** — throw `InvariantViolationException` if violated:

| Code | Condition |
|------|-----------|
| `I-VER-FOLDER` | `section.NodeType != NodeType.Document` |
| `I-VER-DELETED` | `section.IsSoftDeleted == true` |
| `I-VER-CONTENT` | `string.IsNullOrEmpty(section.HtmlContent)` |
| `I-VER-AUTHOR` | `authorId == Guid.Empty` |
| `I-VER-NUMBER` | `nextVersionNumber < 1` |

On success, snapshot `section.HtmlContent` and `section.ContentHash` into the new entity.
Set `CreatedAt = DateTime.UtcNow`. `ChangeClassification` and `AiSummary` are null.

### Tests — `DraftView.Domain.Tests/Entities/SectionVersionTests.cs`

New test class — add class-level XML summary stating what is covered and excluded.
Write all tests failing before implementing `Create`:

```
Create_WithDocumentSection_ReturnsVersion
Create_WithDocumentSection_SnapshotsHtmlContent
Create_WithDocumentSection_SnapshotsContentHash
Create_WithDocumentSection_SetsVersionNumber
Create_WithDocumentSection_SetsAuthorId
Create_WithDocumentSection_SetsCreatedAt
Create_WithDocumentSection_ChangeClassificationIsNull
Create_WithDocumentSection_AiSummaryIsNull
Create_WithFolderSection_ThrowsInvariantViolation
Create_WithSoftDeletedSection_ThrowsInvariantViolation
Create_WithNullHtmlContent_ThrowsInvariantViolation
Create_WithEmptyHtmlContent_ThrowsInvariantViolation
Create_WithEmptyGuidAuthorId_ThrowsInvariantViolation
Create_WithVersionNumberZero_ThrowsInvariantViolation
Create_WithVersionNumberNegative_ThrowsInvariantViolation
```

Run full test suite. Zero regressions before proceeding.
Commit: `domain: add SectionVersion entity with Create factory and invariants`

---

## Deliverable 4 — `ReadEvent` Additions

**File:** `DraftView.Domain/Entities/ReadEvent.cs`

Add to existing entity:

```csharp
public int? LastReadVersionNumber { get; private set; }
```

Add domain method:

```csharp
/// <summary>
/// Records the version number most recently read by this reader.
/// Called when a reader opens a section that has a current SectionVersion.
/// </summary>
public void UpdateLastReadVersion(int versionNumber)
```

Invariant: `versionNumber` must be >= 1. Throw `InvariantViolationException("I-READ-VER", ...)`.

### Tests — add to `DraftView.Domain.Tests/Entities/ReadEventTests.cs`

Write failing tests first:

```
UpdateLastReadVersion_SetsVersionNumber
UpdateLastReadVersion_OverwritesPreviousValue
UpdateLastReadVersion_WithVersionNumberZero_ThrowsInvariantViolation
UpdateLastReadVersion_WithNegativeVersionNumber_ThrowsInvariantViolation
Create_LastReadVersionNumberIsNull
```

Run full test suite. Zero regressions before proceeding.
Commit: `domain: add ReadEvent.LastReadVersionNumber and UpdateLastReadVersion`

---

## Deliverable 5 — `Comment.SectionVersionId`

**File:** `DraftView.Domain/Entities/Comment.cs`

Add to existing entity:

```csharp
public Guid? SectionVersionId { get; private set; }
```

This property is set only at comment creation time. Do not add a setter method.
Update all three factory methods (`CreateRoot`, `CreateReply`, `CreateForImport`) to accept
an optional `Guid? sectionVersionId = null` parameter and assign it.

No new domain tests required for a nullable FK addition to existing factories, but
verify existing `CommentTests` still pass after the factory signature change.

Run full test suite. Zero regressions before proceeding.
Commit: `domain: add Comment.SectionVersionId nullable FK`

---

## Deliverable 6 — `ISectionVersionRepository`

**File:** `DraftView.Domain/Interfaces/Repositories/ISectionVersionRepository.cs`

```csharp
/// <summary>
/// Repository contract for SectionVersion persistence.
/// </summary>
public interface ISectionVersionRepository
{
    /// <summary>Returns the highest VersionNumber for a section, or 0 if none exist.</summary>
    Task<int> GetMaxVersionNumberAsync(Guid sectionId, CancellationToken ct = default);

    /// <summary>Returns the latest SectionVersion for a section, or null if none exist.</summary>
    Task<SectionVersion?> GetLatestAsync(Guid sectionId, CancellationToken ct = default);

    /// <summary>Returns all versions for a section, ordered by VersionNumber ascending.</summary>
    Task<IReadOnlyList<SectionVersion>> GetAllBySectionIdAsync(Guid sectionId, CancellationToken ct = default);

    Task AddAsync(SectionVersion version, CancellationToken ct = default);
}
```

---

## Deliverable 7 — Infrastructure: Repository + EF Configuration

**File:** `DraftView.Infrastructure/Persistence/Repositories/SectionVersionRepository.cs`

Implement `ISectionVersionRepository`. Follow the pattern of `ReadEventRepository.cs`.
`GetLatestAsync` returns the version with the highest `VersionNumber` for the given section.
`GetMaxVersionNumberAsync` returns `MAX(VersionNumber)` or `0` if no versions exist.

**File:** `DraftView.Infrastructure/Persistence/Configurations/SectionVersionConfiguration.cs`

Follow the pattern of `ReadEventConfiguration.cs`.
- `HasKey(v => v.Id)`
- `VersionNumber` is required
- `HtmlContent` is required
- `ContentHash` is required, `MaxLength(64)`
- `AiSummary` is optional, `MaxLength(500)`
- Index on `SectionId` (non-unique — one section has many versions)
- `ChangeClassification` stored as int

**Register in `DraftViewDbContext`:**
- Add `DbSet<SectionVersion> SectionVersions`
- Add `SectionVersionConfiguration` to `OnModelCreating`

**Register in DI** (Web project service registration):
- `ISectionVersionRepository` → `SectionVersionRepository`

Commit: `infra: add SectionVersionRepository, EF configuration, DI registration`

---

## Deliverable 8 — Migration

Generate the migration:

```
dotnet ef migrations add AddVersioningAndManualUpload --project DraftView.Infrastructure --startup-project DraftView.Web
```

Review the generated migration before applying. It must contain:
- New `SectionVersions` table
- New nullable `LastReadVersionNumber` column on `ReadEvents`
- New nullable `SectionVersionId` column on `Comments` with FK to `SectionVersions`
- New `ProjectType` int column on `Projects` with default value `0`

If EF generates unexpected operations (drops, renames), investigate before applying.
Do not apply a migration that drops existing data.

Apply the migration:

```
dotnet ef database update --project DraftView.Infrastructure --startup-project DraftView.Web
```

Commit: `infra: generate AddVersioningAndManualUpload migration`

---

## Phase Gate — All Must Pass Before Marking Complete

Run `dotnet test --nologo` and confirm:

- [ ] All new tests green
- [ ] Total passing count equal to or greater than baseline recorded at session start
- [ ] Migration applied cleanly with no data loss
- [ ] `SectionVersion`, `ChangeClassification`, `ProjectType` exist in `DraftView.Domain`
- [ ] `ISectionVersionRepository` exists in `DraftView.Domain.Interfaces.Repositories`
- [ ] `SectionVersionRepository` and `SectionVersionConfiguration` exist in `DraftView.Infrastructure`
- [ ] `DraftViewDbContext` has `DbSet<SectionVersion>`
- [ ] `Comment.SectionVersionId` exists as a nullable `Guid?`
- [ ] `ReadEvent.LastReadVersionNumber` exists as a nullable `int?`
- [ ] `Project.CreateManual` factory exists
- [ ] No inline styles introduced in any view
- [ ] No production code exists without a corresponding failing test that preceded it
- [ ] TASKS.md Phase 1 checkbox updated to `[x]`
- [ ] All changes committed to `vsprint-1/phase-1-domain-infrastructure`

## Do NOT implement in this phase

- `SectionTreeService` or `ImportService` — Phase 2
- `VersioningService` or `IVersioningService` — Phase 3
- Any controller changes — Phase 5
- Any view changes — Phase 5
- Republish button or upload UI — Phase 5