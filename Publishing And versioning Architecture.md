# DraftView Publishing and Versioning Architecture (v3.0)

---

## 1. Purpose

Define how DraftView:

- Tracks changes to prose
- Converts draft changes into versions
- Controls when versions are published
- Communicates updates to readers
- Maintains a stable and predictable reading experience

This document does **not** define full system architecture.  
It defines **Publishing and Versioning behaviour only**.

---

## 2. Core Principles

> DraftView tracks continuously.  
> Authors publish deliberately.  
> Readers see only stable versions.

---

## 3. Data State Model

### 3.1 Working State (Unpublished)

- Continuously updated
- Derived from:
  - Scrivener (via Dropbox), or
  - DraftView native editing
- Not visible to readers

---

### 3.2 Published State (Versioned)

- Immutable
- Reader-facing
- Created only via **Republish**

---

## 4. Version Creation

### 4.1 Core Rule

> Versions are created only via explicit author action.

---

### 4.2 Republish Action

- Button: **Republish Version**
- Always required to create a version
- Never automatic

---

### 4.3 Republish Flow

1. Author initiates republish
2. System prepares:
   - diff
   - change classification
   - AI summary
3. Author reviews
4. Author confirms
5. Version created

---

## 5. Publishing Interface

### 5.1 Entry Point

- Publish / Republish opens a dedicated **Publishing Page**

---

### 5.2 Publishing Scope

Author selects:

- **Whole Chapter**
- **Scene(s)**

---

### 5.3 Versioning Unit

> Scene is the core version unit  
> Chapter is a container and batch publish tool  

---

### 5.4 Publishing Page Layout

Default: Chapter view

Each chapter displays:

- Republish button  
- Revoke button  
- Pending change indicator  

Optional:

- Expand chapter → show scenes with same controls

---

## 6. Pending Change Indicator

### 6.1 Core Rule

> System continuously shows unpublished change state.

---

### 6.2 Display

Per chapter or scene:

- Polish  
- Revision  
- Rewrite  

---

### 6.3 Behaviour

- Updates dynamically as changes accumulate  
- Represents difference between:
  - current working state
  - last published version  
- Advisory only  

---

## 7. Change Classification

### 7.1 Levels

- **Polish** – minor wording changes  
- **Revision** – paragraph-level changes  
- **Rewrite** – structural or narrative changes  

---

### 7.2 Process

- Diff-based (paragraph level)
- AI suggests classification
- Author confirms on publish

---

## 8. Scheduling Model

### 8.1 Core Rule

> Scheduling delays suggestions, not publishing capability.

---

### 8.2 Scope

Scheduling can be applied at:

- Chapter level (default)
- Scene level (optional)

---

### 8.3 Options

- Immediate  
- Every X days  

---

### 8.4 Behaviour

- Changes accumulate during interval  
- Suggestions suppressed during interval  
- Republish always available  

---

## 9. Chapter Locking

### 9.1 Core Rule

> Locked chapters cannot be published.

---

### 9.2 Behaviour

- Republish disabled  
- Changes continue to accumulate  
- No version creation possible  

---

### 9.3 Reader Visibility

Reader sees:

> “Author is revising this chapter”

---

## 10. Version Retention

### 10.1 Free Tier

- Latest + 3 versions retained  
- Author must delete one to create new version  

---

### 10.2 Paid Tier

- Full version history retained  

---

## 11. Version Deletion

### 11.1 Core Rule

> Deletion is permanent.

---

### 11.2 Behaviour

- Version removed completely  
- No recovery  
- Version numbers are not reused  

---

## 12. Revoke Behaviour

### 12.1 Core Rule

> Authors can revoke the latest version.

---

### 12.2 Behaviour

- Rolls back to previous version  
- Updates reader-visible state  
- Does not recreate deleted versions  

---

## 13. Reader Model

### 13.1 Core Rule

> Readers always see the latest version only.

---

### 13.2 No Version Browsing

- Readers cannot:
  - select older versions
  - compare versions manually  

---

## 14. Reader State

- LastReadVersionNumber  
- LastReadAt  

---

## 15. Reader Messaging

| State | Message |
|------|--------|
| Not yet read | “Updated since you last read” |
| Already read | “Updated from the last version” |
| First read | No message |

---

## 16. Update Banner

- Non-blocking top banner  
- Shows:
  - version
  - one-line summary  
- Dismissible  
- Shown once per version  

---

## 17. Version Access

- Version label clickable  
- Reopens banner panel  

---

## 18. Highlighting

- Paragraph-level only  
- Subtle visual styling  
- Based on reader-relative comparison  

---

## 19. AI Summary System

### 19.1 One-Line Summary (Free)

- Always available  
- Context-aware  
- References:
  - characters
  - locations
  - events  

---

### 19.2 Full Summary (Paid)

- 2–4 bullet points  
- Structured  
- No inline diff preview  

---

### 19.3 AI Constraints

Must:
- use diffed content  
- reference real entities  

Must not:
- speculate  
- add filler  

---

### 19.4 AI Usage Model

#### Free
- 1-line summaries unlimited  
- 5–10 full summaries/month  

#### Paid
- full summary history  
- unlimited generation  

---

## 20. System Behaviour Summary

| Event | Action |
|------|--------|
| Changes occur | Working state updated |
| Changes accumulate | Indicator updates |
| Republish | Version created |
| Revoke | Version rolled back |
| Lock active | Publish blocked |
| Schedule active | Suggestion delayed |
| Reader opens | Latest version shown |

