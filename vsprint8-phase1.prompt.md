---
mode: agent
description: V-Sprint 8 Phase 1 — Dropbox Incremental Sync
---

# V-Sprint 8 / Phase 1 — Dropbox Incremental Sync

## Agent Instructions

Use **Claude Sonnet 4.5** as the model for this session.
Operate in **agent mode** — read existing files, create new files, run terminal commands.

Before writing any code:
1. Read `Publishing And Versioning Architecture.md` sections 2, 3, 12, 13 and V-Sprint 8
2. Read `REFACTORING.md` in full
3. Read `.github/copilot-instructions.md`
4. Read `DraftView.Domain/Entities/Project.cs` — understand `SyncRootId`, `SyncStatus`, `LastSyncedAt`
5. Read `DraftView.Infrastructure/Services/DropboxFileDownloader.cs` — understand current full sync
6. Read `DraftView.Application/Services/SyncService.cs` — understand current sync orchestration
7. Read `DraftView.Domain/Interfaces/Services/ISyncProvider.cs` — understand the sync provider contract
8. Read `DraftView.Infrastructure/Services/ScrivenerSyncService.cs` — understand current `SyncProjectAsync`
9. Read `DraftView.Web/Controllers/DropboxController.cs` — understand how sync is triggered
10. Confirm the active branch is `vsprint-8--phase-1-incremental-sync`
    — if not on this branch, stop and report
11. Run `git status` — confirm the working tree is clean with no uncommitted changes.
    If uncommitted changes exist that are not part of this phase, stop and report.
12. Run `dotnet test --nologo` and record the baseline passing count before touching any code

---

## Goal

Replace full Dropbox file downloads on every sync with cursor-based incremental sync.
Only files changed since the last sync are downloaded. `Section.HtmlContent` is still
the only write target — the sync invariant is unchanged.

> Sync never creates versions. Sync only updates working state. Always.

This phase has no impact on publishing, versioning, the reader experience, or manual
upload. It is a pure infrastructure optimisation.

---

## Architecture

The Dropbox API supports cursor-based listing via `/files/list_folder` and
`/files/list_folder/continue`. A cursor is an opaque string returned by the API
representing the state of a folder at a point in time. On subsequent calls,
`/files/list_folder/continue` returns only the changes since that cursor.

**Cursor storage:**
- Add `DropboxCursor` (nullable string) to `Project`
- Persist cursor after each successful incremental sync
- On first sync (cursor null): perform a full list, store the resulting cursor
- On subsequent syncs: use `/files/list_folder/continue` with stored cursor

**Sync invariant:** unchanged. Sync writes to `Section.HtmlContent` only.

---

## TDD Sequence — Mandatory

Search all existing test files before writing any new tests.
Never write a duplicate test.

1. Add domain property stub
2. Write all failing domain tests
3. Implement domain to make tests green
4. Write failing infrastructure/application tests
5. Implement to make tests green
6. Run full test suite — zero regressions before committing

---

## Deliverable 1 — Domain: `Project` Cursor Property

**File:** `DraftView.Domain/Entities/Project.cs`

Add:

```csharp
/// <summary>
/// Opaque Dropbox cursor representing the last-synced folder state.
/// Null on first sync — triggers a full list to establish the cursor.
/// Populated after each successful incremental sync.
/// Only relevant for ProjectType.ScrivenerDropbox projects.
/// </summary>
public string? DropboxCursor { get; private set; }

public void UpdateDropboxCursor(string cursor)
{
    if (string.IsNullOrWhiteSpace(cursor))
        throw new InvariantViolationException("I-SYNC-CURSOR-EMPTY",
            "Dropbox cursor must not be empty.");
    DropboxCursor = cursor;
}
```

**Domain Tests:**

```
UpdateDropboxCursor_SetsCursor
UpdateDropboxCursor_WithEmptyString_ThrowsInvariantViolation
UpdateDropboxCursor_WithWhitespace_ThrowsInvariantViolation
UpdateDropboxCursor_OverwritesPreviousCursor
```

