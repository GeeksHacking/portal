# GeeksHacking Portal

A multi-project solution for building and experimenting with web APIs, frontends, and authentication playgrounds.

## Prerequisites

- .NET SDK 10+ — https://dot.net/download
- Node.js 25+ — https://nodejs.org/
- pnpm 10+ — https://pnpm.io/
- Aspire 13.3+ (used for orchestration) — https://aspire.dev
- Mise (optional runtime manager) — https://mise.jdx.dev

## Quick start

On Windows PowerShell:

```powershell
$env:Parameters__github_client_id = ""
$env:Parameters__github_client_secret = ""
aspire run -d
```

On macOS / Linux (bash/zsh):

```bash
export Parameters__github_client_id=""
export Parameters__github_client_secret=""
aspire run -d
```

Visit the Aspire dashboard link after startup to open the web UI.

## Database migrations

This repository includes a `GeeksHackingPortal.DbMigrator` tool to inspect and apply schema changes. Typical workflow:

```powershell
# show CLI help
dotnet run --project GeeksHackingPortal.DbMigrator -- --help

# show schema diff
dotnet run --project GeeksHackingPortal.DbMigrator -- diff

# apply changes
dotnet run --project GeeksHackingPortal.DbMigrator -- apply

# apply changes and seed development data
dotnet run --project GeeksHackingPortal.DbMigrator -- apply --seed-development-template
```

`apply` will stop on potentially destructive changes unless `--allow-destructive` is specified.

## Dates and times

- The database stores every timestamp as UTC, and the API returns every timestamp as UTC (ISO 8601, e.g. `2026-03-07T01:00:00Z`), regardless of the host's `TZ`.
- Requests may use any offset (the web app submits schedule fields with the event's `+08:00` offset); the API converts them to UTC before storing.
- Presentation is up to the client. The web app formats timestamps in the event time zone (`Asia/Singapore`, see `app/utils/hackathon-date-time.ts`); server-rendered content such as emails uses `EventTimeZone`.

## Participant names

- A participant's name belongs to their user profile (`User.FirstName` / `User.LastName`), not to a hackathon or workshop registration. It is first taken from the GitHub display name and changed with `PATCH /users/me`.
- Users can view and update their profile name at `/dash/profile`.
- Signing up confirms the profile name: the hackathon registration form and the workshop "Start registration" step show it, require both parts, and save any edit back to the profile, so the change applies everywhere (see `app/composables/useProfileName.ts`).
- Organizer views, participant emails and OIDC `given_name` / `family_name` claims all read the profile name.
- Names are never registration answers: the `first_name` and `last_name` question keys are reserved.

## Development

Common helper tasks (see project-specific READMEs for frontend/backend details):

- Start local services and dashboards: `aspire run -d` or use the project `justfile` if present.
- Frontend: see `GeeksHackingPortal.WebApp/README.md`.
- OIDC playground: see `GeeksHackingPortal.OidcWebPlayground/README.md`.


## AI Agents (MCP & A2A)

The API exposes opt-in FastEndpoints as Model Context Protocol tools and A2A skills, protected by authentication:

- MCP endpoint: `/mcp` (requires authentication)
- A2A agent card: `/.well-known/agent-card.json` (requires authentication)
- A2A JSON-RPC: `/a2a` (requires authentication)

Only endpoints that explicitly call `.McpTool()` / `.A2ASkill()` (or use the attributes) are exposed. Visibility is further restricted to authenticated callers. See https://fast-endpoints.com/docs/ai-agents for details.

Example: the `auth/whoami` endpoint is exposed as the `whoami` tool/skill.

## Contributing

Open issues or PRs against this repository. Include migration diffs when proposing schema changes.
