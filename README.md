# BlogCMS

A multi-user blog content management system built with ASP.NET Core MVC (.NET 8), SQL Server, and ADO.NET — **no ORM**. Designed with a clean three-layer architecture (Data / Business / Web) so each piece is easy to read, test, and review.

---

## Features

### Roles
| Role | Capabilities |
|---|---|
| **Admin**  | Manage users (assign roles, disable accounts), approve/reject posts, moderate comments, full CRUD on any post |
| **Author** | Create / edit / delete own posts, choose status (Draft / Pending), tag posts |
| **Reader** | Browse posts, comment (with replies), like, search/filter |

### Public site
- Paginated post listing with category filter and full-text search (title + content)
- Tag-based browsing
- Single post page with cover image, category, tags, author bio
- Live comment submission via fetch (no full-page reload)
- Threaded replies (one level deep)
- Like / unlike toggle (live)

### Author portal
- Post list with status badges (Draft / Pending / Published / Rejected)
- Rich-form editor for title, excerpt, HTML content, cover image, category, comma-separated tags
- Auto-generated unique slugs

### Admin portal
- Dashboard with counts (users / pending posts / pending comments)
- User management — change role, disable/enable accounts
- Pending posts queue — preview, approve, reject
- Pending comments — approve or delete

---

## Tech stack

- **ASP.NET Core 8 MVC** — Razor views, partials, layouts, tag helpers
- **Cookie authentication** — secure HTTP-only auth cookies, role-based authorization
- **SQL Server / LocalDB** — schema, stored procedures, parameterized queries
- **ADO.NET** with `Microsoft.Data.SqlClient` — no Entity Framework
- **Bootstrap 5** — responsive UI
- **Vanilla JS + fetch API** — live comment submission and like toggle

---

## Architecture

```
BlogCMS/
├── Database/
│   ├── 001_Schema.sql            ← tables, indexes, constraints
│   ├── 002_StoredProcedures.sql  ← sp_GetPostsPaged, sp_GetPostBySlug, etc.
│   └── 003_SeedData.sql          ← demo users, categories, posts, comments
└── src/
    ├── BlogCMS.Data/              ← Data layer (no dependency on Business or Web)
    │   ├── DbConnectionFactory.cs
    │   ├── Entities/              ← POCOs that mirror DB rows
    │   └── Repositories/          ← ADO.NET data access
    │       └── Interfaces/
    ├── BlogCMS.Business/          ← Business layer (depends only on Data)
    │   ├── Services/              ← business rules (auth, post lifecycle, etc.)
    │   ├── DTOs/                  ← input/output records
    │   └── Helpers/               ← PasswordHasher (HMAC-SHA512), SlugGenerator
    └── BlogCMS.Web/               ← MVC Web layer (depends on Business)
        ├── Controllers/
        ├── Views/
        ├── ViewModels/
        ├── wwwroot/
        ├── Program.cs
        └── appsettings.json
```

**Dependency direction is enforced via project references** — Web → Business → Data. The Data layer has no knowledge of MVC, and the Business layer has no knowledge of HTTP.

### Stored procedures

Complex queries live in T-SQL where they belong:
- `sp_GetPostsPaged` — search + category + tag + status + author filtering with pagination, returns rows + total count in one trip
- `sp_GetPostBySlug` — post detail + tags in two result sets
- `sp_GetCommentsForPost` — flat list, ordered for client-side tree assembly
- `sp_GetUserList` — admin user list with post & comment counts
- `sp_GetPendingComments` — moderation queue with post titles joined

Simpler CRUD uses inline parameterized SQL.

---

## Setup

### 1. Database

```bash
sqlcmd -S "(localdb)\mssqllocaldb" -i Database/001_Schema.sql
sqlcmd -S "(localdb)\mssqllocaldb" -i Database/002_StoredProcedures.sql
sqlcmd -S "(localdb)\mssqllocaldb" -i Database/003_SeedData.sql
```

### 2. Run

```bash
cd src/BlogCMS.Web
dotnet run
```

App opens on `http://localhost:5279` (or whichever port `launchSettings.json` picks).

### 3. Demo accounts

All seed users have password **`password123`**:

| Email | Role |
|---|---|
| `admin@blog.test`  | Admin |
| `alice@blog.test`  | Author |
| `bob@blog.test`    | Author |
| `reader@blog.test` | Reader |

---

## Routes

| Path | Auth | Description |
|---|---|---|
| `/` | — | Home / post listing (search, filter, paginate) |
| `/post/{slug}` | — | Single post view with comments |
| `/Account/Login` | — | Sign in |
| `/Account/Register` | — | New reader account (defaults to Reader role) |
| `/Author` | Admin/Author | List own posts |
| `/Author/Create` | Admin/Author | New post form |
| `/Author/Edit/{id}` | Admin/Author | Edit own post |
| `/Admin` | Admin | Dashboard |
| `/Admin/Users` | Admin | User management |
| `/Admin/PendingPosts` | Admin | Approve / reject queue |
| `/Admin/Comments` | Admin | Moderate comments |

### Live JSON endpoints (called from JS)
- `POST /posts/{id}/comments` — add comment, returns rendered data for instant DOM insertion
- `POST /posts/{id}/like` — toggle like, returns `{ liked, count }`

Both protected by anti-forgery tokens.

---

## Security notes

- Passwords hashed with **HMAC-SHA512** + per-user 64-byte random salt
- Cookie auth with HTTP-only flag, 14-day sliding expiration
- Anti-forgery tokens on every state-changing form (`@Html.AntiForgeryToken()`)
- All SQL parameterized — no string concatenation
- Role-based authorization at controller and action level
- Authors can only edit / delete **their own** posts unless they're Admin
- Re-editing a published post by a non-admin author reverts it to Pending
