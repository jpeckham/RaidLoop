# Browser Observability Design

**Date:** 2026-03-21

## Goal

Add browser-side error observability for the Blazor WebAssembly client so unhandled and handled runtime failures are captured in PostHog with session replay, while preserving existing console logging and improving fatal error banner readability.

## Scope

- Integrate PostHog into the browser client.
- Enable session replay.
- Capture global browser errors and unhandled promise rejections.
- Capture handled application errors from bootstrap, auth, and action flows.
- Preserve console logging for all reported errors.
- Restyle the Blazor fatal error banner to keep the app's dark look.

## Non-Goals

- Building a custom Supabase-backed error log store.
- Adding server-side application monitoring.
- Retrying or queueing telemetry offline.
- Full-featured analytics instrumentation beyond error and replay support.

## Recommended Approach

Use the PostHog browser SDK directly in the client with a small local telemetry wrapper. The wrapper will initialize PostHog once, install global error handlers, and expose functions that Blazor code can call for handled exceptions.

This keeps the logging contract app-owned while avoiding a custom backend pipeline. The project token is a browser-public key and will be configured through client app settings rather than embedded ad hoc in code.

## Data Flow

1. The browser loads the PostHog wrapper from `wwwroot/js`.
2. The wrapper initializes PostHog with session replay enabled.
3. Global handlers capture:
   - `window.onerror`
   - `window.unhandledrejection`
4. Blazor code explicitly reports handled failures through a JS interop service.
5. Each report:
   - writes to `console.error`
   - sends a structured PostHog event
6. The built-in Blazor fatal error UI remains in place, but with dark styling.

## Event Model

Use a small set of explicit event names:

- `client_error`
  - handled application errors from Blazor code
- `client_unhandled_error`
  - uncaught browser errors
- `client_unhandled_rejection`
  - unhandled promise rejections
- `client_blazor_fatal`
  - fatal UI error state associated with the Blazor crash banner

Each event should include:

- `message`
- `stack`
- `source`
- `severity`
- `url`
- `userAgent`
- `timestamp`
- `appVersion` if available
- `context` for action/bootstrap/auth metadata when present

## Configuration

Add a PostHog configuration section in client app settings for:

- public project key
- API host
- replay enabled flag

Keep defaults conservative and fail open. If PostHog cannot initialize, the app should continue running and still log to the console.

## Error Capture Strategy

### Global Errors

Install JavaScript listeners for browser-level failures and forward normalized payloads to PostHog.

### Handled Errors

Add a Blazor service for explicit reporting from:

- profile bootstrap failures
- game action failures
- auth/session failures
- any other existing catch blocks that currently only surface UI errors

### Fatal Banner

Preserve the current Blazor crash UI but restyle it for readability with a dark background, high-contrast text, and matching action links.

## UI Styling

The current fatal banner uses a light tan background with white text, which has poor contrast. Replace it with a dark background that matches the rest of the app palette while keeping the banner fixed and obvious.

## Testing Strategy

- Add source-level tests to ensure:
  - PostHog config exists
  - the telemetry script is referenced by the client
  - global handlers are installed
  - the fatal banner keeps dark styling
- Add unit tests for the Blazor-side telemetry service and any new handled-error call sites where practical.

## Risks

- Session replay adds network overhead.
- Browser extensions may block PostHog.
- Over-reporting duplicate errors is possible if both a local catch block and a global handler emit the same failure.

## Mitigations

- Keep event names and sources explicit to aid filtering.
- Prefer handled-error reporting only at meaningful boundaries, not every low-level catch.
- Make the JS wrapper resilient if PostHog is unavailable.
