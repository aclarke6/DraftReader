Here's a coding principles document for DraftView sessions:

---

**DraftView — Session Coding Principles**

**PowerShell Output**
- Every command that produces output must pipe through `2>&1 | Tee-Object -Variable b; $b | clip`
- No exceptions — this shows output in console AND copies to clipboard simultaneously
- Short commands (under ~50 lines) pasted directly into terminal
- Scripts 50+ lines delivered as `.ps1` files using `present_files`

**Script Verification — MANDATORY**
- Every string replacement must compare old and new content before proceeding
- If `$newContent -eq $content` → `Write-Host "ERROR"` and `exit 1`
- Never build after a replacement without first verifying it applied
- Never assume a previous step succeeded — verify explicitly

**CSS Changes — MANDATORY**
- Every script touching any `.css` file must bump `--css-version` in `DraftView.Core.css`
- Always use regex replace — never hardcode the expected current version:
```powershell
$core = $core -replace '--css-version: "v[^"]+";', '--css-version: "v2026-04-02-1";'
```
- Verify the bump applied before saving

**Controller Guards — MANDATORY**
- Every public action in `AuthorController` must have `RequireAuthorAsync()` or `GetAuthorAsync()` as first statement
- Audit after adding any new action:
```powershell
Select-String -Path "AuthorController.cs" -Pattern "public async Task<IActionResult>|GetAuthorAsync|RequireAuthorAsync"
```

**Build and Test**
- Always build before running the app: `dotnet build DraftView.slnx --no-restore -v q`
- Always run tests after code changes: `dotnet test DraftView.slnx --no-build -v q`
- "Green tests" from the user means all passed — do not ask them to paste if they say this
- "Clean build" means 0 errors, 0 warnings — do not ask them to paste if they say this

**File Changes**
- Never guess at file contents — always read before writing replacement scripts
- Full file rewrites preferred over regex patching for complex files
- PowerShell here-strings must use single-quoted `@'...'@` to avoid quote mangling
- Line endings: use `Get-LineEndings.ps1` to detect before building multiline match strings
- Scripts start with `cls` and end with the next required command

**Architecture**
- Tenancy-agnostic: every new table with author-scoped data gets `AuthorId`
- TDD required for all new domain entities — tests first, implementation second
- No full tenancy model until product is live with a single author and billing in place
- Repository methods returning scoped data must accept or imply `AuthorId`

**General Conduct**
- Never suggest the user hasn't run something, restarted the app, or rebuilt
- Never suggest "cache is stale" as a diagnosis
- Never ask "shall we get back to the task list?"
- Do not add `Write-Host` prompts or prose like "Then run:" before commands
- Scripts end with the next required command ready to run — no trailing explanation
- All responses follow the TASKS.md task order — new ideas go into TASKS.md, not into the current session

---

Save this as `PRINCIPLES.md` in the repo root alongside `TASKS.md` and `PowerShell.md`.