import {
  decodeJwtEmail,
  getBearerToken,
  json,
  ok,
  methodNotAllowed,
  serverError,
  unauthorized,
} from "../_shared/http.mjs";

function toCamelCase(value) {
  if (Array.isArray(value)) {
    return value.map(toCamelCase);
  }

  if (value && typeof value === "object") {
    return Object.fromEntries(
      Object.entries(value).map(([key, entryValue]) => {
        const normalizedKey = key.length > 0
          ? `${key[0].toLowerCase()}${key.slice(1)}`
          : key;
        return [normalizedKey, toCamelCase(entryValue)];
      }),
    );
  }

  return value;
}

export function createProfileBootstrapHandler({
  bootstrapProfile,
} = {}) {
  if (typeof bootstrapProfile !== "function") {
    throw new Error("bootstrapProfile dependency is required.");
  }

  return async function handleProfileBootstrap(request) {
    if (request.method === "OPTIONS") {
      return ok();
    }

    if (request.method !== "POST") {
      return methodNotAllowed();
    }

    const accessToken = getBearerToken(request);
    if (!accessToken) {
      return unauthorized();
    }

    try {
      const snapshot = await bootstrapProfile(accessToken);
      return json({
        isAuthenticated: true,
        userEmail: decodeJwtEmail(accessToken),
        snapshot: toCamelCase(snapshot),
      });
    } catch (error) {
      return serverError(error instanceof Error ? error.message : undefined);
    }
  };
}
