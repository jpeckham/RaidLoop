# Browser Observability Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add PostHog-backed browser error tracking with session replay, preserve console logging, and restyle the Blazor fatal error banner to a readable dark theme.

**Architecture:** The browser client will initialize PostHog through a dedicated JS wrapper in `wwwroot/js`, then route global browser errors and explicit Blazor-side handled exceptions into structured PostHog events. Blazor code will use a small telemetry service over JS interop, while the existing fatal crash banner remains but gets a dark high-contrast style.

**Tech Stack:** Blazor WebAssembly, JavaScript interop, PostHog browser SDK, xUnit source-level tests

---

### Task 1: Add Client Configuration For PostHog

**Files:**
- Modify: `C:\Users\james\source\repos\extractor-shooter-light\src\RaidLoop.Client\wwwroot\appsettings.json`
- Modify: `C:\Users\james\source\repos\extractor-shooter-light\src\RaidLoop.Client\Configuration\SupabaseOptions.cs`
- Test: `C:\Users\james\source\repos\extractor-shooter-light\tests\RaidLoop.Core.Tests\HomeMarkupBindingTests.cs`

**Step 1: Write the failing test**

Add a source-level test that asserts `appsettings.json` contains a `PostHog` section with a project key placeholder/setting name and replay toggle.

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~<new test name>"`
Expected: FAIL because the `PostHog` section does not exist yet.

**Step 3: Write minimal implementation**

- Add a `PostHog` section to `appsettings.json`.
- Add a configuration record/class if needed for typed binding.
- Keep the project key and host configurable rather than hardcoding them in services.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~<new test name>"`
Expected: PASS

**Step 5: Commit**

```bash
git add src/RaidLoop.Client/wwwroot/appsettings.json src/RaidLoop.Client/Configuration/*.cs tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs
git commit -m "Add PostHog client configuration"
```

### Task 2: Add Browser Telemetry Wrapper

**Files:**
- Create: `C:\Users\james\source\repos\extractor-shooter-light\src\RaidLoop.Client\wwwroot\js\telemetry.js`
- Modify: `C:\Users\james\source\repos\extractor-shooter-light\src\RaidLoop.Client\wwwroot\index.html`
- Test: `C:\Users\james\source\repos\extractor-shooter-light\tests\RaidLoop.Core.Tests\HomeMarkupBindingTests.cs`

**Step 1: Write the failing test**

Add a source-level test asserting:
- `index.html` loads `js/telemetry.js`
- `telemetry.js` contains `window.onerror`
- `telemetry.js` contains `unhandledrejection`

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~<new test name>"`
Expected: FAIL because the script and handlers do not exist yet.

**Step 3: Write minimal implementation**

- Create `telemetry.js`.
- Load PostHog browser SDK from the wrapper.
- Initialize PostHog exactly once.
- Register global browser error and promise rejection handlers.
- Ensure each handler still writes to `console.error`.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~<new test name>"`
Expected: PASS

**Step 5: Commit**

```bash
git add src/RaidLoop.Client/wwwroot/js/telemetry.js src/RaidLoop.Client/wwwroot/index.html tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs
git commit -m "Add browser telemetry wrapper"
```

### Task 3: Add Blazor Telemetry Service

**Files:**
- Create: `C:\Users\james\source\repos\extractor-shooter-light\src\RaidLoop.Client\Services\ClientTelemetryService.cs`
- Create: `C:\Users\james\source\repos\extractor-shooter-light\src\RaidLoop.Client\Services\IClientTelemetryService.cs`
- Modify: `C:\Users\james\source\repos\extractor-shooter-light\src\RaidLoop.Client\Program.cs`
- Test: `C:\Users\james\source\repos\extractor-shooter-light\tests\RaidLoop.Core.Tests\HomeMarkupBindingTests.cs`

**Step 1: Write the failing test**

Add a source-level test that asserts `Program.cs` registers `IClientTelemetryService` and that the new service references JS interop for reporting handled errors.

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~<new test name>"`
Expected: FAIL because the service is not registered yet.

**Step 3: Write minimal implementation**

