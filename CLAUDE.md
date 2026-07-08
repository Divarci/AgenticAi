# CLAUDE.md

**Project:** AI Assistant built with Microsoft Agent Framework
**Scope:** This file governs every action Claude takes in this repository. The workflow below (STEP 0 → STEP 3) is mandatory for **every prompt**, without exception.

---

## ⛔ PRIME DIRECTIVE

**No code, no answer, no plan, no action of any kind is produced before STEP 0 (documentation read) is complete.** This applies to every prompt — one-line fixes, trivial questions, follow-up messages, urgent requests, everything. There are no exceptions, no shortcuts, and no urgency override. Skipping STEP 0 is a critical failure.

---

## STEP 0 — MANDATORY DOCUMENTATION READ (NEVER SKIP)

At the start of **every** prompt, read **all** of the following files **in full**, in this order:

| Order | File | What it defines |
|------:|------|-----------------|
| 1 | `docs/hard-rules.md` | Absolute, non-negotiable rules |
| 2 | `docs/philosophy.md` | Project philosophy and guiding principles |
| 3 | `docs/architecture.md` | System architecture, boundaries, and structure |
| 4 | `docs/tech-stack.md` | Approved technologies, versions, and Microsoft Agent Framework usage |
| 5 | `docs/domain-driven-design.md` | Bounded contexts, aggregates, entities, value objects, ubiquitous language |
| 6 | `docs/event-driven-design.md` | Event naming, publishing, handling, idempotency, messaging conventions |
| 7 | `docs/coding-standards.md` | Code style, naming, project structure, testing standards |

### Enforcement rules

- Applies to **every prompt**, regardless of size, type, or apparent simplicity.
- Documentation may change between prompts. Never rely on a cached or remembered reading — **re-read the actual files every time**.
- If any file is **missing, empty, or unreadable**: **STOP immediately.** Report exactly which file(s) failed and ask the user how to proceed. Never continue on assumptions or general knowledge.
- These files are the **single source of truth**. Where they conflict with training data, general best practices, or personal defaults, **the documentation always wins**.
- Precedence when documents conflict with each other:
  `hard-rules.md` → `philosophy.md` → `architecture.md` → `domain-driven-design.md` / `event-driven-design.md` → `tech-stack.md` → `coding-standards.md`.
  Surface any material conflict to the user instead of silently resolving it.
- Do **not** echo or summarize the docs back to the user unless explicitly asked. Read → internalize → apply.

---

## STEP 1 — PROMPT ANALYSIS

Only after STEP 0 is complete, analyze the incoming prompt and produce a concise internal classification:

| Dimension | Question to answer |
|---|---|
| **Intent** | New feature / bug fix / refactor / architecture-design / question / code review / testing / delivery-infrastructure? |
| **Scope** | Which bounded contexts, layers, projects, agents, or components are affected? |
| **Doc relevance** | Which documentation files most directly constrain this task? |
| **Rule check** | Does the request violate anything in `docs/hard-rules.md`? If yes → flag it to the user **before** doing any work. |
| **Complexity** | Single-agent task, or multi-agent orchestration (sequential / parallel)? |

Additional analysis rules:

- If the prompt is ambiguous in a way that would change agent selection or a design decision, ask **one targeted clarifying question** instead of guessing.
- If the request conflicts with the documentation, do not silently comply and do not silently refuse: explain the conflict, cite the specific doc and section, and propose a compliant alternative.

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
2. Match the STEP 1 analysis against the ownership table and descriptions. Choose the **minimum set** of agents whose domains fully cover the task.
3. **Single match** → delegate the task to that agent.
4. **Multiple agents needed** → define the orchestration plan before starting:
   - **Sequential** when outputs feed each other (e.g., plan → implement → test → review → deploy).
   - **Parallel** only when the workstreams are truly independent.
5. **No suitable agent** → tell the user explicitly that no agent matches, then either handle it in the main context (if permitted) or ask whether a new agent should be created. **Never force-fit a task onto the wrong agent.**

### Delegation brief (required for every dispatched agent)

Subagent context is **isolated** — a subagent does not inherit anything you have read. Every dispatch must therefore include:

- A precise task statement and the expected output.
- The explicit list of documentation files the agent **must read itself** before working (at minimum: `docs/hard-rules.md` + the docs relevant to its task).
- Any constraints from `docs/hard-rules.md` that apply to the task.
- Any context carried by the originating Impact Report or Delegation Request (see STEP 3), since the target agent knows nothing of the requesting agent's work.
- When **re-dispatching an agent for its own portion of an approved Impact Report**: the go-ahead, its own planned work from that report, and its position in the approved sequence.

---

## STEP 3 — EXECUTION, IMPACT REPORTS & SEQUENCING LOOP

- All work must comply with the documentation read in STEP 0.
- `docs/hard-rules.md` is absolute: if completing the task would require breaking a hard rule, **stop and report** instead.
- Microsoft Agent Framework is new and evolving. **Never rely on memorized API signatures** — follow `docs/tech-stack.md` and the official references it points to.

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

- [ ] **STEP 0:** All 7 documentation files were read **in this turn**.
- [ ] **STEP 1:** The prompt was analyzed and classified.
- [ ] **STEP 2:** Agent(s) were selected per the exclusive ownership table — or the "no suitable agent" path was handled explicitly.
- [ ] Every dispatched agent received a complete brief, including any Impact Report / Delegation Request context.
- [ ] **STEP 3:** Every Impact Report was sequenced and fully dispatched; every Delegation Request was dispatched or explicitly surfaced to the user.
- [ ] No `docs/hard-rules.md` violation exists in the plan or the output.

If any box cannot be checked, do not deliver work output — resolve the gap first.
