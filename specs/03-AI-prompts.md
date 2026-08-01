# AI Prompts Record - Kaitiaki Quest

> **Purpose**: This document records the actual prompts used, the AI responses received, and the human decisions made throughout development — as evidence that AI tools accelerated the work while every output was critically reviewed, tested, and modified by the author.

## Methodology

Development used three AI tools with distinct roles, all originally in Chinese and translated to English below:

| Tool | Role | Volume |
| :--- | :--- | :--- |
| **DeepSeek** | Primary planning & development assistant — drove architecture decisions, generated the bulk of backend/frontend code day-by-day, and wrote the majority of the test suites | 1 continuous conversation, ~226 question/answer exchanges |
| **Gemini** | Supplementary — code review and optimization passes on already-written components, and the entire Azure deployment troubleshooting process | 17 separate topic-specific conversations |
| **Qwen (千问)** | Supplementary — targeted debugging of specific errors (CORS, SignalR contracts, state management, testing infrastructure) | 17 separate topic-specific conversations |

Entries below are grouped by development phase (matching [01-planning.md](01-planning.md)'s timeline) and cross-referenced against `git log` commit hashes where a direct correspondence exists. AI responses are summarized, not reproduced verbatim — the point of this record is to show *what was asked, what was recommended, and what the author decided to do with it*, including cases where the author rejected or modified the AI's suggestion.

---

## Phase 1 — Ideation & Requirements Analysis (7 Jul 2026)

*Tool: DeepSeek. Commits: `9ae6b43`, `9b092de`, `cd322ef`.*

- **Project concept selection.** Asked for gamified project ideas fitting React+TS+MUI / ASP.NET Core and the MSA brief. DeepSeek proposed five concepts (eco-action tracker, coding-challenge platform, habit tracker, NZ-culture quiz app, focus-time PvP app), recommending the eco-action idea for its local-culture fit and natural tie-in to Azure Cognitive Services/Maps/SignalR. After a deeper dive was requested on that concept and the official MSA assessment PDF was cross-referenced against it, the author committed to it and named the project **Kaitiaki Quest**.
- **Feasibility and scope check.** Asked whether an AI-image-upload advanced option was achievable in the 4-week window. DeepSeek gave a week-by-week roadmap and flagged Azure key management and CORS as the main risks. The author ultimately **did not** build the AI-image-recognition feature — the three advanced features actually shipped (SignalR, JWT+RBAC, IMemoryCache) were chosen instead once the PDF's exact wording was mapped against feasibility.
- **Repo/environment setup.** DeepSeek recommended `kaitiaki-quest` as the repo name and gave the mono-repo `.gitignore` template. The author accidentally created `.gitignore` as a **folder** instead of a file (common Windows Explorer slip) — DeepSeek gave the recovery steps (delete folder, recreate as file), fixed in commit `9b092de`.
- **Extended troubleshooting: backend unreachable / Scalar 404.** A multi-step debugging thread: HTTPS dev-certificate trust, `MapScalarApiReference()` missing from `Program.cs`, and — the actual root cause — Visual Studio defaulting to the `Production` environment because the wrong dropdown (Debug/Release instead of the launch-profile selector) was being used, so `launchSettings.json` was never read. Diagnosed by having the author run `$env:ASPNETCORE_ENVIRONMENT="Development"; dotnet run` directly in a terminal to isolate VS-specific behavior from the code itself. Resolved in commit `9ae6b43`.

---

## Phase 2 — Backend Development (8–12 Jul 2026)

*Tool: DeepSeek. Commits: `c877526`…`1625fe9` (see [02-architecture.md](02-architecture.md) for the full commit list).*

- **Admin/role design.** Asked whether the Users table needed an admin/non-admin distinction. DeepSeek recommended ASP.NET Core Identity's built-in `IdentityRole`/`AspNetUserRoles` over a manual boolean flag — adopted, and is the basis of the RBAC advanced feature (`1f3ea9e`).
- **Layered architecture (Controller → Service → EF Core).** The author explicitly asked that controllers only call services, with a `Services/Interfaces` + `Services/Implementations` split, and a `ServiceResult<T>` internal envelope kept separate from the external `ApiResponse<T>` HTTP envelope (DeepSeek confirmed these serve different layers rather than being redundant). This became the standing convention for the rest of the backend (`5881308`, `ac9f95e`, `ef478db`).
- **EF Core query optimization.** The author independently proposed replacing `TeamService`'s `Include`/`ThenInclude` eager-loading with `Select` projection directly into DTOs; DeepSeek confirmed this was the better pattern (precise SQL, no `PasswordHash` leakage) and rewrote the full service accordingly.
- **TeamHub (SignalR) design.** Clarified the Hub/Service/Controller relationship: business services push via `IHubContext<TeamHub>`, while the Hub itself only needs `JoinTeamRoom`/`LeaveTeamRoom`. The author suggested removing unused `NotifyMissionCompleted`/`NotifyTeamXPUpdated` Hub methods once it was clear `SendAsync` was already being called directly from the service layer — DeepSeek agreed, and this simplification was kept (`386cde6`, `649ad18`).

---

## Phase 3 — Frontend Development (13–20 Jul 2026)

*Commits: `6038d4e`…`290bcd4`. Primary tool DeepSeek for the day-by-day build; Gemini and Qwen for targeted fixes below.*

### Core build (DeepSeek)
- 7-day plan delivered and followed day-by-day: theme/Axios/router → auth store/pages → Dashboard → My Missions → Teams+SignalR → Leaderboard/Profile/Badges → polish/deploy prep.
- **Theme module split.** Vite's Fast Refresh warning ("only works when a file only exports components") required splitting a combined Context+Provider+Hook file into three separate files (`ThemeContext.tsx`, `ThemeProvider.tsx`, `useTheme.ts`) — took two attempts, since the first split still left two exports in one file.
- **Login → Dashboard redirect bug (1st occurrence, `c7db8b5`).** Root cause: a spelling error in a `useAuthStore` field name caused the persisted-auth check to always read `undefined`.
- **DTO design decision.** When the backend's flat `UserBadgeResponseDto` didn't match the frontend's nested expected shape, DeepSeek recommended nesting (`Badge` as a sub-object) for reusability and clearer semantics — adopted, along with configuring `JsonNamingPolicy.CamelCase` so PascalCase C# properties matched the frontend's camelCase TypeScript types.

### Login redirect bug — 2nd occurrence, root cause (Gemini, `Fixing-401-Error-on-Dashboard-Load.md`)
A second, distinct occurrence of the same symptom was traced by Gemini to a **typo**: the Axios interceptor read the token from `localStorage.getItem('autho-storage')` (extra "o") while Zustand's `persist` middleware actually wrote it under `auth-storage`, so the parsed token was always `null`. Fix: correct the key (or, as recommended, read the token via `useAuthStore.getState().token` directly instead of manually re-parsing `localStorage`, avoiding this class of bug entirely).

### Component review passes (Gemini)
Gemini was used systematically to review each finished page component before moving on, consistently flagging: unused imports/dead code, `catch (err: any)` breaking type safety (fixed with `axios.isAxiosError`), recomputed values needing `useMemo`, and MUI version-specific API changes. Concrete fixes adopted:
- **`MissionCard`/Dashboard/Leaderboard/API-Controller optimization pass** — extracted `STATUS_CONFIG`/`getRankColor` helper maps instead of inline switches, added `AbortController` cleanup to leaderboard fetches, and fixed a stray `loadDashboardData()` call left over from a category-filter refactor (would have thrown a `ReferenceError`).
- **`TeamsController.cs` review** — Gemini caught a genuine compile-blocking bug (missing semicolon in `GetMyTeam`'s return), a redundant `[Authorize]` attribute, and a missing `IsSuccess` check; also recommended extracting `GetUserId()` into a shared `ClaimsPrincipalExtensions` used across all controllers.
- **`DbUpdateConcurrencyException` in `TeamService`** (`3206016`) — root cause was `Attach(new ApplicationUser { Id = userId })` stub entities carrying a default `ConcurrencyStamp` that never matched the real DB value, so updates silently affected 0 rows. Fixed across `CreateTeamAsync`/`JoinTeamAsync`/`LeaveTeamAsync` by switching to `ExecuteUpdateAsync`/`ExecuteDeleteAsync`, which bypass entity-tracking and concurrency checks entirely.
- **MUI `InputProps`/`TextField` deprecation** — recurred twice (Login page, Teams forms); fixed by migrating to `slotProps={{ input: {...} }}` per MUI's current API.
- **Silent "no team" state** — iteratively refined from "404 with a popup" → "404 handled silently" → the final, cleaner design: the backend always returns `200 OK` with `data: null` when a user has no team, so the frontend needs no special-case error branching at all.
- **Mobile layout fixes** — badge-wall uneven rows (fixed with `flex:1`/`stretch` grid alignment), nav bar items justified left instead of right (root cause: `flexGrow:1` was on the title instead of a dedicated spacer `Box`), and the Admin Panel mission list's `ListItemSecondaryAction` overlapping content on narrow screens (replaced with a three-tier stacked flex layout).

### State-management and CORS fixes (Qwen)
- **CORS setup** — initial failures traced to two real bugs: `http://locahost:5173` (typo, missing the "l") and `UseCors()` positioned before `UseHttpsRedirection()`/`UseAuthentication()` instead of after `UseRouting()`.
- **Auth architecture review (two passes)** — Qwen caught a **critical dual-persistence bug**: Zustand's `persist` wrote to `localStorage['auth-storage']` while the code *also* manually wrote to `localStorage['token']`/`['user']`, with the Axios interceptor reading the manual (stale) key. Fix: remove all manual `localStorage` calls, read exclusively via `useAuthStore.getState()`. Also flagged and fixed: redundant `fetchUser()` calls right after login risking a spurious logout on transient failure, and the .NET convention question of whether login failures should return HTTP 401 (breaks Axios's success-path assumption) vs. `200 OK` with `success:false` — the latter was adopted as the standard.
- **React 19 `use()`/Suspense experiment — tried and reverted.** The Profile page was migrated to React 19's `use()` API to eliminate manual loading/error state. This introduced an infinite-request loop, debugged across several rounds (unstable Promise identity, `MainLayout` re-rendering the whole `<Outlet/>` tree on any Zustand store change). After multiple fixes didn't fully resolve it, **the author made the pragmatic call to revert `useProfileData` to the conventional `useState`+`useEffect` pattern**, with Qwen endorsing this given `use()`'s immaturity with Suspense/Router/ErrorBoundary interaction at the time.

### Case Study: Real-Time Team Sync (SignalR) — a cross-tool debugging arc
This was the single most AI-intensive debugging thread of the project, spanning DeepSeek (initial design), Gemini (two long conversations), and four separate Qwen sessions. It directly underpins the **WebSockets/SignalR advanced feature** claimed in [01-planning.md](01-planning.md), so the iteration is recorded in full:

1. **Contract mismatch.** Gemini and Qwen (independently, in separate sessions) both audited the frontend `signalRService.ts` against the backend `TeamHub`/`TeamService` and found the event payload the frontend expected (`teamName`, `completedBy`, `missionTitle`, `earnedXP`) didn't match what the backend actually sent, and that the **Group name used at join time (`InviteCode`) didn't match the Group name used when broadcasting (`team-{teamId}`)** — meaning no one could ever receive a message. Fixed by standardizing on the raw `InviteCode` everywhere, via a shared naming helper.
2. **React 18 StrictMode double-connect race.** Gemini traced intermittent "SignalR not connected" errors to StrictMode's mount→unmount→remount cycle racing the async connection handshake — the cleanup could run before `connection.start()` resolved. Fixed with an `ensureConnection()` polling helper, and a hard rule (learned the hard way, after several regressions) that the guard must check `!this.connection` (object existence), never a custom `isConnected` boolean that stays false mid-handshake.
3. **"Ghost" duplicate connections.** The same user would appear connected multiple times simultaneously, so `Clients.GroupExcept` only excluded one of several live connections for that user. Two independent causes were found across sessions: **Vite HMR** not disposing the old WebSocket on hot-reload (fixed with `import.meta.hot.dispose(() => connection.stop())`), and **StrictMode's double-invoke in dev** creating a genuine ghost connection that joined a room while the surviving connection never did — confirmed by temporarily disabling `<React.StrictMode>` to verify, then permanently fixed by ensuring `connect()` always `stop()`s and nulls out any prior connection before building a new one, with an `isConnecting` lock.
4. **Silent EF Core XP-reset bug.** `UpdateTeamXPAsync` used `Attach(new Team { Id = teamId })` then `team.TotalTeamXP += earnedXP` — since the stub entity's other fields defaulted to 0, this generated `UPDATE ... SET TotalTeamXP = @earnedXP`, **overwriting** the real total instead of incrementing it. Fixed by loading the real entity via `FindAsync` first.
5. **Architectural hardening ("Thin Hub" principle).** Once the transport-level bugs were fixed, Gemini pushed further architectural review: `TeamHub` should contain zero business logic (only connection lifecycle — `OnConnectedAsync` auto-joining a user's room from their DB-derived team, mirroring `OnDisconnectedAsync`); all broadcasting should happen from Services via injected `IHubContext<TeamHub>`; the actor who triggers an event should be excluded from their own broadcast via `Clients.GroupExcept(room, [connectionId])`, since their own HTTP response already updates their UI (a genuine security/correctness point Gemini raised independently: never trust a frontend-supplied `userId` for permission-sensitive group operations — only `connectionId`, which carries no authority, is safe to accept from the client).
6. **Final root cause of remaining flakiness.** After all of the above, mid-session team joins (no page refresh) still didn't receive broadcasts, traced to `OnConnectedAsync` only firing once at connection time — a user joining a team via the HTTP API mid-session was never retroactively added to the new SignalR group. Fixed with an explicit `Groups.AddToGroupAsync` call from the `JoinTeamAsync`/`LeaveTeamAsync` service methods themselves, using a `connectionId` passed from the frontend for this purpose only.

This thread also surfaced a design decision that was **deliberately rejected**: giving the frontend a way to call a "team XP update" endpoint directly was considered and explicitly rejected on Gemini's advice, since a malicious client could invoke it repeatedly to inflate scores — XP updates are computed and broadcast exclusively from within the same transaction as mission completion, server-side.

---

## Phase 4 — Backend Unit Testing (22 Jul 2026)

*Tool: DeepSeek (test generation), Gemini (environment-failure debugging). Commit: `fe9089d`.*

- **Test strategy pivot: Moq → EF Core InMemory → SQLite.** Initial `GamificationServiceTests` used `Mock<ApplicationDbContext>`, which failed outright (`ArgumentException: Can not instantiate proxy... Could not find a parameterless constructor`), then failed again on non-virtual `DbSet` properties (`Unsupported expression: x => x.Badges`). Rather than marking every `DbSet` `virtual` purely for testability, the author asked what switching to a real database provider would cost the production codebase — DeepSeek confirmed **zero changes** to `ApplicationDbContext`/`Program.cs` would be needed. The author then asked specifically about SQLite over the simpler EF Core InMemory option; DeepSeek recommended SQLite because it enforces foreign keys and real SQL translation, catching bugs InMemory would silently miss — adopted as the standing test-database strategy (see [02-architecture.md §10](02-architecture.md)).
- **Test-writing debugging patterns that recurred across every service** (`GamificationService`, `EcoMissionService`, `BadgeService`, `UserMissionService`, `TeamService` — 5 suites, ~130 tests): `RuntimeBinderException` from using `dynamic` to read anonymous-type properties across the test/production assembly boundary (fixed by defining explicit test-only DTOs like `LeaderboardEntry`/`StatsDto`); Moq being unable to verify SignalR's `SendAsync` since it's a static extension method (must verify the real `SendCoreAsync` instead); and forgetting `SaveChangesAsync()` after a service call that only tracks entities in memory.
- **Genuine business-logic bugs the tests caught** (not just test-code bugs): `EcoMissionService.DeleteMissionAsync` didn't check whether a mission was already inactive before re-deleting it — the author chose to add an idempotency guard (`"Mission is already inactive"`) rather than silently allow repeated soft-deletes.
- **Test-environment corruption (Gemini).** A separate, severe environment problem: Visual Studio's Test Explorer showed all tests as "Not Run" (a compile error, not a test failure), which cascaded through several misdiagnoses (Moq/DbContext proxy errors, then a `PendingModelChangesWarning` from an out-of-date migration after adding `virtual` keywords, then an `NU1903` NuGet vulnerability warning aborting `Add-Migration` in the Package Manager Console, then finally an `Azure.Core` assembly-version conflict from stale `net8.0` metadata in `deps.json` surviving the project's upgrade to `net10.0`). After `bin`/`obj` cleanup and cache-clearing didn't resolve it, the author **deleted and recreated the test project from scratch**, re-adding old test files back incrementally to confirm each one still worked — the pragmatic fix once incremental debugging stalled.

---

## Phase 5 — Controller / Integration Testing (22 Jul 2026)

*Tool: DeepSeek. Commits: `844072a`, `0e5e2d3`, `bbb6faf`.*

- **`WebApplicationFactory` + InMemory DB conflicts.** Integration tests initially crashed with "Services for database providers 'SqlServer', 'InMemory' have been registered... only a single database provider can be registered," because `Program.cs`'s `SeedData.InitializeAsync` ran against SQL Server even when the test factory swapped in an InMemory context. Fixed by gating seeding behind `!IsEnvironment("Testing")`. A second cascading error, "Scheme already exists: Identity.Application," came from re-calling `AddIdentity` in the test's `ConfigureServices` — fixed by **not** re-registering Identity at all, only swapping the `DbContext` registration.
- **Shared `HttpClient` header bug.** Four tests expecting `403 Forbidden` for non-admin users instead returned `200 OK`, traced to a test helper (`CreateTestMissionAsync`) that mutated the shared `_client.DefaultRequestHeaders.Authorization` with an admin token as a side effect, silently promoting subsequent "regular user" requests to admin. Fixed by building standalone `HttpRequestMessage`s with their own auth header per request instead of mutating shared client state.
- **`ApiResponse<bool>` null-deserialization.** Failure-path responses serialize `Data: null`, which can't deserialize into a non-nullable `bool` — fixed by deserializing failure-path assertions as `ApiResponse<object>` instead.
- Final backend test count: **177 tests** across 5 service suites and 5 controller suites (Auth, EcoMissions, Badges, UserMissions, Teams).

### Frontend testing (DeepSeek, primary; Gemini for one review pass)
- **Explicit convention decision:** the author instructed that all code — including test code — must contain no non-English text, since the project targets an English-speaking audience; chat discussion could stay in Chinese. Applied consistently from this point on.
- **`EMFILE: too many open files` crash**, recurring across the Dashboard and Login/Register test files, traced specifically to Vitest/Vite exhausting file handles resolving `@mui/icons-material`'s many individual icon files. Fixed by mocking the whole `@mui/icons-material` module with lightweight `<span>` stubs instead of letting Vitest resolve real icon files.
- **Login/Register tests deliberately deprioritized.** After EMFILE persisted even in a minimal test file, the author asked directly whether these pages would simply go untested — DeepSeek's answer (accepted) was that this was a temporary deferral, not a gap, since their core logic (the actual `login`/`register` calls and redirect behavior) was already covered by `useAuthStore.test.ts` and `ProtectedRoute.test.tsx`.
- **`useAuthStore.test.ts` mocking approach.** After `vi.mock` factory hoisting repeatedly broke (`ReferenceError: Cannot access 'mockPost' before initialization`), the author abandoned `vi.mock` in favor of `vi.spyOn(apiClient, 'post'/'get')`, sidestepping the hoisting problem entirely.
- Final frontend test count: **~85+ tests** across `useAuthStore`, `client.ts`, `ProtectedRoute`, `MissionCard`, Dashboard, MyMissions, Teams, Profile, and Leaderboard.

---

## Phase 6 — Deployment & Documentation (27–31 Jul 2026)

### Azure deployment saga (Gemini — `Gemini Visual-Studio-Azure-订阅问题排查.md`)
A long, iterative troubleshooting process, condensed to its key resolutions:
- **"Azure subscription not found" in Visual Studio** — traced to a stale tenant filter/expired ARM token; worked around permanently by downloading the App Service's `.PublishSettings` file and using VS's "Import Profile," bypassing account login entirely.
- **Cost control** — Azure defaulted the SQL Database to the Hyperscale tier (~$317–333/month estimate); switched to Basic tier (~$5/month). Same instinct applied to the Web App: F1 (Free) tier initially, later needing a B1 upgrade.
- **Subscription cancellation mid-project** — one Azure subscription entered "Cancellation in progress" and became read-only; the author switched everything to a second, active subscription and rebuilt all resources from scratch in a new resource group rather than fighting the frozen one.
- **Deployment split decision** — after comparing three options (both on Azure / backend-Azure+frontend-Vercel / merged single-service), the author chose backend on Azure App Service + frontend on **Azure Static Web Apps** (not Vercel, per the final architecture — see [02-architecture.md §11](02-architecture.md)).
- **Production 503 crash** — `LocalDBNotSupportedException` on Linux App Service, root-caused to an App Settings vs. Connection Strings tab naming mismatch (`ConnectionStrings__DefaultConnection` vs. a differently-typed entry), which meant the app silently fell back to a local dev connection string in production.
- **SignalR + CORS credentials** — `/teamHub/negotiate` failed because Azure Portal's own CORS UI conflicted with the code-level CORS policy and never returned `Access-Control-Allow-Credentials: true`; fixed by clearing the Portal-level CORS list entirely and relying solely on `Program.cs`'s policy with explicit (non-wildcard) origins, since `AllowCredentials()` forbids `*`.
- **CI build failures** — GitHub Actions failed first because leftover TypeScript errors in test files broke `tsc -b` (so `vite build` never actually ran, and Azure tried to upload raw `node_modules`), then again because `VITE_API_BASE_URL` wasn't available to the build step, baking the `localhost` fallback into the production bundle. Fixed by excluding test files from the build's `tsconfig` and passing the env var explicitly in the workflow YAML.

### Backend test-runner debugging (Gemini)
A separate, earlier thread (`Gemini ASP.NET-测试未运行原因分析.md`) diagnosing the Test Explorer "Not Run" cascade described in Phase 4 — included here for completeness since it directly gated whether the 177 backend tests could run at all in Visual Studio.

### Documentation and specs (DeepSeek)
- **README** generated first as a Chinese draft for review, then translated in full to English — covering the tech stack, deployment links, and an advanced-features checklist.
- **Security checklist honesty check.** Asked DeepSeek to confirm which items from the PDF's security-requirements list were *actually* implemented, not just claimed. Result: RBAC and password hashing (via ASP.NET Core Identity) and data validation were kept in the README; **rate limiting was explicitly left out** of the claimed list since it was never implemented, and Anti-CSRF was noted as not applicable to a JWT+localStorage architecture rather than falsely claimed.
- **The `/specs` folder — "evidence, not documentation."** After an initial round where DeepSeek judged the four drafted specs files (`01-planning.md`, `02-architecture.md`, `03-AI-prompts.md`, `04-self-reflection.md`) as satisfying the brief, the author pushed back directly: *"What they want is evidence, but we've only given them a document."* DeepSeek agreed this was the right distinction — 01/02 are fine as polished final documents, but 03 needed to be restructured around actual prompt/response/decision evidence rather than a retrospective summary. This is the same conclusion independently reached in the present Claude conversation, and the reason this document exists in its current form.
- **Language decision.** DeepSeek was first asked whether submitting the AI-prompts record in Chinese was acceptable, and initially said yes (incorrectly claiming Chinese was an official language of New Zealand). The author corrected this and pointed out the more fundamental problem — evaluators who can't read Chinese can't assess the evidence at all — and decided to regenerate the record fully in English, which is the standard this document (produced via a separate, later process cross-referencing the raw DeepSeek/Gemini/Qwen logs against `git log`) follows.

---

## Appendix — Source Log Inventory

All raw conversation exports referenced above are retained by the author outside the repository (not committed, due to volume — one DeepSeek log alone is ~2.9MB / 74,876 lines). For traceability, the 35 source files were:

| Tool | Count | Example topics |
| :--- | :--- | :--- |
| DeepSeek | 1 (≈226 exchanges) | Full project lifecycle, ideation → deployment |
| Gemini | 17 | Azure/VS deployment, useEffect/SignalR analysis, component optimization (Mission/Dashboard/Leaderboard/Teams/API-Controller), MUI prop migrations, 401 debugging, ASP.NET test-runner failures |
| Qwen | 17 | CORS setup, SignalR contract debugging, Zustand/Axios auth architecture (2 passes), React 19 `use()` migration, MUI Grid v2 migration, C# interface/DI questions |
