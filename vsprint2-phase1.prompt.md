---
mode: agent
description: V-Sprint 2 Phase 1 — Diff Engine (Domain)
---

# V-Sprint 2 / Phase 1 — Diff Engine (Domain)

## Agent Instructions

Use **Claude Sonnet 4.5** as the model for this session.
Operate in **agent mode** — read existing files, create new files, run terminal commands.

Before writing any code:
1. Read `Publishing And Versioning Architecture.md` sections 4.3, 5, and V-Sprint 2
2. Read `REFACTORING.md` sections 2, 5, 6, and 9
3. Read `.github/copilot-instructions.md`
4. Read `.github/instructions/versioning.instructions.md`
5. Read `DraftView.Domain/Entities/SectionVersion.cs` — understand the existing entity
6. Confirm the active branch is `vsprint-2--phase-1-diff-engine`
   — if not on this branch, stop and report
7. Run `dotnet test --nologo` and record the baseline passing count before touching any code

---

## Goal

Introduce a paragraph-level HTML diff engine in the Domain layer.
Given two HTML content strings (a previous version and the current version), the engine
produces a list of paragraph-level diff results — added, removed, or unchanged.

This is a pure domain/application concern. No UI in this phase. No controller changes.
The diff engine is source-agnostic: it makes no distinction between sync-sourced and
import-sourced content.

---

## TDD Sequence — Mandatory

1. Create the stub with `throw new NotImplementedException()`
2. Write all failing tests
3. Confirm tests are red
4. Implement to make tests green
5. Run full test suite — zero regressions before committing
6. Commit with `domain:` prefix

---

## Existing Patterns — Follow These Exactly

- Domain enumerations in `DraftView.Domain/Enumerations/`
- Domain value objects and services in `DraftView.Domain/`
- Domain tests in `DraftView.Domain.Tests/`
- Test method naming: `{Method}_{Condition}_{ExpectedOutcome}`
- XML summary on every class and every method over 5 lines
- Test class XML summary states what is covered and what is excluded
- No external diff library dependencies — implement the diff logic directly
- The diff operates on paragraphs extracted from HTML, not raw HTML strings

---

## Deliverable 1 — `DiffResultType` Enum

**File:** `DraftView.Domain/Enumerations/DiffResultType.cs`

```csharp
/// <summary>
/// Classifies a paragraph in a diff result.
/// </summary>
public enum DiffResultType
{
    Unchanged = 0,
    Added     = 1,
    Removed   = 2
}
```

No tests required for an enum. Proceed to Deliverable 2.

---

## Deliverable 2 — `ParagraphDiffResult` Value Object

**File:** `DraftView.Domain/Diff/ParagraphDiffResult.cs`

```csharp
/// <summary>
/// Represents a single paragraph in a diff result.
/// Carries the paragraph text and its classification relative to the comparison.
/// </summary>
public sealed class ParagraphDiffResult
{
    /// <summary>The paragraph content as plain text (HTML tags stripped).</summary>
    public string Text { get; }

    /// <summary>The raw paragraph HTML from the source version.</summary>
    public string Html { get; }

    /// <summary>Whether this paragraph was added, removed, or unchanged.</summary>
    public DiffResultType Type { get; }

    public ParagraphDiffResult(string text, string html, DiffResultType type)
    {
        Text = text;
        Html = html;
        Type = type;
    }
}
```

No tests required for a value object. Proceed to Deliverable 3.

---

## Deliverable 3 — `IHtmlDiffService` Interface

**File:** `DraftView.Domain/Interfaces/Services/IHtmlDiffService.cs`

```csharp
/// <summary>
/// Computes a paragraph-level diff between two HTML content strings.
/// Source-agnostic — makes no distinction between sync and import content.
/// </summary>
public interface IHtmlDiffService
{
    /// <summary>
    /// Computes a paragraph-level diff between the from and to HTML strings.
    /// Returns a list of ParagraphDiffResult ordered as they appear in the
    /// combined sequence (removed paragraphs from `from`, added paragraphs
    /// from `to`, unchanged paragraphs preserved in position).
    /// </summary>
    IReadOnlyList<ParagraphDiffResult> Compute(string? from, string? to);
}
```

No tests required for an interface. Proceed to Deliverable 4.

---

## Deliverable 4 — `HtmlDiffService` Implementation

**File:** `DraftView.Application/Services/HtmlDiffService.cs`

Implements `IHtmlDiffService`.

### Algorithm

The diff operates at paragraph granularity:

