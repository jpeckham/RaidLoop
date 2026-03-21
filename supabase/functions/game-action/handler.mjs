import {
  badRequest,
  getBearerToken,
  json,
  methodNotAllowed,
  ok,
  serverError,
  unauthorized,
} from "../_shared/http.mjs";

const PROFILE_MUTATION_ACTIONS = new Set([
  "sell-stash-item",
  "move-stash-to-on-person",
  "sell-on-person-item",
  "stash-on-person-item",
  "equip-on-person-item",
  "unequip-on-person-item",
  "buy-from-shop",
  "store-luck-run-item",
  "move-luck-run-item-to-on-person",
  "sell-luck-run-item",
]);

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
      return json(buildActionResponse(body.action, snapshot));
    } catch (error) {
      return serverError(error instanceof Error ? error.message : undefined);
    }
  };
}

function buildActionResponse(action, snapshot) {
  if (!PROFILE_MUTATION_ACTIONS.has(action)) {
    return {
      snapshot,
      message: null,
    };
  }

  return {
    eventType: "ProfileMutated",
    event: {
      action,
    },
    projections: buildProfileMutationProjections(action, snapshot),
    snapshot,
    message: null,
  };
}

function buildProfileMutationProjections(action, snapshot) {
  const projections = {};

  if (action === "sell-stash-item"
    || action === "move-stash-to-on-person"
    || action === "sell-on-person-item"
    || action === "buy-from-shop"
    || action === "sell-luck-run-item") {
    projections.economy = buildEconomyProjection(snapshot);
  }

  if (action === "sell-stash-item"
    || action === "move-stash-to-on-person"
    || action === "sell-on-person-item"
    || action === "stash-on-person-item"
    || action === "equip-on-person-item"
    || action === "unequip-on-person-item"
    || action === "buy-from-shop"
    || action === "move-luck-run-item-to-on-person") {
    projections.loadout = buildLoadoutProjection(snapshot);
  }

  if (action === "sell-stash-item"
    || action === "move-stash-to-on-person"
    || action === "stash-on-person-item"
    || action === "store-luck-run-item") {
    projections.stash = buildStashProjection(snapshot);
  }

  if (action === "store-luck-run-item"
    || action === "move-luck-run-item-to-on-person"
    || action === "sell-luck-run-item") {
    projections.luckRun = buildLuckRunProjection(snapshot);
  }

  return projections;
}

function buildEconomyProjection(snapshot) {
  return {
    money: snapshot?.money ?? 0,
  };
}

function buildStashProjection(snapshot) {
  return {
    mainStash: snapshot?.mainStash ?? [],
  };
}

function buildLoadoutProjection(snapshot) {
  return {
    onPersonItems: snapshot?.onPersonItems ?? [],
  };
}

function buildLuckRunProjection(snapshot) {
  return {
    randomCharacterAvailableAt: snapshot?.randomCharacterAvailableAt ?? null,
    randomCharacter: snapshot?.randomCharacter ?? null,
  };
}
