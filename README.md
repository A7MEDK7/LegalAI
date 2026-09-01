# AI-Driven Legal Precedent System for Banking

An internal platform that lets a bank's legal department upload legal documents, run semantic search over legal precedents, and manage legal opinions (fatwas) through a complete AI-assisted drafting and approval workflow — built with **.NET 8** on **Onion Architecture**, integrated with an external **AI Legal RAG service** for semantic search and knowledge-base indexing.

> This README reflects the state of the backend as of the latest revision. Frontend and some AI-service capabilities are still in progress — see [Roadmap](#roadmap--known-issues) below.

---

## Table of Contents

- [Overview](#overview)
- [Tech Stack](#tech-stack)
- [Architecture](#architecture)
- [Project Structure](#project-structure)
- [User Roles](#user-roles)
- [Domain Model](#domain-model)
- [Features](#features)
  - [Authentication & Identity](#1-authentication--identity)
  - [Password Reset](#2-password-reset)
  - [Admin User Management](#3-admin-user-management)
  - [Legal Opinion Workflow](#4-legal-opinion-workflow)
  - [AI Service Integration](#5-ai-service-integration)
- [API Reference (Summary)](#api-reference-summary)
- [Getting Started](#getting-started)
- [AI Service Setup (Docker)](#ai-service-setup-docker)
- [Test Accounts](#test-accounts)
- [Roadmap / Known Issues](#roadmap--known-issues)

---

## Overview

Four roles use the system, each with a distinct set of capabilities:

| Role | Can do |
|---|---|
| **Lawyer** | Search precedents, ask the AI knowledge base questions, view/download documents |
| **Legal Advisor** | Everything a Lawyer can, plus: draft/submit/resubmit legal opinions, manage unanswered questions |
| **Legal Manager** | Review submitted legal opinions: approve, reject, or request revision |
| **System Administrator** | Manage users (create/block/activate/delete), platform administration |

The system is split into two independently deployable services:

```
┌──────────────────────┐        HTTP         ┌──────────────────────┐
│   ASP.NET Backend      │ ───────────────────▶ │   AI Legal RAG         │
│   (this repository)    │                       │   Service (Python,     │
│                         │ ◀─────────────────── │   FastAPI, Docker)      │
└──────────────────────┘                       └──────────────────────┘
   Auth, Users, Legal Opinions,                  Embeddings, Vector Search
   Documents, Business Rules, DB                 (Qdrant), LLM Generation (Ollama)
```

---

## Tech Stack

- **.NET 8** / ASP.NET Core Web API
- **Entity Framework Core** (Code-First) with **SQL Server**
- **ASP.NET Core Identity** for authentication and role-based authorization
- **Redis** for caching (OTP codes, rate limiting)
- **AutoMapper** for entity-to-DTO mapping
- **JWT Bearer** authentication
- **Onion Architecture**: Domain → Persistence/Service → Presentation
- **Generic Repository + Specification + Unit of Work** patterns for data access
- External **AI Legal RAG service** (Python, FastAPI, Qdrant, Ollama) via HTTP

---

## Architecture

The project follows **Onion Architecture**, with dependencies flowing inward only:

```
Presentation → Service → Domain
Persistence  → implements Domain contracts
```

- **Domain** knows nothing about any other layer — just Entities, Enums, Contracts (interfaces), and Exceptions.
- **Service** contains business logic and depends only on Domain.
- **Persistence** implements Domain contracts (repositories, DbContext, configurations).
- **Presentation** (Controllers) depends on Service, never talks to Persistence directly.

---

## Project Structure

```
Core/
  Domain/                   → Entities, Enums, Contracts, Exceptions
  Service/                  → Business logic, Specifications
  Service.Abstraction/      → Service interfaces (Contracts)
Infrastructure/
  Persistence/              → DbContext, Configurations, Repositories, Migrations
LegalAI/                    → API host: Controllers, Program.cs, appsettings.json
Shared/                     → DTOs, Settings, cross-cutting types shared across layers
```

---

## User Roles

Seeded automatically on first run by `DbInitilazer`:

| Role |
|---|
| Lawyer |
| Legal Advisor |
| Legal Manager |
| System Administrator |

See [Test Accounts](#test-accounts) for ready-to-use credentials for each role.

---

## Domain Model

### Core Entities

| Entity | Purpose |
|---|---|
| `ApplicationUser` | Extends `IdentityUser<int>`; adds FirstName, LastName, Address, Governorate, IsActive, ProfilePicture |
| `LegalDocument` | An uploaded/generated legal document (PDF/DOCX/TXT), tracked through Processing → Indexed/Failed |
| `LegalOpinion` | A legal opinion (Title, Question, Answer) authored by a Legal Advisor, going through a review cycle |
| `OpinionReview` | One review decision (Approved/Rejected/RevisionRequired) recorded against a `LegalOpinion` |
| `UnknownQuestion` | A question asked via `/Ask` that the AI service had no answer for |

### Key Enums

```csharp
public enum DocumentStatus {
    Processing = 1,
    Failed = 2,
    Indexed = 3
}

public enum OpinionStatus {
    Submitted = 1,
    Approved = 2,
    Rejected = 3,
    RevisionRequired = 4
}

public enum ReviewDecision {
    Approved = 1,
    Rejected = 2,
    RevisionRequired = 3
}
```

### Relationships

- `LegalOpinion` ↔ `LegalDocument`: **one-to-one, optional** (`LegalOpinion.LegalDocument`, FK `LegalDocument.LegalOpinionId`, `DeleteBehavior.SetNull`).
- `LegalOpinion` → `OpinionReview`: **one-to-many** (`DeleteBehavior.Cascade` — deleting an opinion deletes its review history).
- `ApplicationUser` → `LegalDocument` / `LegalOpinion` / `OpinionReview` / `UnknownQuestion`: **one-to-many** (`DeleteBehavior.Restrict` — a user with linked records cannot be deleted).

---

## Features

### 1. Authentication & Identity

- JWT-based login (`POST /api/Authentication/Login`), issuing a token with `NameIdentifier`, `Name`, `Email`, and role claims.
- Full profile management: view/update profile, upload/delete profile picture, change email (OTP-verified).
- Custom password policy: minimum 8 characters, at least one digit (no uppercase/lowercase/special-character requirement).

### 2. Password Reset

Three-step forgot-password flow, OTP-based via Redis:

1. `POST /api/Password/RequestReset` — emails a 6-digit code (5-minute expiry, rate-limited to one active code at a time).
2. `POST /api/Password/ConfirmReset` — verifies the code, returns a single-use Identity reset token.
3. `POST /api/Password/ResetPassword` — applies the new password using that token.

### 3. Admin User Management

System Administrators can:
- List, search (by name/phone), and filter users by role.
- View a user's full activity (uploaded documents, created/reviewed opinions, review count).
- Onboard new users via an OTP-gated flow (`RequestAddNewUser` → `AddNewUser`).
- Block / activate / delete users, with dependency checks (e.g. a Legal Advisor with linked opinions/documents cannot be deleted) and protection against modifying System Administrator accounts.

### 4. Legal Opinion Workflow

The core business workflow, spanning four roles:

**Ask Flow** (Lawyer, Legal Advisor)
Ask a natural-language legal question. If the AI service finds an answer, it's returned directly. If not, the question is automatically logged under **Unknown Questions** for a Legal Advisor to turn into a formal opinion later.

**Create Flow** (Legal Advisor)
Submit a new opinion (Title, Question, Answer, optional supporting file). Saved as `Submitted` and enters the review queue.

**Review Flow** (Legal Manager)
Review any submitted opinion and record one of three decisions:
- **Approved** → triggers AI indexing (see below).
- **Rejected** → the attached file is deleted from disk; the opinion is kept as a closed historical record.
- **Revision Required** → returns to the original Advisor with the reviewer's comment; the Advisor edits and resubmits (`PUT /{id}/Resubmit`), re-entering the queue at `Submitted`.

**Delete Flow** (Legal Advisor, Legal Manager)
Delete a *not-yet-approved* opinion and its attached file. Advisors may delete only their own; Managers may delete any. Approved opinions cannot be deleted (see [AI Service Integration](#5-ai-service-integration)).

### 5. AI Service Integration

On **approval only** (never at creation time), approved content is pushed to the external AI service so future `/Ask` queries can find it:

1. The opinion's attached document (if any) is sent as-is.
2. If the opinion has a non-empty `Answer`, a Question/Answer text file is generated on the fly, saved to disk, and sent as a second document.
3. Both operations are **all-or-nothing**: if either indexing call fails, the whole approval rolls back — no partial database state, no orphaned files left on disk.

Because approved content is indexed externally with no delete/undo capability on the AI service's side yet, **approved opinions cannot be deleted** — doing so would leave the AI's knowledge base referencing content with no corresponding backend record.

---

## API Reference (Summary)

> Full request/response shapes are documented separately for the frontend team. Below is the endpoint map.

### `api/Authentication`

| Method | Route | Auth |
|---|---|---|
| POST | `/Login` | Public |
| GET | `/Profile` | Authenticated |
| PUT | `/Profile` | Authenticated |
| PATCH | `/Picture` | Authenticated |
| POST | `/RequestChangeEmail` | Authenticated |
| POST | `/ConfirmEmailChange` | Authenticated |

### `api/Password`

| Method | Route | Auth |
|---|---|---|
| POST | `/RequestReset` | Public |
| POST | `/ConfirmReset` | Public |
| POST | `/ResetPassword` | Public |

### `api/LegalOpinion`

| Method | Route | Policy |
|---|---|---|
| POST | `/Ask` | AdvisorOrLawyer |
| GET | `/Document-By-Source` | AdvisorOrLawyer |
| POST | `/` (Create) | LegalAdvisor |
| GET | `/Legal-Advisor-Opinions` | LegalAdvisor |
| PUT | `/{id}/Resubmit` | LegalAdvisor |
| GET | `/Unknown-Questions` | LegalAdvisor |
| DELETE | `/Unknown-Questions/{id}` | LegalAdvisor |
| GET | `/` | LegalManager |
| GET | `/{id}` | AdvisorOrManger |
| POST | `/{id}/Review` | LegalManager |
| DELETE | `/{id}` | AdvisorOrManger |

Authorization uses **named policies** (defined once in `Program.cs`) rather than inline role strings:

```csharp
builder.Services.AddAuthorization(options => {
    options.AddPolicy("SystemAdministrator", policy => policy.RequireRole("System Administrator"));
    options.AddPolicy("LegalManager", policy => policy.RequireRole("Legal Manager"));
    options.AddPolicy("LegalAdvisor", policy => policy.RequireRole("Legal Advisor"));
    options.AddPolicy("AdvisorOrManger", policy => policy.RequireRole("Legal Advisor", "Legal Manager"));
    options.AddPolicy("AdvisorOrLawyer", policy => policy.RequireRole("Legal Advisor", "Lawyer"));
});
```

---

## Getting Started

### Prerequisites

- .NET 8 SDK
- SQL Server (local or containerized)
- Redis
- (Optional, for the Legal Opinion AI features) the AI Legal RAG service — see [AI Service Setup](#ai-service-setup-docker)

### Configuration

Fill in `appsettings.json` (or better, use `dotnet user-secrets` for anything sensitive):

```json
{
  "ConnectionStrings": {
    "DefaultConnections": "Server=...;Database=LegalAIDatabase;...",
    "Redis": "localhost:6379"
  },
  "JwtOptions": {
    "SecretKey": "...",
    "Issuer": "https://localhost:7265/",
    "Audience": "MyAudience",
    "DurationInDays": 3
  },
  "FileStorage": {
    "RootPath": "D:\\Full Stack (.NET Developer)\\Dotnet Projects\\Intern\\LegalAI-Storage\\Documents"
  },
  "LegalAiService": {
    "BaseUrl": "http://localhost:8000",
    "TimeoutSeconds": 150
  },
  "ApiBaseUrl": "https://localhost:7265/"
}
```

> ⚠️ **Never commit real secrets.** The SQL password, JWT secret key, and SMTP credentials should come from User Secrets or environment variables, not from a committed `appsettings.json`.

### Run

```bash
dotnet restore
dotnet ef database update      # applies pending migrations
dotnet run --project LegalAI
```

On first run, `DbInitilazer` seeds the four roles and one test user per role (see [Test Accounts](#test-accounts)).

Swagger UI is available in the Development environment at `/swagger`.

---

## AI Service Setup (Docker)

The Legal Opinion workflow's `/Ask` and approval-time indexing depend on the external AI Legal RAG service, distributed as a Docker image.

```bash
# Pull the image (check the registry for the current tag)
docker pull <registry>/ai-legal-rag-api:<tag>

# Bring up the full stack: API + Qdrant (vector DB) + Ollama (local LLM)
docker compose up -d

# One-time: pull the LLM model into the Ollama container
docker exec -it ai-legal-ollama ollama pull qwen2.5:3b

# Verify
curl http://localhost:8000/health
```

The backend talks to this service over plain HTTP via a typed `HttpClient` (`ILegalAiService`), configured through the `LegalAiService` section above. See [`docker-compose.yml`](./docker-compose.yml) for the full stack definition (API + Qdrant + Ollama).

---

## Test Accounts

All seeded with the same password for convenience during development:

| Role | Email | Password |
|---|---|---|
| Lawyer | `Ahmed@gmail.com` | `Passw0rd123!` |
| Legal Advisor | `Ali@gmail.com` | `Passw0rd123!` |
| Legal Manager | `Khaled@gmail.com` | `Passw0rd123!` |
| System Administrator | `Ahmeddd@gmail.com` | `Passw0rd123!` |

> ⚠️ This seeding must be disabled before any staging/production deployment.

---

## Roadmap / Known Issues

**In progress**
- Document upload/reprocessing outside the Legal Opinion flow (Knowledge Base Management).
- Semantic search UI (Search Precedents).
- Audit log querying (audit *writing* is not yet implemented at all).

**Known issues to fix**
- `AiServiceUnavailableException` → 503 is mapped in the global error handler; `FailedToAddDocumentException` is not yet mapped and currently falls through to a generic 500.
- A few identifiers have long-standing typos that are now baked into the API surface (`DbInitilazer`, `GlobalErrorHandelingMiddleware`, `ServiceManger`) — left as-is to avoid a wider breaking rename; new code should not copy the misspellings.
- `SpecificationsEvaluator.QetQuery` should be `GetQuery`.
- Login currently reveals whether an email exists via differing error messages (`"Email Is Not Exist"` vs. the generic `"Invalid Email Or Password"`) — a user-enumeration risk worth closing.
- No attempt-limiting on the *confirm* step of OTP flows (password reset / email change) — request-level rate limiting exists, but a valid issued code can still be brute-forced within its 5-minute window.
- The generated Q&A `LegalDocument` (created on opinion approval) has no foreign key back to its `LegalOpinion` — discoverable only through the AI service's own index.

**Security — before any non-development deployment**
- Rotate and relocate the JWT secret key, SQL Server password, and SMTP credentials out of `appsettings.json` into User Secrets / environment variables / a secrets manager.
- Disable the Identity seeding in `DbInitilazer` for Production.
