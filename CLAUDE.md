# CLAUDE.md

**Project:** AI Assistant built with Microsoft Agent Framework
**Scope:** This file governs every action Claude takes in this repository. **STEP 0 (prompt analysis) runs for every prompt.** STEP 1 → STEP 3 — the documentation read, agent selection, and execution loop — run **only** when STEP 0 identifies a task that belongs to a sub-agent's domain. Prompts with no such task are ordinary conversation and are answered directly.

---

## ⛔ PRIME DIRECTIVE

**Every prompt begins with STEP 0 — prompt analysis — and nothing else may precede it.** STEP 0's decisive output is whether the prompt contains a task that belongs to a sub-agent's domain (planning, application code, bug fix, tests, code review, or delivery/infrastructure).

- **If it does not** — ordinary conversation, an unrelated/off-topic subject, a general question, or a custom/ad-hoc request to the AI — then STEP 1 → STEP 3 and the mandatory documentation read **do not apply**. Respond to the user normally. Never force platform reads or agent delegation onto a prompt that carries no agent task.
- **If it does** — **first confirm the task with the user**, and only then does the rest become mandatory: **no code, no plan, no delegation, and no final work output is produced until (1) the target platform(s) have been identified — STEP 0 — and (2) the platform-scoped documentation for every identified platform has been read in full — STEP 1 — both in the current turn.** The only work permitted before the STEP 1 read is the STEP 0 analysis needed to decide *which* documentation to read. Within this branch there are no exceptions, no shortcuts, and no urgency override; skipping or reordering STEP 0 → STEP 1 is a critical failure.

---

## STEP 0 — PROMPT ANALYSIS & ROUTING (ALWAYS FIRST)

At the start of **every** prompt, **before reading any guideline document**, analyze the incoming prompt and produce a concise internal classification:

| Dimension | Question to answer |
|---|---|
| **Delegatable work?** | Does the prompt contain a task owned by a sub-agent — planning, application code, bug fix, tests, code review, or delivery/infrastructure (`planner`/`coder`/`bugfixer`/`tester`/`reviewer`/`deployer`)? **This is the gating decision.** |
| **Intent** | New feature / bug fix / refactor / architecture-design / question / code review / testing / delivery-infrastructure? |
| **Target platform(s)** | *(Only when there is delegatable work.)* Which platform(s) does the task touch — `api`, `mobile`, `web`, `deployment`? One, several, or **all**. `deployment` covers all delivery/infrastructure (CI/CD, Docker, compose, environments, secrets). This selection drives the STEP 1 reads. |
| **Scope** | Which bounded contexts, layers, projects, agents, or components are affected within each platform? |
| **Complexity** | Single-agent task, or multi-agent orchestration (sequential / parallel)? |

### Routing gate — decides whether STEP 1 → STEP 3 run at all

- **No delegatable work** → the prompt is ordinary conversation, an unrelated/off-topic subject, a general question, or a custom/ad-hoc request to the AI. **STEP 1 → STEP 3 and the mandatory documentation read do not apply — just respond to the user normally, as a regular assistant.** Do not force platform reads or agent delegation onto a conversation that has no agent task.
- **Delegatable work detected** → **confirm with the user before any read or delegation.** A single prompt may bundle **several** delegatable tasks spanning different agents — capture **all** of them, never just the first. Restate every task, the agent each maps to (`planner`/`coder`/`bugfixer`/`tester`/`reviewer`/`deployer`), and — when there is more than one — the proposed order (sequential vs. parallel per dependencies). Present this as **one consolidated confirmation** and proceed only on the user's go-ahead. If the user approves only part of it, act on the approved subset and drop the rest; if they add or change scope, re-confirm. Once confirmed, STEP 1 → STEP 2 → STEP 3 and the PRIME DIRECTIVE become mandatory for every confirmed task.
- **Mixed prompts** — when a prompt contains **both** conversation and delegatable work, answer the conversational part normally, then surface the delegatable task(s) and confirm them before starting.
- **When unsure** whether a prompt crosses into agent work, ask **one targeted clarifying question** rather than either forcing the workflow or skipping it.

> **Confirm — single task:** "This is a bug fix in the API domain layer → `bugfixer`. Shall I proceed?"
> **Confirm — multi-task:** "This spans three tasks: (1) add the endpoint → `coder` (api); (2) cover it with tests → `tester`; (3) wire the pipeline stage → `deployer` (deployment). Proposed order: 1 → 2 → 3 (2 and 3 can run in parallel after 1). Proceed with all three, or a subset?"

Rules *(apply only to the delegatable-work branch)*:

- **Platform detection is a routing decision only** — it decides which document sets STEP 1 must read. It is the sole analysis allowed before the documentation read; all substantive task work (design, code, delegation, work output) still waits for STEP 1 to complete.
- Include a platform if the task touches it either directly or indirectly (e.g., a web change that needs a new API endpoint → `web` + `api`).
- A task with no clear platform footprint (repo-wide, tooling, or governance work) defaults to the `api` set at minimum, plus any platform the work actually touches.
- If the prompt is ambiguous in a way that would change **platform selection**, agent selection, or a design decision, ask **one targeted clarifying question** instead of guessing.

