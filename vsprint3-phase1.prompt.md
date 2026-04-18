---
mode: agent
description: V-Sprint 3 Phase 1 — Reader State (LastReadAt)
---

# V-Sprint 3 / Phase 1 — Reader State

## Agent Instructions

Use **Claude Sonnet 4.5** as the model for this session.
Operate in **agent mode** — read existing files, create new files, run terminal commands.

Before writing any code:
1. Read `Publishing And Versioning Architecture.md` sections 4.5 and V-Sprint 3 Phase 1
2. Read `REFACTORING.md` in full
3. Read `.github/copilot-instructions.md`
4. Read `DraftView.Domain/Entities/ReadEvent.cs` — understand the existing entity
5. Read `DraftView.Domain.Tests/Entities/ReadEventTests.cs` — understand existing test coverage
6. Read `DraftView.Infrastructure/Persistence/Configurations/ReadEventConfiguration.cs`
7. Confirm the active branch is `vsprint-3--phase-1-reader-state`
   — if not on this branch, stop and report
8. Run `dotnet test --nologo` and record the baseline passing count before touching any code

---

## Goal

Add `LastReadAt` to `ReadEvent` — a nullable `DateTime` that records when the reader
most recently read this section. This is distinct from `LastOpenedAt` (which tracks
every open including brief visits) — `LastReadAt` is set deliberately when a reader
reaches the end of a section or spends meaningful time on it.

For this sprint, `LastReadAt` is set at the same time as `LastReadVersionNumber` —
when a reader opens a section that has a current `SectionVersion`. The distinction
between "opened" and "read" will be refined in a later sprint when scroll position
tracking is introduced.

`LastReadVersionNumber` is already set in V-Sprint 1 Phase 4. Do NOT re-implement
or duplicate that logic. This phase adds only `LastReadAt` alongside it.

---

## TDD Sequence — Mandatory

1. Read existing `ReadEventTests.cs` to understand current coverage
2. Create stub — add property and method stub with `throw new NotImplementedException()`
3. Write all failing tests
4. Confirm tests are red
5. Implement to make tests green
6. Run full test suite — zero regressions before committing
7. Commit with `domain:` prefix

---

## Existing Patterns — Follow These Exactly

- Domain entities in `DraftView.Domain/Entities/`
- Domain tests in `DraftView.Domain.Tests/Entities/`
- All domain changes require TDD — no exceptions
- EF configuration in `DraftView.Infrastructure/Persistence/Configurations/`
- Migrations in `DraftView.Infrastructure/Migrations/`
- Test method naming: `{Method}_{Condition}_{ExpectedOutcome}`
- XML summary on every new method
- `DateTime.UtcNow` everywhere — never `DateTime.Now`

---

## Deliverable 1 — `ReadEvent.LastReadAt` Property

**File:** `DraftView.Domain/Entities/ReadEvent.cs`

Add to the Properties region:

```csharp
/// <summary>
/// The most recent time the reader completed reading this section.
/// Null until the reader has read the section with a current SectionVersion.
/// Set alongside LastReadVersionNumber when a reader opens a versioned section.
/// </summary>
public DateTime? LastReadAt { get; private set; }
```

---

## Deliverable 2 — `ReadEvent.RecordRead` Domain Method

Add to the Behaviour region of `ReadEvent`:

```csharp
/// <summary>
/// Records that the reader has read this section at the current version.
/// Sets LastReadAt to the current UTC time.
/// Called when a reader opens a section that has a current SectionVersion.
/// </summary>
public void RecordRead()
{
    LastReadAt = DateTime.UtcNow;
}
```

---

## Deliverable 3 — Domain Tests

**File:** `DraftView.Domain.Tests/Entities/ReadEventTests.cs`

Add to the existing test class. Do NOT create a new file — add to the existing one.

Write all tests **failing** before implementing:

```
RecordRead_SetsLastReadAt
RecordRead_OverwritesPreviousLastReadAt
RecordRead_DoesNotAffectOtherProperties
Create_HasNullLastReadAt
```

**Key expectations:**

- `RecordRead_SetsLastReadAt`: call `RecordRead()` on a fresh event → `LastReadAt` is not null
  and is approximately `DateTime.UtcNow` (within 1 second tolerance)

- `RecordRead_OverwritesPreviousLastReadAt`: call `RecordRead()` twice with a small delay →
  second call produces a later or equal `LastReadAt`

- `RecordRead_DoesNotAffectOtherProperties`: call `RecordRead()` → `SectionId`, `UserId`,
  `FirstOpenedAt`, `OpenCount`, `LastReadVersionNumber` are unchanged

- `Create_HasNullLastReadAt`: `ReadEvent.Create(...)` → `LastReadAt` is null

Run full test suite. Zero regressions.
Commit: `domain: add LastReadAt and RecordRead to ReadEvent`

---

## Deliverable 4 — EF Configuration

**File:** `DraftView.Infrastructure/Persistence/Configurations/ReadEventConfiguration.cs`

Add mapping for `LastReadAt`:

```csharp
builder.Property(e => e.LastReadAt)
    .HasColumnName("LastReadAt")
    .IsRequired(false);
```

