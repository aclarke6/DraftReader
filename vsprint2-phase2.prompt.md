---
mode: agent
description: V-Sprint 2 Phase 2 — Application Diff Service
---

# V-Sprint 2 / Phase 2 — Application Diff Service

## Agent Instructions

Use **Claude Sonnet 4.5** as the model for this session.
Operate in **agent mode** — read existing files, create new files, run terminal commands.

Before writing any code:
1. Read `Publishing And Versioning Architecture.md` sections 9.1 and V-Sprint 2
2. Read `.github/copilot-instructions.md`
3. Read `.github/instructions/versioning.instructions.md`
4. Read `DraftView.Application/Services/HtmlDiffService.cs` — understand the existing diff engine
5. Read `DraftView.Domain/Interfaces/Repositories/ISectionVersionRepository.cs`
6. Read `DraftView.Application/Services/ReadingProgressService.cs`
7. Confirm the active branch is `vsprint-2--phase-2-diff-service`
   — if not on this branch, stop and report
8. Run `dotnet test --nologo` and record the baseline passing count before touching any code

---

## Goal

Introduce `ISectionDiffService` and `SectionDiffService` — an application-layer service
that computes the diff between the version a reader last read and the current latest version.

The output of this service is consumed by the reader view in Phase 3.
No UI changes in this phase.

---

## TDD Sequence — Mandatory

1. Create the stub with `throw new NotImplementedException()`
2. Write all failing tests
3. Confirm tests are red
4. Implement to make tests green
5. Run full test suite — zero regressions before committing
6. Commit with `app:` prefix

---

## Existing Patterns — Follow These Exactly

- Application service interfaces in `DraftView.Domain/Interfaces/Services/`
- Application service implementations in `DraftView.Application/Services/`
- Test classes in `DraftView.Application.Tests/Services/`
- Test method naming: `{Method}_{Condition}_{ExpectedOutcome}`
- XML summary on every class and every method over 5 lines
- Inject `ISectionVersionRepository` for version lookups — already registered in DI
- Inject `IHtmlDiffService` for the diff computation
- `ISectionVersionRepository.GetLatestAsync(sectionId, ct)` — returns latest version or null
- `ISectionVersionRepository.GetByVersionNumberAsync(sectionId, versionNumber, ct)` — 
  check if this method exists before using it; if not, use `GetAllBySectionIdAsync` and
  filter in memory

---

## Deliverable 1 — `SectionDiffResult` DTO

**File:** `DraftView.Domain/Contracts/SectionDiffResult.cs`

```csharp
/// <summary>
/// The result of comparing two versions of a section.
/// Contains the paragraph-level diff and version metadata.
/// </summary>
public sealed class SectionDiffResult
{
    /// <summary>The version number the reader last read. Null if never read.</summary>
    public int? FromVersionNumber { get; init; }

    /// <summary>The current latest version number.</summary>
    public int CurrentVersionNumber { get; init; }

    /// <summary>True when the reader's last read version differs from the current version.</summary>
    public bool HasChanges { get; init; }

    /// <summary>Paragraph-level diff results. Empty when no changes or no prior version.</summary>
    public IReadOnlyList<ParagraphDiffResult> Paragraphs { get; init; }
        = Array.Empty<ParagraphDiffResult>();
}
```

No tests required for a DTO.

---

## Deliverable 2 — `ISectionDiffService`

**File:** `DraftView.Domain/Interfaces/Services/ISectionDiffService.cs`

```csharp
/// <summary>
/// Computes the diff between what a reader last read and the current version.
/// </summary>
public interface ISectionDiffService
{
    /// <summary>
    /// Returns the diff for a section from the reader's last read version
    /// to the current latest version. Returns null if no current version exists.
    /// Returns a result with HasChanges = false if the reader is on the latest version.
    /// </summary>
    Task<SectionDiffResult?> GetDiffForReaderAsync(
        Guid sectionId,
        int? lastReadVersionNumber,
        CancellationToken ct = default);
}
```

---

## Deliverable 3 — `SectionDiffService`

**File:** `DraftView.Application/Services/SectionDiffService.cs`

