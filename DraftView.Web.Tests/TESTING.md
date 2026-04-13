# Web Test Hosting

This project supports full HTTP tests with `WebApplicationFactory<Program>`.

Two seams in `DraftView.Web/Program.cs` make that possible:

- `public partial class Program;`
- startup migration, seeding, and stale-sync reset are skipped when the host runs in the `Testing` environment

That means a test can boot the real MVC, Identity, middleware, and Razor pipeline without inheriting production startup side effects.

## Running tests

From the solution root, run web-hosted tests with `dotnet test` as normal.

Examples:

Run one governing rendered-output test by method name:

```powershell
dotnet test DraftView.Web.Tests --filter Governing_RenderedOutput_NoStoredEmailDisplayedInNonWhitelistedPages_MUST_FAIL_UNTIL_PHASE2
```

Run one test class:

```powershell
dotnet test DraftView.Web.Tests --filter GoverningRenderedEmailExposureTests
```

Run the real HTTP login regression test:

```powershell
dotnet test DraftView.Web.Tests --filter Account_Login_Post_WithValidAuthorCredentials_RedirectsToAuthorDashboard_AndAuthenticatesSession
```

For the real login regression, the configured PostgreSQL user must be able to create and drop the dedicated test database used by the factory.

Run all web tests:

```powershell
dotnet test DraftView.Web.Tests
```

You do not need to set `ASPNETCORE_ENVIRONMENT` manually when the test is written correctly.
The custom `WebApplicationFactory<Program>` should call `UseEnvironment("Testing")`, so the host switches into test mode automatically during `dotnet test`.

## How to use it

### 1. Create a custom factory

Derive from `WebApplicationFactory<Program>` and force the host environment to `Testing`.

Why:

- in `Testing`, the app does not auto-migrate
- in `Testing`, the app does not auto-seed
- in `Testing`, the app does not auto-reset sync state

This keeps the test host deterministic.

### 2. Replace or configure the app's data services

Inside `ConfigureWebHost`, replace the normal database registration with test-owned infrastructure.

Typical options:

- use a dedicated PostgreSQL test database when the scenario depends on Identity, EF mappings, or realistic repository behaviour
- use an in-memory provider only if the scenario does not depend on real relational behaviour
- remove or replace noisy hosted/background services if they are irrelevant to the scenario

For the login regression currently in this project, the factory rewrites `DefaultConnection` to a dedicated database named `draftview_webtests_login` and resets it at test startup.

The important rule is:

- the test owns the application state
- production startup must not be allowed to create surprise data

### 3. Seed only the scenario you need

Arrange the minimum data needed for the route or flow under test.

Examples:

- rendered author page:
  - one author domain user
  - one matching Identity user if the page requires normal auth
  - only the projects, readers, invitations, or sections needed by the page

- invitation page:
  - one invited user
  - one valid invitation token

- real login flow:
  - one domain user
  - one matching Identity user
  - one known valid password

Avoid reusing broad dev seed data. Tests should be explicit about what exists.

### 4. Choose the correct auth mode

There are two valid patterns.

#### A. Use a test auth scheme

Use this when the test is about rendered output or authorized page behaviour, but not about the login process itself.

Examples:

- `/Author/Dashboard`
- `/Author/Readers`
- `/Support/Dashboard`

Requirements:

- the injected claims principal must match a real domain user in test data
- `User.Identity.Name` must be realistic if the app or layout reads it
- role claims must match the route policy

This is important because the app is not purely claims-driven. Several controllers resolve the current user from the repository using `User.Identity.Name`.

#### B. Use the real login flow

Use this when the test is specifically about `/Account/Login`.

Examples:

- authentication succeeds with valid credentials
- login continues to work after protected lookup replaces plaintext email lookup

Requirements:

- do not short-circuit auth with a test scheme for this scenario
- seed a real Identity user with a known password
- seed the matching domain user expected by the app
- send a real HTTP POST to `/Account/Login`

### 5. Create the client and run the request

Use the client from the factory to exercise the real app over HTTP.

Typical patterns:

- `GET` a rendered page and inspect final HTML
- `POST` the login form and inspect the redirect or authenticated follow-up request

For rendered-output privacy tests:

- assert against a known seeded email string
- check final HTML, not just controller output

For real login tests:

- assert successful redirect
- assert the expected role landing page
- optionally assert that a follow-up authorized request succeeds

## When to use which pattern

Use test auth when:

- you need an authenticated request
- login itself is not what you are testing
- you want a narrow, stable rendered-page or authorization test

Use real login when:

- the test is specifically about authentication behaviour
- the scenario must prove `/Account/Login` still works through the actual HTTP pipeline

## Practical examples

### Rendered page privacy test

Goal:

- verify a non-whitelisted page does not display a known stored email in final HTML

Recommended approach:

- host app in `Testing`
- seed one known user and any minimum page data
- authenticate using a test auth scheme
- request the page
- assert the known email string is absent from the response HTML

### Login regression test

Goal:

- verify `/Account/Login` succeeds for a known user

Recommended approach:

- host app in `Testing`
- point the factory at a dedicated PostgreSQL test database
- create the schema inside the test harness
- seed one domain user and one matching Identity user
- assign a known password
- post the login form over HTTP
- assert successful post-login redirect or authenticated follow-up request

## Why this setup exists

Production startup currently performs migration, seeding, and sync reset automatically.

That behaviour is useful for normal app startup, but it makes web tests brittle because:

- the test does not fully control its data
- auth scenarios can fail for unrelated reasons
- rendered-page assertions can be contaminated by extra seeded users or state

Running under `Testing` fixes that by letting the test host own all setup while still exercising the real web application.