---

## Deliverable 5 — EF Migration

Run:

```
dotnet ef migrations add AddLastReadAtToReadEvents --project DraftView.Infrastructure --startup-project DraftView.Web
```

Review the generated migration — confirm it adds a nullable `DateTime` column
`LastReadAt` to the `ReadEvents` table. It must not drop or alter any existing columns.

If the migration looks correct, do not apply it to the database yet — leave
`dotnet ef database update` for after the full test suite is green.

Then apply:

```
dotnet ef database update --project DraftView.Infrastructure --startup-project DraftView.Web
```

---

## Deliverable 6 — Wire `RecordRead` in `ReaderController`

**File:** `DraftView.Web/Controllers/ReaderController.cs`

In `ResolveSceneContentAndDiffAsync` (extracted helper from V-Sprint 2 Phase 3),
after the call to `ProgressService.UpdateLastReadVersionAsync`, add:

```csharp
var readEventForUpdate = await readEventRepo.GetAsync(scene.Id, userId, ct);
readEventForUpdate?.RecordRead();
```

Then save via unit of work. Check how `UpdateLastReadVersionAsync` saves — if it
already calls `SaveChangesAsync`, do not call it again. If it does not, add a save.

Read `ReadingProgressService.UpdateLastReadVersionAsync` before making this change
to understand the save pattern. Do not duplicate the save if one already occurs.

**Important:** Do not inject a new `IUnitOfWork` into `ReaderController`. If a save
is needed that is not covered by the existing progress service call, route it through
`IReadingProgressService` by adding a `RecordReadAsync` method — see Deliverable 7.

---

## Deliverable 7 — `IReadingProgressService.RecordReadAsync`

**File:** `DraftView.Domain/Interfaces/Services/IReadingProgressService.cs`

Add:

```csharp
/// <summary>
/// Records that the reader has read a section at the current version.
/// Sets LastReadAt on the existing ReadEvent if one exists.
/// No-op if no ReadEvent exists for this section and user.
/// </summary>
Task RecordReadAsync(Guid sectionId, Guid userId, CancellationToken ct = default);
```

**File:** `DraftView.Application/Services/ReadingProgressService.cs`

Implement:

```csharp
public async Task RecordReadAsync(Guid sectionId, Guid userId, CancellationToken ct = default)
{
    var readEvent = await _readEventRepo.GetAsync(sectionId, userId, ct);
    if (readEvent is null) return;

    readEvent.RecordRead();
    await _unitOfWork.SaveChangesAsync(ct);
}
```

### Tests — `DraftView.Application.Tests/Services/ReadingProgressServiceTests.cs`

Add to the existing test class:

```
RecordReadAsync_SetsLastReadAt_WhenReadEventExists
RecordReadAsync_DoesNotThrow_WhenNoReadEventExists
```

Run full test suite. Zero regressions.
Commit: `app: add RecordReadAsync to ReadingProgressService`

---

## Deliverable 8 — Call `RecordReadAsync` from Controller

**File:** `DraftView.Web/Controllers/ReaderController.cs`

In `ResolveSceneContentAndDiffAsync`, after `UpdateLastReadVersionAsync`:

```csharp
if (latestVersion is not null)
{
    await ProgressService.UpdateLastReadVersionAsync(scene.Id, userId, latestVersion.VersionNumber, ct);
    await ProgressService.RecordReadAsync(scene.Id, userId, ct);
}
```

No new dependencies needed — `ProgressService` is already available.

---

## Phase Gate — All Must Pass Before Marking Complete

Run `dotnet test --nologo` and confirm:

- [ ] All new tests green
- [ ] Total passing count equal to or greater than baseline
- [ ] Solution builds without errors
- [ ] `ReadEvent.LastReadAt` property exists (nullable DateTime)
- [ ] `ReadEvent.RecordRead()` method exists
- [ ] `IReadingProgressService.RecordReadAsync` exists
- [ ] `ReadingProgressService.RecordReadAsync` implemented
- [ ] EF migration created and applied
- [ ] `ReaderController.ResolveSceneContentAndDiffAsync` calls `RecordReadAsync`
- [ ] No inline styles introduced
- [ ] TASKS.md Phase 1 checkbox updated to `[x]`
- [ ] All changes committed to `vsprint-3--phase-1-reader-state`
- [ ] No warnings in test output linked to phase changes
- [ ] Refactor considered and applied where appropriate, tests green after refactor

---

## Identify All Warnings in Tests

Run `dotnet test --nologo` and identify any warnings in the test output.
Address any warnings that are linked to code changes made in this phase before
proceeding, as they may indicate potential issues in the code.

---

## Refactor Phase

After implementing the above, consider if any refactor is needed to improve code
quality, as per the refactoring guidelines. If so, perform the refactor and ensure
all tests still pass.

---

## Do NOT implement in this phase

- Update messaging ("Updated since you last read") — Phase 2
- Update banner — Phase 3
- Banner dismissal tracking — Phase 3
- Scroll position tracking — V-Sprint 5
- Any author-facing changes