- Create a telemetry service abstraction.
- Use `IJSRuntime` to call into `telemetry.js`.
- Normalize handled exceptions into a consistent payload shape with `source`, `message`, `stack`, and optional context.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~<new test name>"`
Expected: PASS

**Step 5: Commit**

```bash
git add src/RaidLoop.Client/Services/ClientTelemetryService.cs src/RaidLoop.Client/Services/IClientTelemetryService.cs src/RaidLoop.Client/Program.cs tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs
git commit -m "Add Blazor telemetry reporting service"
```

### Task 4: Report Handled App Errors

**Files:**
- Modify: `C:\Users\james\source\repos\extractor-shooter-light\src\RaidLoop.Client\Pages\Home.razor`
- Modify: `C:\Users\james\source\repos\extractor-shooter-light\src\RaidLoop.Client\Pages\Home.razor.cs`
- Modify: `C:\Users\james\source\repos\extractor-shooter-light\src\RaidLoop.Client\Components\AuthGate.razor`
- Modify: `C:\Users\james\source\repos\extractor-shooter-light\src\RaidLoop.Client\Services\SupabaseAuthService.cs`
- Test: `C:\Users\james\source\repos\extractor-shooter-light\tests\RaidLoop.Core.Tests\ProfileMutationFlowTests.cs`
- Test: `C:\Users\james\source\repos\extractor-shooter-light\tests\RaidLoop.Core.Tests\HomeMarkupBindingTests.cs`

**Step 1: Write the failing test**

Add tests that verify handled bootstrap/action/auth failure paths invoke the telemetry service at meaningful boundaries.

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~ProfileMutationFlowTests|FullyQualifiedName~<new source-level test names>"`
Expected: FAIL because those paths do not yet report telemetry.

**Step 3: Write minimal implementation**

- Inject `IClientTelemetryService` where handled failures are surfaced.
- Report structured handled errors for:
  - bootstrap failures
  - game action failures
  - auth/session failures
- Avoid duplicate reporting inside tight low-level loops.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~ProfileMutationFlowTests|FullyQualifiedName~<new source-level test names>"`
Expected: PASS

**Step 5: Commit**

```bash
git add src/RaidLoop.Client/Pages/Home.razor src/RaidLoop.Client/Pages/Home.razor.cs src/RaidLoop.Client/Components/AuthGate.razor src/RaidLoop.Client/Services/SupabaseAuthService.cs tests/RaidLoop.Core.Tests/ProfileMutationFlowTests.cs tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs
git commit -m "Report handled client errors to telemetry"
```

### Task 5: Restyle The Fatal Banner

**Files:**
- Modify: `C:\Users\james\source\repos\extractor-shooter-light\src\RaidLoop.Client\wwwroot\css\app.css`
- Test: `C:\Users\james\source\repos\extractor-shooter-light\tests\RaidLoop.Core.Tests\HomeMarkupBindingTests.cs`

**Step 1: Write the failing test**

Add a source-level test asserting `#blazor-error-ui` uses a dark background and high-contrast foreground instead of the current tan/light-only styling.

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~<new style test name>"`
Expected: FAIL because `app.css` still uses the tan banner.

**Step 3: Write minimal implementation**

- Change the banner background to a dark color already in the app palette.
- Set readable foreground/link colors.
- Keep layout/position behavior unchanged unless readability requires a small spacing tweak.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~<new style test name>"`
Expected: PASS

**Step 5: Commit**

```bash
git add src/RaidLoop.Client/wwwroot/css/app.css tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs
git commit -m "Restyle Blazor fatal error banner"
```

### Task 6: Verify End-To-End Integration

**Files:**
- Verify only

**Step 1: Run focused automated tests**

Run:

```bash
dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~HomeMarkupBindingTests|FullyQualifiedName~ProfileMutationFlowTests"
```

Expected: PASS

**Step 2: Run broader automated tests**

Run:

```bash
dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj
```

Expected: PASS

**Step 3: Manual browser verification**

Run the app locally and verify:
- PostHog initializes without breaking load
- session replay starts
- a handled action error emits a telemetry event and still logs to the console
- an unhandled JS error emits a telemetry event and still logs to the console
- the fatal banner is dark and readable

**Step 4: Commit**

```bash
git add .
git commit -m "Verify browser observability integration"
```