---

## STEP 1 — MANDATORY PLATFORM-SCOPED DOCUMENTATION READ (NEVER SKIP)

Using the platform(s) identified in STEP 0, read **in full**, in the listed order, the core guideline set for **every** involved platform. Multiple platforms → read **all** of their sets.

**API — `docs/api/guidelines/`**

| Order | File | What it defines |
|------:|------|-----------------|
| 1 | `hard-rules.md` | Absolute, non-negotiable rules |
| 2 | `philosophy.md` | Project philosophy and guiding principles |
| 3 | `architecture.md` | System architecture, boundaries, and structure |
| 4 | `tech-stack.md` | Approved technologies, versions, and Microsoft Agent Framework usage |
| 5 | `domain-driven-design.md` | Bounded contexts, aggregates, entities, value objects, ubiquitous language |
| 6 | `event-driven-design.md` | Event naming, publishing, handling, idempotency, messaging conventions |
| 7 | `coding-standards.md` | Code style, naming, project structure, testing standards |
| 8 | `security.md` | Security rules and requirements |

**Mobile — `docs/mobile/guidelines/`** · **Web — `docs/web/guidelines/`** (identical set, in this order)

| Order | File | What it defines |
|------:|------|-----------------|
| 1 | `hard-rules.md` | Absolute, non-negotiable rules |
| 2 | `philosophy.md` | Platform philosophy and guiding principles |
| 3 | `architecture.md` | Platform architecture and boundaries |
| 4 | `tech-stack.md` | Approved technologies and versions |
| 5 | `structure.md` | Project structure and organization |
| 6 | `coding-standards.md` | Code style, naming, and conventions |
| 7 | `security.md` | Security rules and requirements |
| 8 | `testing.md` | Testing standards and expectations |

**Deployment — `docs/deployment/guidelines/`** (delivery & infrastructure — owned by `deployer`)

| Order | File | What it defines |
|------:|------|-----------------|
| 1 | `hard-rules.md` | Absolute, non-negotiable rules |
| 2 | `philosophy.md` | Delivery philosophy and guiding principles |
| 3 | `architecture.md` | Deployment topology, environments, and boundaries |
| 4 | `tech-stack.md` | Approved CI/CD platforms, registries, base images, tooling |
| 5 | `structure.md` | Layout and organization of pipeline and infrastructure assets |
| 6 | `coding-standards.md` | Conventions for pipelines, Dockerfiles, compose files, scripts |
| 7 | `security.md` | Secrets, environment, and supply-chain rules |

> The orchestrator reads the **platform core sets** above. Layer- and area-specific documents (`docs/api/guidelines/layers/**`, including `layers/testing/**`) are read by the owning subagents per their own conditional-read rules — never skip them at the agent level.

### Enforcement rules

- Applies to **every prompt**, regardless of size, type, or apparent simplicity.
- Read the set for **every** platform identified in STEP 0 — one, several, or all four. Never read a platform set the task does not touch, and never skip one it does.
- Documentation may change between prompts. Never rely on a cached or remembered reading — **re-read the actual files every time**.
- If any required file is **missing, empty, or unreadable**: **STOP immediately.** Report exactly which file(s) failed and ask the user how to proceed. Never continue on assumptions or general knowledge.
- These files are the **single source of truth**. Where they conflict with training data, general best practices, or personal defaults, **the documentation always wins**.
- Precedence when documents conflict **within a platform**:
  `hard-rules.md` → `philosophy.md` → `architecture.md` → `domain-driven-design.md` / `event-driven-design.md` (API) or `structure.md` (mobile/web/deployment) → `tech-stack.md` → `coding-standards.md` → `security.md`.
  Across platforms, surface any material conflict to the user instead of silently resolving it.
- **Rule check (now that `hard-rules.md` is loaded):** if the request would violate a hard rule of any involved platform, flag it to the user — citing the specific doc and section — **before** doing any work. Do not silently comply and do not silently refuse; propose a compliant alternative.
- Do **not** echo or summarize the docs back to the user unless explicitly asked. Read → internalize → apply.

---

## STEP 2 — AGENT SELECTION

Agent definitions live in `.claude/agents/`. Each agent file describes its specialty, responsibilities, and boundaries.

### Domain ownership (exclusive)

| Domain | Exclusive owner |
|---|---|
| Planning, task breakdown, architectural decisions | `planner` |
| Application source code — new functionality and changes | `coder` |
| Application source code — defect fixes | `bugfixer` |
| All test code and test infrastructure (unit / integration / E2E, fixtures, mocks, test config) | `tester` |
| Code review and compliance findings (read-only) | `reviewer` |
| CI/CD pipelines, Dockerfiles, docker-compose, environments, secret/env management | `deployer` |

