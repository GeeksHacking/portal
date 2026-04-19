# GeeksHacking Portal 

????

## Getting started

[Aspire](https://aspire.dev) is used to bootstrap the project. These can be installed [here](https://aspire.dev).

[.NET](https://dot.net) is used for the API. It can be installed [here](https://dot.net/download).

Along with Aspire, you also need NodeJS and PNPM. These can be installed using [mise](https://mise.jdx.dev).

After installing the above, you can simply run:

```shell
export Parameters__github_client_id=""
export Parameters__github_client_secret=""
aspire run -d
```

And visit the dashboard link with everything setup!

The API now fails fast on startup if the live database schema differs from the current SqlSugar model. The dedicated `GeeksHackingPortal.DbMigrator` service is used to inspect and apply those changes before the API starts.

Typical workflow:

```shell
dotnet run --project GeeksHackingPortal.DbMigrator -- --help
dotnet run --project GeeksHackingPortal.DbMigrator -- diff
dotnet run --project GeeksHackingPortal.DbMigrator -- apply
dotnet run --project GeeksHackingPortal.DbMigrator -- apply --seed-development-template
```

The `DbMigrator` CLI uses `XenoAtom.CommandLine` for its command surface. `diff` uses SqlSugar `GetDifferenceTables(...).ToDiffString()` to show the schema drift. `apply` runs `InitTables(...)` after review. If a change would delete columns, `apply` stops unless you explicitly pass `--allow-destructive`.

`--seed-development-template` seeds the local development template data after `apply`. The Aspire AppHost uses that flag for local orchestration, while CI/deploy does not.

The deploy workflow publishes the schema diff in the job summary and as an artifact. When changes are detected, the `apply-database-changes` job waits on the protected `production-database` GitHub environment, so required reviewers can inspect the diff before approving the schema update.

## Deployment

The HackOMania API is deployed to Google Cloud Run with **zero-downtime updates** using health probes.

### Deployment Process

1. **Automatic Trigger**: Deployments are automatically triggered when changes are pushed to the `main` branch that affect:
   - `HackOMania.Api/**`
   - `.github/workflows/deploy-api.yml`

2. **Cloud Run Health Probes**:
   - **Startup Probe**: `/health` endpoint validates container readiness before accepting traffic (30s timeout)
   - **Liveness Probe**: `/alive` endpoint continuously monitors container health (10s intervals)
   - Cloud Run automatically handles traffic migration only after health checks pass
   - Failed deployments are automatically rejected without affecting production traffic

3. **Health Check Endpoints**:
   - `/health` - Full readiness check (all registered health checks must pass)
   - `/alive` - Liveness check (only "live" tagged checks)

4. **Infrastructure**: 
   - Infrastructure is managed using Pulumi (C#-based IaC)
   - Cloud Run service configuration is in `HackOMania.Infra/Stacks/DefaultStack.cs`
   - Run `pulumi up` in the `HackOMania.Infra` directory to update infrastructure

### Manual Deployment

To manually trigger a deployment, use the GitHub Actions workflow dispatch:
- Go to Actions → Deploy (API) → Run workflow

## Development

For simplicity, a `justfile` is provided with common scripts.

`just dev` starts the processes.

`just codegen` generates new Kiota API clients based on the OpenAPI schema.
