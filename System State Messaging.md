# Sprint Scope

Implement a single global system state message that is:

- created and edited by SystemSupport
- visible to all users
- rendered in the footer via _Layout.cshtml
- optionally expiring, defaulting to no expiry
- never deleted from history
- auto-replacing the previous active message
- safe to fail with log + ignore

TDD applies to Domain, Application, and Infrastructure.

---

# Locked Decisions

- Active model: single active message  
- New message behaviour: auto-revoke previous active message  
- History: retain all messages  
- History sort: latest first  
- Expiry: optional, default “don’t expire”  
- Visibility: all users  
- Placement: footer in _Layout.cshtml  
- Failure behaviour: log + ignore  
- Editing: allowed by Support User  

---

# Recommendation on Severity

Not necessary for this sprint.

Reason:

- you only have one active message  
- footer space is small  
- no escalation workflow exists yet  

Adding severity now would force:

- enum/domain rules  
- styling variants  
- support UI choices  
- display logic  
- tests  

That is cost without present need. Leave it out unless you already know you want:

- informational  
- degraded service  
- outage  

---

# Sprint Tasks

## 1. Identity and Role

- Add SystemSupport role  
- Seed support@draftview.co.uk  
- Ensure role claim is present in auth  

---

## 2. Domain

Create a SystemStateMessage aggregate/entity with rules for:

- create  
- edit  
- revoke  
- expire  
- determine active status  

Fields:

- Id  
- Message  
- CreatedAtUtc  
- CreatedByUserId  
- UpdatedAtUtc nullable  
- UpdatedByUserId nullable  
- Revoked bool  
- RevokedAtUtc nullable  
- RevokedByUserId nullable  
- ExpiresAtUtc nullable  

---

## 3. Domain Tests first

Minimum behaviours:

- new message is active when not revoked and not expired  
- message with no expiry stays active  
- expired message is inactive  
- revoked message is inactive  
- editing changes message content and update audit fields  
- revoking sets revoke audit fields  

---

## 4. Application Layer

Add service/use cases:

- CreateSystemStateMessageAsync  
- EditSystemStateMessageAsync  
- RevokeSystemStateMessageAsync  
- GetActiveSystemStateMessageAsync  
- GetSystemStateHistoryAsync  

Rule:

- creating a new active message revokes the prior active message in the same transaction  

---

## 5. Application Tests first

Minimum behaviours:

- creating message revokes previous active message  
- only support user can create/edit/revoke  
- active message query ignores revoked  
- active message query ignores expired  
- history returns latest first  

---

## 6. Infrastructure

- EF configuration  
- migration  
- repository methods for:  
  - current active message  
  - history latest first  
  - by id  
- indexes on:  
  - Revoked  
  - ExpiresAtUtc  
  - CreatedAtUtc  

---

## 7. Infrastructure Tests first

Minimum behaviours:

- active query returns correct message  
- expired message excluded  
- revoked message excluded  
- history sorted latest first  

---

## 8. Web

- add SupportController  
- add [Authorize(Roles = "SystemSupport")]  
- add GetSupportAsync() helper  
- redirect non-support users safely  
- support page for:  
  - current active message  
  - create new message  
  - edit current message  
  - revoke current message  
  - history list  

---

## 9. Footer Integration

- _Layout.cshtml displays the active message in footer  
- if none exists, footer falls back cleanly  
- if lookup fails, log + render without message  

---

## 10. Caching

Optional for this sprint unless you want it now.

My recommendation:

- skip cache this sprint  
- add only if profiling or production load justifies it  

Reason:

- one footer lookup  
- single active record  
- premature cache invalidation logic adds risk  

---

# Architecturally Important Notes

## Editing allowed

This is fine, but it changes the audit model.

You need to decide whether:

- editing mutates the same row, or  
- editing creates a new row and revokes the old one  

Your decision:

- editing creates a new row and revokes the old one  

This provides a full audit trail.

---

## Support access

Right now I am assuming:

- only SystemSupport may manage system state messages  

That is the cleanest rule for this sprint.

# Sprint Definition — System State Messaging

## Goal

Implement a **single global system state message** that is:

- Created and managed by `SystemSupport`
- Visible to all users
- Rendered in the footer (`_Layout.cshtml`)
- Optionally expiring (default: no expiry)
- Fully auditable (no destructive edits)
- Safe to fail (log + ignore)

---

## Key Behaviour Decisions (Locked)

- Active model: **Single active message**
- New message: **Auto-revokes previous active message**
- History: **All messages retained**
- Ordering: **Latest first**
- Expiry: **Optional, default no expiry**
- Visibility: **Global (all users)**
- Placement: **Footer**
- Failure behaviour: **Log + ignore**
- Editing model: **Create new row + auto-revoke previous**

---

## Audit Model (Confirmed)

Yes — this **does provide a proper audit trail**.

Each change results in:
- A **new record**
- The previous active message being:
  - Marked `Revoked = true`
  - Given `RevokedAtUtc`
  - Given `RevokedByUserId`

This gives you:
- Full history of all messages
- Who created each message
- Who revoked each message
- When transitions occurred

No data is lost. No mutation ambiguity.

---

## Domain Model

### Entity: `SystemStateMessage`

Fields:

- `Id`
- `Message`
- `CreatedAtUtc`
- `CreatedByUserId`
- `Revoked` (bool)
- `RevokedAtUtc` (nullable)
- `RevokedByUserId` (nullable)
- `ExpiresAtUtc` (nullable)

---

## Domain Rules

- A message is **Active** if:
  - `Revoked == false`
  - `ExpiresAtUtc == null OR ExpiresAtUtc > now`

- Only **one active message** exists at any time

- Creating a new message:
  - Revokes the current active message (if any)
  - Inserts new message as active

- Messages are **never deleted**

---

## Application Layer (Use Cases)

### CreateSystemStateMessage
- Validate caller is `SystemSupport`
- Revoke existing active message (if present)
- Create new message
- Save in single transaction

### RevokeSystemStateMessage
- Mark message as revoked
- Set audit fields

### GetActiveSystemStateMessage
- Return single active message (or null)

### GetSystemStateHistory
- Return all messages ordered by `CreatedAtUtc DESC`

---

## Infrastructure

- EF Core configuration
- Migration for `SystemStateMessage`
- Indexes:
  - `Revoked`
  - `ExpiresAtUtc`
  - `CreatedAtUtc`

---

## Web Layer

### SupportController

- `[Authorize(Roles = "SystemSupport")]`

Helper:
```csharp
private async Task<User?> GetSupportAsync()
{
    var email = User.Identity?.Name;
    if (email is null) return null;

    var user = await userRepo.GetByEmailAsync(email);
    return user?.Role == Role.SystemSupport ? user : null;
}