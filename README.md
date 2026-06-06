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

## Development

Common helper tasks (see project-specific READMEs for frontend/backend details):

- Start local services and dashboards: `aspire run -d` or use the project `justfile` if present.
- Frontend: see `GeeksHackingPortal.WebApp/README.md`.
- OIDC playground: see `GeeksHackingPortal.OidcWebPlayground/README.md`.

## Contributing

Open issues or PRs against this repository. Include migration diffs when proposing schema changes.

