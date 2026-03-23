# Local Email/Password Auth Design

**Date:** 2026-03-22

**Goal:** Make local development use a reliable, non-Google sign-in path by adding email/password sign up and sign in for the `Local` environment, while preserving Google sign-in for non-local environments.

## Problem

The local Supabase workflow is now the default development path, but the client auth gate only exposes `Sign in with Google`. That creates two issues:

- local Google OAuth is not fully configured in repo defaults and is brittle for everyday development
- developers cannot easily sign in to the local Supabase stack, even though local email/password auth is already supported by `supabase/config.toml`

## Recommended Approach

Show a local-only email/password form in the auth gate when the app runs in `Local`, and keep the current Google sign-in button for non-local environments.

This keeps the product-facing Google flow intact while making local development deterministic and easy to use.

## UI Design

In `AuthGate.razor`:

- if the host environment is `Local`, render:
  - email input
  - password input
  - `Sign in` button
  - `Sign up` button
- if the host environment is not `Local`, render the existing `Sign in with Google` button
- show a small inline error message when an auth attempt fails

This keeps the environment-specific auth choice in one place and avoids exposing a broken local Google path.

## Service Design

Add two methods to `SupabaseAuthService`:

- `SignInWithEmailPasswordAsync(string email, string password)`
- `SignUpWithEmailPasswordAsync(string email, string password)`

Behavior:

- initialize the Supabase client if needed
- call local Supabase Auth using email/password
- persist any returned session using the existing session storage path
- raise auth state changes the same way the existing OAuth flow does
- let the UI catch and render failures

Environment branching should stay in the UI layer, not inside the auth service.

## Scope Boundaries

This increment includes:

- local-only email/password sign-in and sign-up UI
- email/password auth methods in the client auth service
- tests for the new local auth gate shape and service surface

This increment does not include:

- fixing local Google OAuth
- changing hosted Google auth behavior
- changing remote auth provider configuration

## Testing Strategy

Add tests that pin:

- `AuthGate.razor` renders local email/password controls
- `AuthGate.razor` still renders the Google button for non-local flows
- `SupabaseAuthService` exposes and uses email/password auth methods
- existing Google/PKCE tests remain intact

The implementation remains small and local-dev-focused while preserving the current hosted sign-in behavior.
