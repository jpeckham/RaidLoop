# Local Email/Password Auth Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add a reliable local-development sign-up/sign-in path using email and password in the `Local` environment, while preserving Google sign-in for non-local environments.

**Architecture:** The auth gate currently exposes only Google sign-in, while local Supabase Auth already supports email/password. This increment keeps environment-specific auth choice in `AuthGate.razor`, adds email/password methods to `SupabaseAuthService`, and leaves hosted Google auth untouched. Tests should pin the local auth UI, the new service surface, and the continued presence of the Google flow for non-local use.

**Tech Stack:** Blazor WebAssembly, C#, Supabase Auth client, Razor components, xUnit, .NET test runner

---

### Task 1: Pin the local auth gate requirements with failing tests

**Files:**
- Modify: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`
- Test: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`

**Step 1: Write the failing test**

Add binding tests that require:
- `AuthGate.razor` to branch on the host environment
- a local email input
- a local password input
- `Sign in` and `Sign up` buttons for local use
- the existing Google sign-in button to still exist for non-local use

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter HomeMarkupBindingTests`

Expected: FAIL because the auth gate currently only renders a Google button.

**Step 3: Write minimal implementation**

Only add the tests. Do not change production code yet.

**Step 4: Run test to verify it still fails for the expected reason**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter HomeMarkupBindingTests`

Expected: FAIL with missing local auth controls and environment branching.

**Step 5: Commit**

```bash
git add tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs
git commit -m "test: pin local email auth gate requirements"
```

### Task 2: Add email/password auth methods to the client auth service

**Files:**
- Modify: `src/RaidLoop.Client/Services/SupabaseAuthService.cs`
- Modify: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`
- Test: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`

**Step 1: Write the failing test**

Extend the binding/service-shape tests to assert:
- `SupabaseAuthService` exposes `SignInWithEmailPasswordAsync`
- `SupabaseAuthService` exposes `SignUpWithEmailPasswordAsync`
- those methods call Supabase Auth with email/password credentials
- existing Google PKCE behavior remains present

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter HomeMarkupBindingTests`

Expected: FAIL because the new methods do not exist yet.

**Step 3: Write minimal implementation**

Update `SupabaseAuthService` to:
- initialize the client if needed
- call Supabase Auth email/password sign-in
- call Supabase Auth email/password sign-up
- persist returned sessions if present
- raise auth state changes the same way existing auth flows do

Do not change the auth gate UI in this task.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter HomeMarkupBindingTests`

Expected: PASS for the auth service shape assertions, with remaining failures limited to the auth gate UI.

**Step 5: Commit**

```bash
git add src/RaidLoop.Client/Services/SupabaseAuthService.cs tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs
git commit -m "feat: add email password auth methods"
```

### Task 3: Render local email/password auth controls in the auth gate

**Files:**
- Modify: `src/RaidLoop.Client/Components/AuthGate.razor`
- Modify: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`
- Test: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`

**Step 1: Write the failing test**

Extend the auth gate binding test to assert:
- `AuthGate.razor` injects or consumes the host environment
- `Local` renders email/password controls plus `Sign in` and `Sign up`
- non-local continues to render the Google button
- an inline auth error placeholder exists

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter HomeMarkupBindingTests`

Expected: FAIL because the auth gate has not been updated yet.

**Step 3: Write minimal implementation**

Update `AuthGate.razor` so:
- `Local` shows email/password form fields and buttons
- non-local shows the Google button
- the component tracks local form state and renders an error string when auth actions fail

Keep the signed-in state rendering unchanged.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter HomeMarkupBindingTests`

Expected: PASS for the auth gate assertions.

**Step 5: Commit**

```bash
git add src/RaidLoop.Client/Components/AuthGate.razor tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs
git commit -m "feat: add local email password auth gate"
```

### Task 4: Verify targeted suites and local development behavior

**Files:**
- Modify: `docs/plans/2026-03-22-local-email-password-auth.md`
- Test: `tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj`

**Step 1: Run the targeted auth-related .NET tests**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "HomeMarkupBindingTests|ProfileMutationFlowTests"`

Expected: PASS

**Step 2: Run the full solution test suite**

Run: `dotnet test RaidLoop.sln`

Expected: PASS

**Step 3: Inspect the diff**

Run:

```bash
git diff -- src/RaidLoop.Client/Components/AuthGate.razor src/RaidLoop.Client/Services/SupabaseAuthService.cs tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs docs/plans/2026-03-22-local-email-password-auth.md
```

Confirm:
- local email/password controls only appear in `Local`
- Google sign-in remains for non-local
- auth service exposes email/password methods without removing PKCE

**Step 4: Update the plan doc status**

Document the actual verification commands and outcomes in this file once implementation is complete.

**Step 5: Commit**

```bash
git add src/RaidLoop.Client/Components/AuthGate.razor src/RaidLoop.Client/Services/SupabaseAuthService.cs tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs docs/plans/2026-03-22-local-email-password-auth.md
git commit -m "feat: add local email password auth flow"
```
