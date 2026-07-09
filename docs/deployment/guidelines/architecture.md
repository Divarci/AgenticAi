# Architecture

Deployment topology for AgenticAi: the environments, the delivery flow from pull request to running container, and the container / network / volume model. This document describes the **model and its conventions**; it is grounded in AgenticAi's current single-service reality but is written to extend cleanly as services are added.

---

## Two environments — and only two

| Environment | Where it runs | Compose files | Configuration source | Local infra |
|---|---|---|---|---|
| **Development** | Developer workstation | `docker-compose.yml` **+** `docker-compose.override.yml` (auto-merged) | Inline throwaway values in the override + local user-secrets | Local containers added in the override (e.g. a `sqlserver` container) |
| **Production** | Single self-hosted machine (home server / on-prem PC) | `docker-compose.yml` only (no override) | `.env` from Azure DevOps Secure Files | None — managed/external services reached via `.env` values |

The production target is a **self-hosted machine** — a home server / on-prem PC that runs the Azure DevOps self-hosted agent (`Default` pool). It is **not** an Azure- or cloud-hosted VM. Azure DevOps is only the CI/CD *service* that orchestrates the build and deploy; the machine the containers actually run on is self-hosted.

There is **no staging environment**. Development is local and disposable; Production is the one self-hosted machine. Any configuration difference between them is expressed as a delta in `docker-compose.override.yml` and is therefore explicit and reviewable.

---

## Compose file merging — base vs. override

The two Compose files are not two separate configurations; they are a **base** and a **development overlay** that Docker combines. Understanding the merge is essential — this is the single most misread part of the setup.

- **Automatic merge in development.** When you run a bare `docker compose` command (e.g. `docker compose up`) in the project directory, Docker Compose **automatically discovers and merges `docker-compose.override.yml` on top of `docker-compose.yml`**. You do not name the override file; its presence is enough.
- **Override supersedes base.** Where a key exists in both files, the value in `docker-compose.override.yml` **overrides** the value from `docker-compose.yml`. Keys present only in the base pass through unchanged; keys/services present only in the override are added. This is why the override can add a dev-only service (e.g. a local `sqlserver`), pin `ASPNETCORE_ENVIRONMENT=Development`, expose dev ports, and mount local user-secrets/certs — each entry wins over (or adds to) the base for local runs.
- **Base holds the production definition.** `docker-compose.yml` carries the production-grade, fully `${VAR}`-parameterized definitions (see the `${ENV}` substitution model below). It hardcodes nothing environment-specific; the override is what supplies concrete dev values locally.
- **Production ignores the override.** The CD pipeline runs Compose with an **explicit** file selection — `docker compose -f docker-compose.yml ...` — so the override is **never** merged in production. Production therefore uses only the base file plus the `.env` supplied at deploy time. The dev-only override values can never leak into a production deploy.

```
Development (bare `docker compose ...`):   base  ⊕  override   → override values win
Production  (`docker compose -f docker-compose.yml ...`):   base only  +  .env
```

`coding-standards.md` gives the authoring rules for what may live in each file; `structure.md` records their location.

---

## Delivery flow

```
   Developer
      │  push branch, open PR
      ▼
┌──────────────────────────────────────────────────────────┐
│  Azure DevOps — CI (hosted agent, ubuntu-latest)          │
│  pr: ["*"]   trigger: none                                 │
│  ┌────────────────────────┐   ┌────────────────────────┐  │
│  │ api build + test        │   │ (future) web / mobile   │  │
│  │ restore/build/test .slnx│   │ build + test            │  │
│  └────────────────────────┘   └────────────────────────┘  │
└──────────────────────────────────────────────────────────┘
      │  gate passes → review → merge to master
      ▼
┌──────────────────────────────────────────────────────────┐
│  Azure DevOps — CD (self-hosted agent, pool: Default)     │
│  trigger: master   (no pr)                                 │
│                                                            │
│  1. DownloadSecureFile .env → copy into project dir        │
│  2. docker compose down --remove-orphans                   │
│  3. docker prune (image -a / volume / network / builder)   │
│  4. git fetch + git reset --hard origin/master             │
│  5. docker compose --env-file .env up -d --build           │
│  6. rm -f .env            (condition: always())            │
│  7. docker compose ps     (status check)                   │
└──────────────────────────────────────────────────────────┘
      │  builds images on the self-hosted machine, starts containers
      ▼
┌──────────────────────────────────────────────────────────┐
│    Production — self-hosted machine (home / on-prem)     │
│   ┌────────────────────────────────────────────────────┐  │
│   │  Docker Compose project (bridge network)            │  │
│   │                                                     │  │
│   │   ┌──────────────┐        (future services join     │  │
│   │   │ adapter.restful│        the same network:         │  │
│   │   │  :8080/:8081  │        scheduler, cache, db, …)  │  │
│   │   └──────┬───────┘                                   │  │
│   │          │                                           │  │
│   └──────────┼───────────────────────────────────────────┘  │
│              │                                              │
└──────────────┼──────────────────────────────────────────────┘
               ▼
        External services (not containerised):
        Keycloak (identity), managed database, etc.
        — reached via URLs/connection strings from .env
```