1. **Extract paragraphs** from each HTML string:
   - Split on `<p>`, `<p `, `</p>`, `<br>`, `<br/>`, `<br />`
   - Strip all HTML tags from each extracted segment for text comparison
   - Discard empty segments after stripping
   - Preserve the original HTML of each paragraph for rendering

2. **Compare** using a Longest Common Subsequence (LCS) approach:
   - Compare paragraphs by their stripped text content (case-sensitive)
   - Paragraphs present in both → `Unchanged`
   - Paragraphs present only in `to` → `Added`
   - Paragraphs present only in `from` → `Removed`

3. **Null/empty handling:**
   - If `from` is null or empty and `to` has content → all `to` paragraphs are `Added`
   - If `to` is null or empty and `from` has content → all `from` paragraphs are `Removed`
   - If both null or empty → return empty list

4. **Return** the merged sequence ordered: Removed paragraphs from `from` appear
   before the corresponding insertion point in `to`.

### HTML Tag Stripping Helper

```csharp
private static string StripTags(string html)
    => System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", string.Empty).Trim();
```

### Paragraph Extraction Helper

Extract paragraphs by splitting on `<p>` boundaries. For each match of `<p[^>]*>(.*?)</p>`
(case-insensitive, single-line), capture the inner content. If no `<p>` tags are found,
treat the entire content as a single paragraph.

---

### Tests — `DraftView.Application.Tests/Services/HtmlDiffServiceTests.cs`

Write all tests **failing** before implementing:

```
Compute_BothNull_ReturnsEmptyList
Compute_BothEmpty_ReturnsEmptyList
Compute_FromNull_AllAdded
Compute_ToNull_AllRemoved
Compute_IdenticalContent_AllUnchanged
Compute_AddedParagraph_DetectsAddition
Compute_RemovedParagraph_DetectsRemoval
Compute_ChangedParagraph_DetectsRemovalAndAddition
Compute_MultiParagraph_CorrectSequence
Compute_IgnoresHtmlTagDifferences_WhenTextIsIdentical
Compute_PreservesOriginalHtml_InResult
```

**Key test expectations:**

- `Compute_IdenticalContent_AllUnchanged`: two identical single-paragraph strings →
  one result with `Type = Unchanged`

- `Compute_AddedParagraph_DetectsAddition`: `from` has 1 paragraph, `to` has 2 →
  one `Unchanged` + one `Added`

- `Compute_RemovedParagraph_DetectsRemoval`: `from` has 2 paragraphs, `to` has 1 →
  one `Removed` + one `Unchanged`

- `Compute_ChangedParagraph_DetectsRemovalAndAddition`: `from` has `<p>Hello</p>`,
  `to` has `<p>World</p>` → one `Removed` + one `Added`

- `Compute_IgnoresHtmlTagDifferences_WhenTextIsIdentical`: `<p><strong>Hello</strong></p>`
  vs `<p>Hello</p>` → both strip to "Hello" → `Unchanged`

- `Compute_PreservesOriginalHtml_InResult`: verify `Html` property on result preserves
  the original paragraph HTML, not the stripped text

Run full test suite. Zero regressions before proceeding.
Commit: `domain: add HtmlDiffService with paragraph-level LCS diff`

---

## Deliverable 5 — DI Registration

**File:** `DraftView.Web/Extensions/ServiceCollectionExtensions.cs`

In `AddApplicationServices`:

```csharp
services.AddScoped<IHtmlDiffService, HtmlDiffService>();
```

Run `dotnet build --nologo` to confirm the solution compiles.
Run `dotnet test --nologo` — full suite green.
Commit: `app: register HtmlDiffService in DI`

---

## Phase Gate — All Must Pass Before Marking Complete

Run `dotnet test --nologo` and confirm:

- [ ] All new tests green
- [ ] Total passing count equal to or greater than baseline
- [ ] Solution builds without errors
- [ ] `DiffResultType` enum exists in `DraftView.Domain/Enumerations`
- [ ] `ParagraphDiffResult` exists in `DraftView.Domain/Diff`
- [ ] `IHtmlDiffService` exists in `DraftView.Domain/Interfaces/Services`
- [ ] `HtmlDiffService` exists in `DraftView.Application/Services`
- [ ] `IHtmlDiffService` registered in `ServiceCollectionExtensions.cs`
- [ ] No controller changes
- [ ] No view changes
- [ ] No migration required
- [ ] TASKS.md Phase 1 checkbox updated to `[x]`
- [ ] All changes committed to `vsprint-2--phase-1-diff-engine`

## Do NOT implement in this phase

- `IDiffService` application wrapper — Phase 2
- Reader highlighting — Phase 3
- Change classification — V-Sprint 4
- Any UI changes