---

## Deliverable 2 — Infrastructure: EF Migration

**File:** Migration — `AddProjectDropboxCursor`

Add to `Projects` table:
- `DropboxCursor` — nullable string (nvarchar(max) / text)

Run:
```
dotnet ef migrations add AddProjectDropboxCursor --project DraftView.Infrastructure --startup-project DraftView.Web
dotnet ef database update --project DraftView.Infrastructure --startup-project DraftView.Web
```

Verify the generated migration does not drop or recreate the `Projects` table.
Add EF column mapping in `ProjectConfiguration` if not auto-detected.

---

## Deliverable 3 — Infrastructure: `IDropboxFileDownloader` Extension

**File:** `DraftView.Infrastructure/Services/DropboxFileDownloader.cs`
(or equivalent — inspect the actual file before modifying)

Inspect the current implementation before making any changes. Do not guess structure.

The incremental sync flow requires two new operations alongside the existing full download:

**3a — List changed entries since cursor:**

```csharp
/// <summary>
/// Returns changed file entries and the new cursor since the given cursor position.
/// Use when DropboxCursor is already set on the project.
/// </summary>
Task<(IReadOnlyList<DropboxChangedEntry> Entries, string NewCursor)>
    ListChangedEntriesAsync(string cursor, CancellationToken ct = default);
```

**3b — Establish initial cursor (first sync):**

```csharp
/// <summary>
/// Performs a full folder listing to establish the initial cursor.
/// Returns all current file entries and the cursor representing current state.
/// Use when DropboxCursor is null on the project.
/// </summary>
Task<(IReadOnlyList<DropboxChangedEntry> Entries, string InitialCursor)>
    ListAllEntriesWithCursorAsync(string dropboxPath, CancellationToken ct = default);
```

**`DropboxChangedEntry` model:**

```csharp
public record DropboxChangedEntry(
    string Path,
    DropboxEntryType EntryType,   // Added, Modified, Deleted
    string? ContentHash           // null for Deleted entries
);

public enum DropboxEntryType { Added, Modified, Deleted }
```

Place `DropboxChangedEntry` and `DropboxEntryType` in
`DraftView.Infrastructure/Models/` or alongside the downloader — check existing
model placement conventions before deciding.

**Dropbox API calls to use:**
- Initial listing: `POST /files/list_folder` with `path`, `recursive: true`
- Continuation: `POST /files/list_folder/continue` with `cursor`
- Handle pagination: loop `list_folder/continue` while `has_more: true`
- Map `.tag: "deleted"` entries to `DropboxEntryType.Deleted`
- Map `.tag: "file"` entries to `Added` or `Modified` based on whether the file
  existed in the previous sync (use content hash comparison where available)

Inspect existing Dropbox HTTP client configuration and auth token handling before
implementing — reuse existing patterns exactly.

---

## Deliverable 4 — Application: Incremental Sync in `SyncService` / `ScrivenerSyncService`

**File:** `DraftView.Application/Services/SyncService.cs` and/or
`DraftView.Infrastructure/Services/ScrivenerSyncService.cs`

Inspect both files before modifying. Do not assume which owns the sync loop.

Update the sync path for `ScrivenerDropbox` projects:

```
if project.DropboxCursor is null:
    → call ListAllEntriesWithCursorAsync(project.DropboxPath)
    → process all returned entries (existing full-sync behaviour)
    → call project.UpdateDropboxCursor(initialCursor)
    → save

else:
    → call ListChangedEntriesAsync(project.DropboxCursor)
    → process only changed entries:
        - Added / Modified → download file, update Section.HtmlContent
        - Deleted → soft-delete matching Section if found
    → call project.UpdateDropboxCursor(newCursor)
    → save
```

**Important constraints:**
- The existing full-sync path must remain intact and reachable (cursor null = first sync)
- `Section.HtmlContent` is still the only write target — no version creation
- Deleted entries: call existing soft-delete path if one exists; do not invent new deletion
  behaviour — inspect before deciding
