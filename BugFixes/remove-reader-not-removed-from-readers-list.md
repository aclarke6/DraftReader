You are working in the DraftView repository.

Goal:
Register, investigate, and fix the bug where removing a reader from `/Author/Readers`
does not remove that reader from the list.

Branching Strategy (strictly follow):
- The Mac owns a long-running branch: `BugFix-Mac`
- All bug fixes branch from `BugFix-Mac`, not `main`
- This bug must be developed in its own branch from `BugFix-Mac`
- After completion, merge the bugfix branch back into `BugFix-Mac`
- `BugFix-Mac` will later be merged into `main` as a batch and that is not part of this task

Work in this exact order. Do not skip steps. Do not guess file contents. Inspect files before changing them.

------------------------------------------------------------
1. Ensure `BugFix-Mac` exists and is up to date
------------------------------------------------------------
- checkout `main`
- pull latest `main`
- create or update `BugFix-Mac` from `main`
- switch to `BugFix-Mac`

------------------------------------------------------------
2. Create a dedicated bugfix branch from `BugFix-Mac`
------------------------------------------------------------
Create this branch:

`bugfix/remove-reader-not-removed-from-readers-list`

This branch must contain only this bug fix.

------------------------------------------------------------
3. Update `TASKS.md`
------------------------------------------------------------
Ensure the existing open bug entry remains accurate and, if necessary, sharpen it with the
root-cause detail discovered during investigation.

Bug title:
`Removing a reader from /Author/Readers does not remove the reader from the list`

Type:
`Bug Fix`

Problem:
Removing an invited or active reader appears to complete, but the reader still appears
in the `/Author/Readers` list afterwards.

Required outcome:
- removing an invited reader removes them from the `/Author/Readers` list
- removing an active reader removes them from the `/Author/Readers` list
- if removal cannot be applied, the UI must report that clearly instead of silently leaving stale state
- list rendering must reflect the persisted removal state

Acceptance criteria:
- `/Author/Readers` no longer shows successfully removed readers
- reader removal persists across a fresh page load
- invited and active reader cases are both covered
- regression tests prove the list behaviour

------------------------------------------------------------
4. Inspect relevant files BEFORE changes
------------------------------------------------------------
Read these files before making any edits:
- `TASKS.md`
- `DraftView.Web/Controllers/AuthorController.cs`
- `DraftView.Application/Services/UserService.cs`
- `DraftView.Infrastructure/Persistence/Repositories/UserRepository.cs`
- `DraftView.Web/Views/Author/Readers.cshtml`
- `DraftView.Web.Tests/Controllers/AuthorControllerTests.cs`
- any existing web/integration tests that exercise `/Author/Readers`

Do not assume the current implementation matches this prompt. Confirm it from the files.

------------------------------------------------------------
5. Current inspected fault seam
------------------------------------------------------------
Based on the current code inspection, these are the likely investigation seams:

- `AuthorController.Readers()` loads readers via `userRepo.GetAllBetaReadersAsync()` and then filters
  `!r.IsSoftDeleted` before building the list rows
- `AuthorController.SoftDeleteReader(Guid userId)` revokes access records, calls
  `userService.SoftDeleteUserAsync(userId, author.Id)`, saves via unit of work, and redirects back to `Readers`
- existing controller tests currently prove that `SoftDeleteUserAsync` is called, but do not prove that the removed
  reader disappears from the rendered readers list on a subsequent request
- likely root-cause candidates include:
  - soft-delete not being persisted as expected
  - readers query still returning stale or wrongly scoped records
  - invited vs active reader state not being handled consistently in the readers list path
  - list filtering not matching the real persisted removal semantics

Do not assume the root cause from this seam list. Reproduce and confirm it.

------------------------------------------------------------
6. Reproduce and identify the real root cause
------------------------------------------------------------
Do not patch blind.

Before implementation:
- reproduce locally if possible
- inspect the remove-reader POST flow end to end
- inspect how `/Author/Readers` loads and filters its rows
- identify exactly whether the defect is in:
  - controller orchestration
  - application-service behaviour
  - repository filtering/query shape
  - invited vs active reader status logic
  - persistence not being saved or reflected correctly

Document the confirmed root cause in the final report.

------------------------------------------------------------
7. Implement the fix (architecture-first)
------------------------------------------------------------
Apply these rules:

Behaviour:
- preserve the existing successful redirect after remove when the removal is valid
- ensure the removed reader no longer appears on `/Author/Readers`
- if removal is invalid or blocked, show a clear outcome rather than silently leaving the list unchanged

Architecture:
- keep HTTP result handling in Web
- keep user removal orchestration in Application
- keep query/filter logic in the repository layer if the bug is a data retrieval issue
- do not move logic across layers as a side effect
- do not introduce unrelated refactors

Refactoring discipline:
- if a small extraction is needed, keep it separate from the behaviour change commit
- no whole-class refactor unless the fault genuinely requires it

------------------------------------------------------------
8. Tests (required before finalising)
------------------------------------------------------------
Follow TDD.

Required coverage:
- removing an invited reader results in that reader no longer appearing in the readers list
- removing an active reader results in that reader no longer appearing in the readers list
- existing controller-level removal orchestration remains covered
- if the fix belongs in a repository or broader web flow, add coverage in the correct test layer rather than forcing it into a narrow controller unit test

Likely files to extend:
- `DraftView.Web.Tests/Controllers/AuthorControllerTests.cs`
- any existing `/Author/Readers` web regression tests, if present
- application or infrastructure tests if the root cause proves to be below the controller layer

------------------------------------------------------------
9. Validate
------------------------------------------------------------
- run restore, build, and tests
- confirm the relevant reader-removal tests are green
- confirm `/Author/Readers` no longer shows removed readers in the tested scenario
- confirm there is no regression in invited/active reader list behaviour

------------------------------------------------------------
10. Commit and merge into `BugFix-Mac`
------------------------------------------------------------
Commit in logical steps:
- `TASKS.md` update
- tests
- refactor commit if required
- implementation

Then:
- merge `bugfix/remove-reader-not-removed-from-readers-list` into `BugFix-Mac`
- remain on `BugFix-Mac`

------------------------------------------------------------
11. Final report
------------------------------------------------------------
Report:
- files changed
- confirmed root cause
- before vs after behaviour
- test results
- any follow-up risks

------------------------------------------------------------
Constraints
------------------------------------------------------------
- Do not modify behaviour outside this bug
- Do not invent production facts you have not verified
- Do not stop after `TASKS.md`
- Complete the workflow end-to-end once implementation begins
- Do not ask for confirmation mid-task