---

## 21. Key Constraints

- Republish is the only way to create versions  
- Indicator is advisory only  
- Scheduling never blocks republish  
- Locking blocks publishing only  
- No inline diff preview in AI  
- Scene is core version unit  
- Chapter is publishing container  

---

## 22. Design Outcome

This architecture provides:

- clear author control over publishing  
- minimal version noise  
- consistent reader experience  
- scalable versioning model  
- strong alignment with real writing workflows  

---

## 23. Final Principle

> Show the author what has changed.  
> Let the author decide when it becomes a version.  
> Show the reader only what matters.










# DraftView Publishing & Versioning – Sprint Plan

---

## Guiding Rule

> Each sprint must produce a working, testable behaviour end-to-end.

No foundation-only sprints. Every sprint must deliver visible value.

---

# Sprint 1 — Core Versioning Backbone

## Goal
Prove:
Working state → Republish → Version → Reader sees latest

---

## Scope

### Domain
- Version entity
  - VersionNumber
  - CreatedAt
  - ContentSnapshot
- Chapter + Scene structure
- Reader state:
  - LastReadVersionNumber

---

### Application
- Republish command (chapter-level only)
- No AI
- No classification
- No scheduling
- No locking

---

### Infrastructure
- Store full snapshot per version

---

### Web
- Republish button (simple)
- Reader view shows latest version only

---

## Deliverable

- Author clicks Republish → new version created  
- Reader sees latest version  
- Previous version retained  

---

## Do NOT include

- AI summaries  
- diff highlighting  
- scene-level publishing  
- Dropbox incremental sync  

---

# Sprint 2 — Diff + Highlighting

## Goal
Deliver core differentiator:
> “See what changed”

---

## Scope

### Domain
- Paragraph-level diff engine

---

### Application
- Compare:
  - current vs last-read version

---

### Web
- Highlight changed paragraphs
- Always-on (no settings yet)

---

## Deliverable

- Reader sees highlighted changes since last read

---

## Do NOT include

- AI summaries  
- version classification  
- banner messaging  
- scheduling  

---

# Sprint 3 — Reader Experience Layer

## Goal
Make system usable and intentional

---

## Scope

### Reader State
- Update LastReadVersion when reading

---

### Messaging
- “Updated since you last read”
- “Updated from the last version”

---

### UI
- Top banner (non-blocking)
- Version label clickable

---

## Deliverable

- Banner appears once per version  
- Can dismiss  
- Can reopen via version label  

---

# Sprint 4 — Pending Change Indicator + Classification

## Goal
Give authors visibility before publishing

---

## Scope

### Domain
- Change classification:
  - Polish
  - Revision
  - Rewrite

---

### Application
- Continuous evaluation of:
  - working vs published

---

### Web
- Indicator next to Republish button
- Colour + label only

---

## Deliverable

- Author sees live change level before publishing

---

## Do NOT include

- AI summaries  
- scheduling  
- locking  

---

# Sprint 5 — AI Summary System

## Goal
Add value without destabilising system

---

## Scope

### Application
- Generate:
  - 1-line summary
  - full summary (limited)

---

### Constraints
- No inline diff preview  
- Must reference actual text entities  

---

### Web
- Show summary on publish page
- Allow author editing

---

## Deliverable

- Author sees AI summary before publish  
- Reader sees 1-line summary  

---

# Sprint 6 — Scene-Level Publishing

## Goal
Enable granular publishing

---

## Scope

### Domain
- Scene as version unit

---

### Application
- Publish selected scenes

---

### Web
- Publish page:
  - chapter list
  - expandable scenes
  - per-scene republish

---

## Deliverable

- Author can publish:
  - whole chapter  
  - or selected scenes  

---

# Sprint 7 — Scheduling + Locking

## Goal
Control version noise

---

## Scope

### Scheduling
- Per chapter (default)
- Optional per scene

---

### Locking
- Chapter lock blocks publish
- Reader sees:
  - “Author is revising this chapter”

---

## Deliverable

- Author can:
  - pause publishing  
  - limit publish frequency  

---

# Sprint 8 — Dropbox Incremental Sync

## Goal
Scalable ingestion

---

## Scope

### Infrastructure
- Incremental change ingestion
- Full sync background task

---

### Application
- Update working state only

---

## Deliverable

- Efficient sync  
- No impact on publishing  

---

# Sprint 9 — Version Retention + Deletion

## Goal
Enforce pricing model

---

## Scope

### Domain
- Version retention rules

---

### Application
- Require deletion when limit exceeded

---

### Web
- Version selection UI before publish

---

## Deliverable

- Free tier:
  - latest + 3 versions  
- Controlled deletion flow  

---

# Sprint Order Summary

1. Core versioning  
2. Diff + highlighting  
3. Reader UX  
4. Change indicator  
5. AI summaries  
6. Scene publishing  
7. Scheduling + locking  
8. Sync improvements  
9. Retention rules  

---

# Critical Discipline Rules

- Do not mix sprint scope  
- Do not “just add one more thing”  
- Each sprint must end:
  - working  
  - testable  
  - stable  

---

# Outcome Timeline

- Sprint 2 → core value visible  
- Sprint 3–4 → product feels real  
- Sprint 5–6 → monetisation-ready  

---

# Final Principle

> Build thin, complete slices.  
> Validate behaviour early.  
> Never build ahead of proof.