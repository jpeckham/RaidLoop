import { corsHeaders } from "./cors.mjs";

export function methodNotAllowed() {
  return new Response("Method Not Allowed", { status: 405, headers: corsHeaders });
}

export function ok() {
  return new Response("ok", { status: 200, headers: corsHeaders });
}

export function json(body, init = {}) {
  const headers = new Headers(corsHeaders);
  for (const [key, value] of Object.entries(init.headers ?? {})) {
    headers.set(key, value);
  }
  headers.set("Content-Type", "application/json");

  return new Response(JSON.stringify(body), {
    ...init,
    headers,
  });
}

export function unauthorized() {
  return json({ error: "Authentication required." }, { status: 401 });
}

export function badRequest(message) {
  return json({ error: message }, { status: 400 });
}

export function serverError(message = "Unexpected server error.") {
  return json({ error: message }, { status: 500 });
}

export function getBearerToken(request) {
  const authorization = request.headers.get("Authorization") ?? "";
  const [scheme, token] = authorization.split(" ", 2);
  if (scheme?.toLowerCase() !== "bearer" || !token) {
    return null;
  }

  return token;
}

export function decodeJwtEmail(accessToken) {
  const parts = accessToken.split(".");
  if (parts.length < 2) {
    return null;
  }

  try {
    const base64 = parts[1].replace(/-/g, "+").replace(/_/g, "/");
    const padded = base64.padEnd(Math.ceil(base64.length / 4) * 4, "=");
    const payload = JSON.parse(atob(padded));
    return typeof payload.email === "string" ? payload.email : null;
  } catch {
    return null;
  }
}
