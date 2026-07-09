# Structure

Where deployment assets live in the AgenticAi repository and how they are named. This layout is a convention: assets that do not yet exist (pipelines, `.env.example`, additional Dockerfiles) MUST follow the placement and naming described here when they are added.

---

## Repository layout

```
AgenticAi/                                  ← repo root
├── AgenticAi.slnx                          ← solution (built by CI)
├── docker-compose.yml                      ← base / production service definitions
├── docker-compose.override.yml             ← development overrides (auto-merged locally)
├── docker-compose.dcproj                   ← VS Compose project wrapper
├── .dockerignore                           ← build-context exclusions (root)
├── .env                                    ← runtime secrets — GIT-IGNORED, never committed
├── .env.example                            ← required key names + placeholders (committed)  [to add]
├── azure-pipelines-ci-cd.yml               ← CD: build on self-hosted machine + deploy       [to add]
├── azure-pipelines-api-build-and-test.yml       ← CI: api PR gate                            [to add]
├── azure-pipelines-web-build-and-test.yml       ← CI: web PR gate (only if a web app exists) [to add]
├── azure-pipelines-mobile-build-and-test.yml    ← CI: mobile PR gate (only if a mobile app)  [to add]
├── api/
│   └── src/
│       └── adapters/
│           ├── Adapter.Restful/
│           │   ├── Adapter.Restful.csproj
│           │   └── Dockerfile              ← co-located with its csproj
│           ├── Adapter.Scheduler/
│           │   ├── Adapter.Scheduler.csproj
│           │   └── Dockerfile              ← when containerised, co-located here [to add]
│           └── Adapter.AppHost/
└── docs/
    └── deployment/
        └── guidelines/                     ← these documents
```

Entries marked `[to add]` are conventions for assets not yet present; create them in these exact locations.

---

## Placement rules

| Asset | Location | Rule |
|---|---|---|
| **Dockerfile** | Alongside the owning adapter's `.csproj` (e.g. `api/src/adapters/Adapter.Restful/Dockerfile`) | One Dockerfile per runnable host, co-located with its project. Never centralised in a `docker/` folder. |
| **`docker-compose.yml`** | Repo root | Single base file for all services. Build **context is the repo root**; the `dockerfile:` key holds the path to the co-located Dockerfile. |
| **`docker-compose.override.yml`** | Repo root | Development deltas only; merged automatically by `docker compose` when present. |
| **`docker-compose.dcproj`** | Repo root | Visual Studio Compose tooling wrapper; leave in place. |
| **Pipeline definitions** | Repo root | Named `azure-pipelines-*.yml` (see naming below). |
| **`.dockerignore`** | Repo root | Single root file governing every build context. |
| **`.env` / `.env.example`** | Repo root | `.env` git-ignored; `.env.example` committed with keys only. |
| **Deploy/ops scripts** | Repo root or a `scripts/` folder | Kept in the repo, reviewed like any asset; never hand-run steps that live nowhere. |

---

## Pipeline naming

Pipelines are named by role so their purpose is obvious in the Azure DevOps list:

There is one CD pipeline and **one CI pipeline per platform** (`api`, `web`, `mobile`). A platform's CI pipeline exists only once that platform exists in the repo; `api` is the only one present today.

| File | Role | Trigger |
|---|---|---|
| `azure-pipelines-ci-cd.yml` | CD — build on the self-hosted machine and deploy | push to `master` |
| `azure-pipelines-api-build-and-test.yml` | CI — api build + test gate | pull request (`*`) |
| `azure-pipelines-web-build-and-test.yml` | CI — web build + test gate *(only if a web app exists)* | pull request (`*`) |
| `azure-pipelines-mobile-build-and-test.yml` | CI — mobile build + test gate *(only if a mobile app exists)* | pull request (`*`) |

Additional CI gates follow the same per-platform pattern: `azure-pipelines-{platform}-build-and-test.yml`.

---

## Dockerfile / service naming

- **Compose service name** matches the adapter, lowercased with a dot: `adapter.restful`, and (when added) `adapter.scheduler`. Stock supporting services use a short functional name (e.g. a cache or database service).
- **Image name** for a custom build follows `${DOCKER_REGISTRY-}<compactname>` (e.g. `${DOCKER_REGISTRY-}adapterrestful`). The `${DOCKER_REGISTRY-}` prefix resolves to empty in this no-registry setup — it is retained only for Visual Studio tooling compatibility and never points at a real registry.
- **`container_name`** (when set) uses a readable PascalCase project prefix (e.g. `AgenticAi-Api`), added when it aids operability; it is optional for single-instance services.

---

## What does NOT belong here

- Application source, `appsettings*.json`, and `Program.cs` live under `api/src/**` and are owned by the `api` platform — not deployment assets.
- Test projects live under the solution's test tree and are owned by `tester`.
- These guideline documents are the only files under `docs/deployment/`.