Implements `ISectionDiffService`.

**`GetDiffForReaderAsync` sequence:**

1. Load latest version via `ISectionVersionRepository.GetLatestAsync(sectionId, ct)`
2. If no version exists → return `null` (section has never been published with versioning)
3. If `lastReadVersionNumber` is null → reader has never read this section
   - Return `SectionDiffResult` with `HasChanges = false`, `FromVersionNumber = null`,
     `CurrentVersionNumber = latestVersion.VersionNumber`, `Paragraphs = empty`
   - Rationale: no diff to show on first read
4. If `lastReadVersionNumber == latestVersion.VersionNumber` → reader is current
   - Return `SectionDiffResult` with `HasChanges = false`
5. If `lastReadVersionNumber < latestVersion.VersionNumber` → changes exist
   - Load the "from" version: find the version matching `lastReadVersionNumber`
     via `GetAllBySectionIdAsync` filtered in memory
   - If the from version is not found (version was deleted or never existed)
     → treat as `HasChanges = true` with empty `Paragraphs`
   - Call `IHtmlDiffService.Compute(fromVersion.HtmlContent, latestVersion.HtmlContent)`
   - Return `SectionDiffResult` with `HasChanges = true`, diff paragraphs, version numbers

### Tests — `DraftView.Application.Tests/Services/SectionDiffServiceTests.cs`

Write all tests **failing** before implementing:

```
GetDiffForReaderAsync_WhenNoVersionExists_ReturnsNull
GetDiffForReaderAsync_WhenReaderHasNeverRead_ReturnsNoChanges
GetDiffForReaderAsync_WhenReaderIsOnLatestVersion_ReturnsNoChanges
GetDiffForReaderAsync_WhenNewerVersionExists_ReturnsHasChanges
GetDiffForReaderAsync_WhenNewerVersionExists_ReturnsCorrectVersionNumbers
GetDiffForReaderAsync_WhenNewerVersionExists_CallsDiffService
GetDiffForReaderAsync_WhenFromVersionNotFound_ReturnsHasChangesWithEmptyParagraphs
```

For `GetDiffForReaderAsync_WhenNewerVersionExists_CallsDiffService`:
verify `IHtmlDiffService.Compute` is called with the `from` version's `HtmlContent`
and the latest version's `HtmlContent`.

Use `SectionVersion.Create(section, authorId, versionNumber)` to build test versions.
Build a `Section` via `Section.CreateDocument(...)` or `Section.CreateDocumentForUpload(...)`
before calling `SectionVersion.Create`.

Run full test suite. Zero regressions before proceeding.
Commit: `app: add SectionDiffService computing reader diff from last read version`

---

## Deliverable 4 — DI Registration

**File:** `DraftView.Web/Extensions/ServiceCollectionExtensions.cs`

In `AddApplicationServices`:

```csharp
services.AddScoped<ISectionDiffService, SectionDiffService>();
```

Run `dotnet build --nologo` to confirm compilation.
Run `dotnet test --nologo` — full suite green.
Commit: `app: register SectionDiffService in DI`

---

## Phase Gate — All Must Pass Before Marking Complete

Run `dotnet test --nologo` and confirm:

- [ ] All new tests green
- [ ] Total passing count equal to or greater than baseline
- [ ] Solution builds without errors
- [ ] `SectionDiffResult` exists in `DraftView.Domain/Contracts`
- [ ] `ISectionDiffService` exists in `DraftView.Domain/Interfaces/Services`
- [ ] `SectionDiffService` exists in `DraftView.Application/Services`
- [ ] `ISectionDiffService` registered in `ServiceCollectionExtensions.cs`
- [ ] No controller changes
- [ ] No view changes
- [ ] No migration required
- [ ] TASKS.md Phase 2 checkbox updated to `[x]`
- [ ] All changes committed to `vsprint-2--phase-2-diff-service`

## Do NOT implement in this phase

- Reader highlighting — Phase 3
- Change classification — V-Sprint 4
- Any UI changes
- `ReadEvent.LastReadVersionNumber` update — already done in V-Sprint 1 Phase 4