- Log the number of changed entries processed and the new cursor (truncated) at
  `Information` level
- If `ListChangedEntriesAsync` throws a Dropbox `reset_cursor` error (cursor expired
  or invalidated): clear `project.DropboxCursor`, fall back to full sync, log a warning

---

## Deliverable 5 — Tests

Inspect existing sync/downloader test files before writing. Do not duplicate.

**Domain tests** (Deliverable 1 above) — write in `DraftView.Domain.Tests`.

**Infrastructure tests** — write in `DraftView.Infrastructure.Tests` or equivalent:

```
ListChangedEntriesAsync_ReturnsParsedEntries_ForModifiedFiles
ListChangedEntriesAsync_ReturnsParsedEntries_ForDeletedFiles
ListChangedEntriesAsync_HandlesMultiplePages
ListAllEntriesWithCursorAsync_ReturnsAllEntriesAndCursor
```

Use `HttpMessageHandler` mocking or equivalent existing test pattern — inspect before
writing.

**Application/integration tests** — write in the appropriate test project:

```
SyncProjectAsync_WithNullCursor_PerformsFullSyncAndStoresCursor
SyncProjectAsync_WithExistingCursor_ProcessesOnlyChangedEntries
SyncProjectAsync_WhenCursorExpired_FallsBackToFullSync
SyncProjectAsync_WithDeletedEntry_SoftDeletesMatchingSection
SyncProjectAsync_UpdatesDropboxCursor_AfterSuccessfulSync
```

---

## Phase Gate — All Must Pass Before Marking Complete

Run `dotnet test --nologo` and confirm:

- [ ] All new tests green
- [ ] Total passing count equal to or greater than baseline
- [ ] Solution builds without errors
- [ ] `Project.DropboxCursor` property exists
- [ ] `Project.UpdateDropboxCursor()` domain method exists with empty-string guard
- [ ] EF migration `AddProjectDropboxCursor` applied cleanly
- [ ] `ListChangedEntriesAsync` implemented and tested
- [ ] `ListAllEntriesWithCursorAsync` implemented and tested
- [ ] `DropboxChangedEntry` and `DropboxEntryType` exist
- [ ] Incremental sync path active when `DropboxCursor` is set
- [ ] Full sync path active when `DropboxCursor` is null
- [ ] Cursor stored after each successful sync
- [ ] Cursor-expired fallback implemented and tested
- [ ] Deleted entries handled via existing soft-delete path
- [ ] Sync invariant unchanged — no version creation in sync path
- [ ] No changes to publishing, versioning, or reader flows
- [ ] No changes to manual upload flow
- [ ] Logging added for changed entry count and cursor update
- [ ] TASKS.md Phase 1 checkbox updated to `[x]`
- [ ] All changes committed to `vsprint-8--phase-1-incremental-sync`
- [ ] No warnings in test output linked to phase changes
- [ ] Refactor considered and applied where appropriate, tests green after refactor

---

## Identify All Warnings in Tests

Run `dotnet test --nologo` and identify any warnings in the test output.
Address any warnings linked to code changes made in this phase before proceeding.

---

## Refactor Phase

After implementing the above, consider if any refactor is needed to improve code
quality, as per the refactoring guidelines. In particular:

- The full-sync and incremental-sync paths may share entry-processing logic — consider
  extracting a private `ProcessSyncEntriesAsync(IReadOnlyList<DropboxChangedEntry>)`
  helper to avoid duplication.
- Ensure all methods remain under the 30-line threshold per REFACTORING.md section 2.

---

## Do NOT implement in this phase

- Dropbox webhook controller for push-based sync — backlog item
- Dropbox OAuth2 token refresh — backlog item
- Any changes to publishing, versioning, locking, or scheduling
- Any reader-facing changes
- Any changes to the manual upload flow
- Any changes to `ISyncProvider` contract — implementation change only
