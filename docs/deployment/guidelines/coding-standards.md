# Coding Standards

Conventions for writing AgenticAi delivery assets: Dockerfiles, Compose files, Azure DevOps pipelines, and scripts. Examples are drawn from the adopted reference model. These are style and correctness rules; the absolute constraints they support live in `hard-rules.md`.

---

## General formatting

- **No whitespace padding for visual alignment.** Use a single space around `=` and after `:`. Do not align values into columns with extra spaces — it creates noisy diffs.

  ```yaml
  # correct
  ASPNETCORE_ENVIRONMENT: ${ASPNETCORE_ENVIRONMENT}
  ASPNETCORE_URLS: ${ASPNETCORE_URLS}

  # wrong — padded for alignment
  ASPNETCORE_ENVIRONMENT:   ${ASPNETCORE_ENVIRONMENT}
  ASPNETCORE_URLS:          ${ASPNETCORE_URLS}
  ```

- **Two-space indentation** in all YAML. No tabs.
- **Comments are purposeful.** Use them to mark sections and explain non-obvious choices, not to restate the obvious. Prefer English; do not leave mixed-language comments in shared files.
- **One trailing newline** at end of file; no trailing whitespace on lines.

---

## Dockerfiles

### Multi-stage pattern (.NET)

Every .NET service Dockerfile uses four stages — `base`, `build`, `publish`, `final` — matching the reference:

```dockerfile
# base — runtime image, non-root user, ports
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# build — SDK image, restore then build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["api/src/adapters/Adapter.Restful/Adapter.Restful.csproj", "api/src/adapters/Adapter.Restful/"]
RUN dotnet restore "./api/src/adapters/Adapter.Restful/Adapter.Restful.csproj"
COPY . .
WORKDIR "/src/api/src/adapters/Adapter.Restful"
RUN dotnet build "./Adapter.Restful.csproj" -c $BUILD_CONFIGURATION -o /app/build

# publish
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Adapter.Restful.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# final — copy published output onto the non-root runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Adapter.Restful.dll"]
```

Rules:

- **Runtime base by host type.** HTTP hosts use `dotnet/aspnet:10.0`; worker/background hosts use `dotnet/runtime:10.0`. Both build stages use `dotnet/sdk:10.0`.
- **Non-root final stage.** The runtime stage sets `USER $APP_UID` (from the base image). The `final` stage inherits `FROM base`, so it stays non-root. Never add `USER root` to the final stage.
- **Layered restore for cache efficiency.** Copy **only the `.csproj` files** the target project needs, `RUN dotnet restore`, and *then* `COPY . .`. This keeps the expensive restore layer cached until a project's dependencies actually change. When a project references others, copy each referenced `.csproj` before the restore (mirror the reference RestApi Dockerfile, which copies every project in the dependency graph).
- **`/p:UseAppHost=false`** on publish — no native apphost is needed; the container runs `dotnet <dll>`.
- **Build context is the repo root** (set in Compose), so `COPY` paths are repo-relative (`api/src/...`).

### Multi-stage pattern (Node — future web adapter)

Three stages — `deps`, `builder`, `runner`:

- `deps`: copy `package.json` + lockfile, install.
- `builder`: bring in `node_modules`, `COPY . .`, receive build-time config via `ARG`, promote needed ones to `ENV` for the build, then `npm run build`.
- `runner`: `ENV NODE_ENV=production`, create a dedicated non-root user/group, `COPY --chown` the build output, `USER <nonroot>`, then `CMD`.

Build-time-only values arrive as `ARG` (passed from Compose `build.args`); runtime values arrive as container `environment`. Never bake a secret into an image layer.

### `.dockerignore`

A single root `.dockerignore` governs every build context. It **must** exclude at least `**/.env`, `**/.git`, `**/bin`, `**/obj`, `**/node_modules`, `**/.vs`, and `**/docker-compose*` / `**/Dockerfile*`, so secrets, VCS data, and build detritus never enter a build context. See `security.md`.

---

## Compose files

