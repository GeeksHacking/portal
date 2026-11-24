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

## Header images (Cloudflare R2)

- Hackathons, hackathon workshops and standalone workshops can have a header image, stored in an S3-compatible bucket (Cloudflare R2 in production, RustFS locally). Every list/get response for them includes `headerImageUrl` (`null` when unset).
- Only admins (`App:AdminEmails` / `App:AdminGitHubLogins`) can change images. The browser uploads straight to the bucket; the API never handles the file:
  1. `POST /admin/activities/{activityId}/header-image/upload-url` with `{ contentType, contentLength }` (JPEG, PNG or WebP, max 5 MB) returns a presigned `uploadUrl` (valid 10 minutes) and a `key`.
  2. `PUT` the file to `uploadUrl` with the same `Content-Type` header.
  3. `PUT /admin/activities/{activityId}/header-image` with `{ key }`. The API checks the object's size and file signature (deleting it if invalid), then sets it and deletes the previous image.
  - `DELETE /admin/activities/{activityId}/header-image` removes the image.
- The bucket is read publicly via its custom domain / r2.dev URL; the R2 API token (Object Read & Write, scoped to this bucket) lives only in the API.
- API configuration: `R2:AccountId` (or `R2:ServiceUrl` for another S3 endpoint), `R2:WriteSecrets` (JSON `{"accessKeyId": "...", "secretAccessKey": "..."}`; the only way to pass credentials), `R2:BucketName`, `R2:PublicBaseUrl`. Without them, uploads return 503.
- Locally, `aspire run` starts a RustFS container (`storage`, console login `rustfsadmin` / `rustfsadmin`) and the API creates the `portal` bucket with a public-read policy on startup (`R2:EnsureBucket`). No setup needed.
- Production: the Pulumi stack reads Secret Manager secret `r2-write-secrets` (injected as `R2__WriteSecrets`) and the `r2:accountId`, `r2:bucketName`, `r2:publicBaseUrl` config in `GeeksHackingPortal.Infra/Pulumi.production.yaml`.
- The R2 bucket needs a CORS rule so browsers can upload (Cloudflare dashboard → R2 → bucket → Settings → CORS policy):

  ```json
  [
    {
      "AllowedOrigins": ["https://portal.geekshacking.com"],
      "AllowedMethods": ["PUT"],
      "AllowedHeaders": ["Content-Type"],
      "MaxAgeSeconds": 3600
    }
  ]
  ```

- Uploads that are never confirmed (e.g. the tab is closed between steps 2 and 3) stay in the bucket. An R2 lifecycle rule can clean them up if that becomes a problem.

## Development

Common helper tasks (see project-specific READMEs for frontend/backend details):

- Start local services and dashboards: `aspire run -d` or use the project `justfile` if present.
- Frontend: see `GeeksHackingPortal.WebApp/README.md`.
- OIDC playground: see `GeeksHackingPortal.OidcWebPlayground/README.md`.

## Contributing

Open issues or PRs against this repository. Include migration diffs when proposing schema changes.