The self-hosted agent runs **on** the production machine (the home server / on-prem PC) and is registered in the Azure DevOps `Default` agent pool. This is what lets the CD pipeline operate on that machine's Git checkout and Docker daemon directly — no cloud VM and no container registry are involved.

---

## Container topology

AgenticAi today defines a **single service** in `docker-compose.yml`:

| Service | Built from | Role |
|---|---|---|
| `adapter.restful` | `api/src/adapters/Adapter.Restful/Dockerfile` | ASP.NET Core REST API host, built with the Microsoft Agent Framework |

The model is designed to grow without restructuring. As the solution's other adapters and infrastructure providers are containerised, each is added as another service on the **same bridge network**, following the same conventions. Illustrative future additions (patterns, not commitments):

| Possible service | Kind | Notes |
|---|---|---|
| `adapter.scheduler` | custom build (`Adapter.Scheduler`) | background/worker host — runtime image, no exposed HTTP port |
| a cache service | stock image | named volume for persistence |
| a log/observability service | stock image | named volume for persistence |
| a database (**dev only**) | stock image, defined in the **override** | Production uses a managed/external instance via `.env` |
| a web frontend | custom build (`node:22-alpine`) | build-time args + runtime env |

Rules that hold regardless of how many services exist:

- **One bridge network.** All services share a single user-defined bridge network so they resolve each other by service name.
- **Named volumes for stateful services.** Any service that must persist data uses a named volume; stateless services use none.
- **Custom images build from the repo root context** so a Dockerfile can `COPY` any project it references; the `dockerfile:` path points at the co-located Dockerfile.
- **External services are never containerised in production.** Identity (Keycloak) and any managed database are reached over the network via `.env`-supplied URLs and connection strings. In development the override may point these at local addresses or add a local container (e.g. a dev database).

---

## The `${ENV}` substitution model

Configuration flows through two hops from the secret store to the .NET process:

```
Secure Files (.env)              docker-compose.yml                 container process
─────────────────────           ──────────────────────             ──────────────────────
SOME_CONNECTION_STRING=...  ──►  CONNECTIONSTRINGS__SQL:       ──►  IConfiguration:
                                   ${SOME_CONNECTION_STRING}          ConnectionStrings:Sql
```

1. **`.env` → Compose.** `.env` holds flat, uppercase, single-underscore keys (e.g. `ASPNETCORE_ENVIRONMENT`, a connection-string key). `docker-compose.yml` references them as `${VAR}`. Compose substitutes the values at `up` time. Nothing environment-specific is literal in the base file.
2. **Compose → container.** Compose sets the container's environment variables using the .NET double-underscore convention (`SECTION__SUBSECTION__KEY`), which ASP.NET Core maps onto its hierarchical `IConfiguration` (`Section:Subsection:Key`).

This is why the base Compose file is environment-agnostic: swapping `.env` swaps the entire environment with no file change. See `coding-standards.md` for the naming rules and `security.md` for how HTTPS-metadata and certificate-trust settings differ between dev and prod.

---

## Boundaries

- The deployment architecture owns **how** the application is packaged, wired, and delivered — not **what** the application does. The API's internal layering (adapters / applications / core / infrastructures) is the `api` platform's concern; deployment only needs to know which projects produce runnable hosts and how to build them.
- Any change that requires the application to expose something new for deployment (a health endpoint, a new configuration key it must read) is a Delegation Request to `coder`/`bugfixer`, not a deployment change.