- **Base is fully parameterized.** In `docker-compose.yml`, every environment value is `${VAR}` resolved from `.env`. Only structural facts are literal (service name, `build.context`, `dockerfile`, `networks`, `restart`, volume names).

  ```yaml
  services:
    adapter.restful:
      image: ${DOCKER_REGISTRY-}adapterrestful
      build:
        context: .
        dockerfile: api/src/adapters/Adapter.Restful/Dockerfile
      environment:
        ASPNETCORE_ENVIRONMENT: ${ASPNETCORE_ENVIRONMENT}
        ASPNETCORE_URLS: ${ASPNETCORE_URLS}
      restart: unless-stopped
      networks:
        - agenticai-network
  ```

- **Override holds development deltas only, and it overrides the base.** Docker Compose automatically merges `docker-compose.override.yml` on top of `docker-compose.yml` for local `docker compose` runs, and any value in the override **supersedes** the matching value in the base (see the merge semantics in `architecture.md`). Put here only **throwaway** development values (a local `ASPNETCORE_ENVIRONMENT=Development`, dev ports, a dev database container, user-secret/HTTPS cert mounts). Never place a production or real secret here. Production deploys run `docker compose -f docker-compose.yml ...` explicitly, so the override is never applied there.
- **`restart: unless-stopped`** for long-running services.
- **Single bridge network**, named for the project (e.g. `agenticai-network`); every service joins it.
- **Named volumes** for stateful services only; declare them in the top-level `volumes:` block.
- **`depends_on`** expresses start ordering between services; keep it accurate as services are added.

### Environment variable naming

Two distinct conventions, bridged by Compose:

| Layer | Convention | Example |
|---|---|---|
| `.env` keys | flat, `UPPER_SNAKE_CASE`, single underscore | `ASPNETCORE_ENVIRONMENT`, a connection-string key |
| Container env → .NET config | `SECTION__SUBSECTION__KEY` (double underscore) | `CONNECTIONSTRINGS__SQL`, `AUTHENTICATION__AUDIENCE` |

The double underscore (`__`) is how .NET maps a flat environment variable onto its hierarchical `IConfiguration` (`__` → `:`). Compose reads the flat `.env` key and assigns it to the double-underscore container variable:

```yaml
environment:
  CONNECTIONSTRINGS__SQL: ${SOME_CONNECTION_STRING}
```

Do not invent alternate casings or separators; follow the reference exactly.

---

## Pipelines (Azure DevOps YAML)

- **Every step has a `displayName`** in plain, human-readable language ("docker compose up -d --build", "Restore", "Download .env from Secure Files").
- **Section comments** group related steps in the CD pipeline, matching the reference style:

  ```yaml
  # ── 2. Docker Compose down ───────────────────────────────
  - script: |
      cd "$(PROJECT_DIR)"
      docker compose -f "$(COMPOSE_FILE)" down --remove-orphans
    displayName: 'docker compose down'
  ```

- **Triggers are explicit.** CI: `trigger: none` + `pr: branches: include: ["*"]`. CD: `trigger: branches: include: ["master"]` and no `pr`.
- **Pools are explicit.** CI: `pool: { vmImage: ubuntu-latest }`. CD: `pool: { name: 'Default' }`.
- **Variables over literals.** Repeated paths/values (`COMPOSE_FILE`, `PROJECT_DIR`, `buildConfiguration`, tool versions) go in the `variables:` block and are referenced as `$(VAR)`.
- **Multi-line scripts** use the `script: |` block form; keep each command on its own line and echo a short confirmation at the end of a block where it aids the log.
- **Fail-safe steps** that must run regardless of prior failure carry `condition: always()` — used for `.env` removal and test-result publishing.
- **No secret echoing.** Never `echo` a secret value or `cat` the `.env` in a pipeline step.

---

## Scripts

- POSIX `sh`/`bash` for anything running on the self-hosted machine / agents.
- Quote all path variables (`"$(PROJECT_DIR)"`).
- Make cleanup idempotent (`rm -f`, `--remove-orphans`) so a re-run from any state succeeds.
- Never embed a secret; read from the environment or the transient `.env`.
