# Tech Stack

The approved delivery and infrastructure tooling for AgenticAi. Only the technologies listed here are permitted for deployment work. Introducing a new tool, platform, registry, or base image is a decision to raise with the user first — never a silent default. Versions match the reference project and the application's `net10.0` target.

---

## CI/CD platform

| Technology | Usage |
|---|---|
| **Azure DevOps Pipelines** | The single CI/CD system. Pipelines are YAML files at the repository root (`azure-pipelines-*.yml`). |
| **Azure DevOps Secure Files** | Delivery of the production `.env` to the CD pipeline. |
| **Azure DevOps Pipeline Library** | Non-file pipeline variables and variable groups. |

---

## Build agents

| Agent | Pool / image | Used by |
|---|---|---|
| **Self-hosted** | Pool `Default` | CD pipeline (`azure-pipelines-ci-cd.yml`) — runs on the self-hosted production machine (home server / on-prem PC) and its Docker daemon. |
| **Microsoft-hosted** | `ubuntu-latest` | CI pipelines (build + test) — ephemeral cloud agents, no access to the deploy machine. |

The pool name `Default` must match the pool registered in Azure DevOps → Agent Pools. CD requires the self-hosted agent because it operates directly on the self-hosted machine's Git checkout and Docker daemon (no registry). Azure DevOps is only the CI/CD *service*; the deploy target is the self-hosted machine, not a cloud VM.

---

## Containerisation

| Technology | Version | Usage |
|---|---|---|
| **Docker Engine** | Host-installed on the self-hosted machine and developer machines | Image build and container runtime. |
| **Docker Compose** | v2 (`docker compose`, plugin syntax) | Service orchestration on both dev and the self-hosted machine. |
| **Compose file merging** | `docker-compose.yml` + `docker-compose.override.yml` | Base + development overrides (auto-merged locally; production uses the base only). |

There is **no container registry**. Images are built on the self-hosted machine at deploy time via `docker compose up -d --build`. No `docker push` / `docker pull`, no image tags to promote, no registry credentials.

---

## Base images

| Base image | Version | Used for |
|---|---|---|
| `mcr.microsoft.com/dotnet/aspnet` | `10.0` | Runtime stage of HTTP hosts (e.g. `Adapter.Restful`). Provides the non-root `$APP_UID`. |
| `mcr.microsoft.com/dotnet/runtime` | `10.0` | Runtime stage of worker hosts (e.g. `Adapter.Scheduler`) — no web server. |
| `mcr.microsoft.com/dotnet/sdk` | `10.0` | Build/publish stages for all .NET services. |
| `node:22-alpine` | `22-alpine` | All stages of any future Node/Next.js web adapter. |

Base images are pinned to a major version and pulled from the official Microsoft Container Registry / Docker Hub official images. Do not substitute unofficial or `latest`-floating base images for application services. Stock supporting services (a cache, a log sink, a dev database) use their official images and should be pinned to a specific tag when added rather than left on `latest`.

---

## Application toolchain (what the pipelines invoke)

These are owned by the `api` (and any future `web`) platform, but the deployment pipelines must invoke the matching versions. Verify against `docs/api/guidelines/tech-stack.md` — never hardcode a remembered version.

| Technology | Version | Invoked by |
|---|---|---|
| **.NET SDK** | `10.0.x` (preview versions enabled) | CI build/test steps (`UseDotNet@2`, `DotNetCoreCLI@2`) against `AgenticAi.slnx`. |
| **Microsoft Agent Framework** | Per `docs/api/guidelines/tech-stack.md` | The application runtime; the pipelines build it but do not configure it. |
| **Node.js** *(if/when a web adapter exists)* | `22.x` | Frontend CI (`NodeTool@0`) and the web Dockerfile. |

---

## Secrets & configuration delivery

| Mechanism | Usage |
|---|---|
| **Azure DevOps Secure Files** | Stores the production `.env`; downloaded per deploy with `DownloadSecureFile@1`, removed after with `condition: always()`. |
| **`.env` (git-ignored)** | Runtime configuration/secret values on the self-hosted machine, transient during a deploy. |
| **`.env.example` (committed)** | Documents required key names with empty/placeholder values only. |
| **.NET user-secrets** | Local development secrets, mounted read-only into the dev container via the override; never used in production. |
| **`System.AccessToken`** | Pipeline-scoped token used for authenticated `git fetch` in CD (`http.extraHeader` bearer). |

---

## Git-based deploy

| Technology | Usage |
|---|---|
| **Git** | The deploy mechanism: `git fetch` + `git reset --hard origin/master` brings the self-hosted machine's checkout to the target commit before rebuild. Rollback = revert on `master`. |
