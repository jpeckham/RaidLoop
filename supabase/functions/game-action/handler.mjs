import {
  badRequest,
  getBearerToken,
  json,
  methodNotAllowed,
  ok,
  serverError,
  unauthorized,
} from "../_shared/http.mjs";

export function createGameActionHandler({
  dispatchAction,
} = {}) {
  if (typeof dispatchAction !== "function") {
    throw new Error("dispatchAction dependency is required.");
  }

  return async function handleGameAction(request) {
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

    let body;
    try {
      body = await request.json();
    } catch {
      return badRequest("Action and payload are required.");
    }

    if (typeof body?.action !== "string" || body.payload === undefined) {
      return badRequest("Action and payload are required.");
    }

    try {
      const snapshot = await dispatchAction(accessToken, body.action, body.payload);
      return json({
        snapshot,
        message: null,
      });
    } catch (error) {
      return serverError(error instanceof Error ? error.message : undefined);
    }
  };
}
