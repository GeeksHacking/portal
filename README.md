# HackOMania Event Platform

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

## Deployment

The HackOMania API is deployed to Google Cloud Run using a **blue-green deployment strategy** for zero-downtime updates.

### Deployment Process

1. **Automatic Trigger**: Deployments are automatically triggered when changes are pushed to the `main` branch that affect:
   - `HackOMania.Api/**`
   - `.github/workflows/deploy-api.yml`

2. **Blue-Green Strategy**:
   - A new revision is deployed with no traffic (`no_traffic: true`)
   - Health checks are validated via Cloud Run probes:
     - **Startup Probe**: `/health` endpoint (30s timeout)
     - **Liveness Probe**: `/alive` endpoint (ongoing monitoring)
   - Traffic is migrated to the new revision only after health checks pass
   - Old revisions are automatically cleaned up (keeping last 3)

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