Domains are **exclusive** — no agent ever works in another agent's domain. A task spanning several domains is **split** across the owning agents (sequential or parallel per dependencies), never absorbed by one agent.

### Selection procedure

1. List `.claude/agents/` and read the **description of every available agent** — never select from memory.
2. Match the STEP 0 analysis against the ownership table and descriptions. Choose the **minimum set** of agents whose domains fully cover the task.
3. **Single match** → delegate the task to that agent.
4. **Multiple agents needed** → define the orchestration plan before starting:
   - **Sequential** when outputs feed each other (e.g., plan → implement → test → review → deploy).
   - **Parallel** only when the workstreams are truly independent.
5. **No suitable agent** → tell the user explicitly that no agent matches, then either handle it in the main context (if permitted) or ask whether a new agent should be created. **Never force-fit a task onto the wrong agent.**

### Delegation brief (required for every dispatched agent)

Subagent context is **isolated** — a subagent does not inherit anything you have read. Every dispatch must therefore include:

- A precise task statement and the expected output.
- The explicit list of documentation files the agent **must read itself** before working (at minimum: the `hard-rules.md` of every platform the task touches — `docs/api|mobile|web|deployment/guidelines/hard-rules.md` — plus the docs relevant to its task).
- Any constraints from the applicable platform `hard-rules.md` files that apply to the task.
- Any context carried by the originating Impact Report or Delegation Request (see STEP 3), since the target agent knows nothing of the requesting agent's work.
- When **re-dispatching an agent for its own portion of an approved Impact Report**: the go-ahead, its own planned work from that report, and its position in the approved sequence.

---

## STEP 3 — EXECUTION, IMPACT REPORTS & SEQUENCING LOOP

- All work must comply with the documentation read in STEP 1.
- The applicable `hard-rules.md` (of every involved platform) is absolute: if completing the task would require breaking a hard rule, **stop and report** instead.
- Microsoft Agent Framework is new and evolving. **Never rely on memorized API signatures** — follow `docs/api/guidelines/tech-stack.md` and the official references it points to.

### Orchestration loop (mandatory)

**Subagents cannot invoke other subagents.** All cross-domain coordination flows through you, in two forms:

**1. Impact Report (pre-execution).** An agent analyzed its task, found that the complete solution requires work in other agents' domains, and stopped **before changing anything**. Its report contains: its own planned work, one Delegation Request per out-of-domain need, and a suggested ordering. On receiving an Impact Report:
  1. Evaluate **every** work item — the reporting agent's own planned work AND each Delegation Request.
  2. Decide the correct execution order from the dependencies. The agent's suggested ordering is **input, not the decision** — sequencing is yours. For large, non-trivial dependency graphs, you may dispatch `planner` to produce the sequence.
  3. Dispatch the agents **one at a time, in that order** — re-dispatching the reporting agent for its own portion at its position in the sequence, with the go-ahead and full context.

**2. Delegation Requests attached to completed work (post-execution).** Remaining needs reported after an agent executed its in-domain work. Dispatch `blocking` requests before treating the originating work as done; dispatch `follow-up` requests afterwards.

The overall task is complete only when **every work item from every Impact Report and Delegation Request has been executed or explicitly surfaced to the user** — nothing is ever silently dropped. Before presenting any final result, verify it against the documentation.

### Worked example

User reports a bug → `bugfixer` is dispatched → bugfixer diagnoses the root cause and determines the complete solution: a domain-model fix (its own work), which invalidates existing domain unit tests (tester's domain) and requires a pipeline change (deployer's domain). Bugfixer **changes nothing** and returns an Impact Report: own planned fix + Delegation Request → `tester` (update invalidated domain unit tests, add a regression test — with full reproduction details) + Delegation Request → `deployer` (the pipeline change) + suggested ordering. The main agent evaluates the dependencies, decides the sequence (e.g., 1. `bugfixer` applies the fix, 2. `tester` updates and adds tests, 3. `deployer` updates the pipeline — or a different order if dependencies dictate), and dispatches each agent in order with a complete brief. The task is done only when all three have completed.

---

## ✅ COMPLIANCE CHECKLIST (self-verify before every response)

- [ ] **STEP 0:** The prompt was analyzed. **If it carries no delegatable agent task → the items below are N/A; respond conversationally.** Otherwise the task was confirmed with the user and its target platform(s) identified.
- [ ] **STEP 1:** The full core guideline set was read **in this turn** for **every** identified platform.
- [ ] **STEP 2:** Agent(s) were selected per the exclusive ownership table — or the "no suitable agent" path was handled explicitly.
- [ ] Every dispatched agent received a complete brief, including any Impact Report / Delegation Request context.
- [ ] **STEP 3:** Every Impact Report was sequenced and fully dispatched; every Delegation Request was dispatched or explicitly surfaced to the user.
- [ ] No `docs/api/guidelines/hard-rules.md` violation exists in the plan or the output.

Items STEP 1 → STEP 3 apply only to the delegatable-work branch. If a box in that branch cannot be checked, do not deliver work output — resolve the gap first.
