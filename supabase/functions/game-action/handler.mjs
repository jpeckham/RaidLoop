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

const RAID_START_ACTIONS = new Set([
  "start-main-raid",
  "start-random-raid",
]);

const COMBAT_ACTIONS = new Set([
  "attack",
  "burst-fire",
  "reload",
  "flee",
  "use-medkit",
]);

const LOOT_ACTIONS = new Set([
  "take-loot",
  "drop-carried",
  "drop-equipped",
  "equip-from-discovered",
  "equip-from-carried",
]);

const ENCOUNTER_ACTIONS = new Set([
  "continue-searching",
  "move-toward-extract",
  "attempt-extract",
]);

const IN_RAID_ACTIONS = new Set([
  ...COMBAT_ACTIONS,
  ...LOOT_ACTIONS,
  ...ENCOUNTER_ACTIONS,
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
      return json(buildActionResponse(body.action, body.payload, snapshot));
    } catch (error) {
      return serverError(error instanceof Error ? error.message : undefined);
    }
  };
}

function buildActionResponse(action, payload, snapshot) {
  if (PROFILE_MUTATION_ACTIONS.has(action)) {
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

  if (RAID_START_ACTIONS.has(action)) {
    return {
      eventType: "RaidStarted",
      event: {
        action,
      },
      projections: buildRaidStartProjections(action, snapshot),
      snapshot,
      message: null,
    };
  }

  if (IN_RAID_ACTIONS.has(action)) {
    if (!snapshot?.activeRaid) {
      return {
        eventType: "RaidFinished",
        event: {
          action,
        },
        projections: buildRaidFinishedProjections(snapshot),
        snapshot,
        message: null,
      };
    }

    return {
      eventType: resolveRaidEventType(action),
      event: {
        action,
      },
      projections: buildInRaidProjections(snapshot, getKnownLogCount(payload)),
      snapshot,
      message: null,
    };
  }

  return {
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

function buildRaidStartProjections(action, snapshot) {
  const projections = {
    raid: buildRaidProjection(snapshot?.activeRaid),
  };

  if (action === "start-random-raid") {
    projections.luckRun = buildLuckRunProjection(snapshot);
  }

  return projections;
}

function buildRaidProjection(activeRaid) {
  return buildRaidProjectionWithLogOptions(activeRaid, {
    includeFullLogEntries: true,
    knownLogCount: 0,
  });
}

function buildRaidProjectionWithLogOptions(activeRaid, { includeFullLogEntries, knownLogCount }) {
  const logEntries = Array.isArray(activeRaid?.logEntries) ? activeRaid.logEntries : [];
  const projection = {
    health: activeRaid?.health ?? 0,
    backpackCapacity: activeRaid?.backpackCapacity ?? 0,
    ammo: activeRaid?.ammo ?? 0,
    weaponMalfunction: activeRaid?.weaponMalfunction ?? false,
    medkits: activeRaid?.medkits ?? 0,
    lootSlots: activeRaid?.lootSlots ?? 0,
    extractProgress: activeRaid?.extractProgress ?? 0,
    extractRequired: activeRaid?.extractRequired ?? 0,
    encounterType: activeRaid?.encounterType ?? "Neutral",
    encounterTitle: activeRaid?.encounterTitle ?? "",
    encounterDescription: activeRaid?.encounterDescription ?? "",
    enemyName: activeRaid?.enemyName ?? "",
    enemyHealth: activeRaid?.enemyHealth ?? 0,
    lootContainer: activeRaid?.lootContainer ?? "",
    awaitingDecision: activeRaid?.awaitingDecision ?? false,
    discoveredLoot: activeRaid?.discoveredLoot ?? [],
    carriedLoot: activeRaid?.carriedLoot ?? [],
    equippedItems: activeRaid?.equippedItems ?? [],
  };

  if (includeFullLogEntries) {
    projection.logEntries = logEntries;
  } else {
    projection.logEntriesAdded = logEntries.slice(Math.max(knownLogCount, 0));
  }

  return projection;
}

function buildInRaidProjections(snapshot, knownLogCount) {
  return {
    raid: buildRaidProjectionWithLogOptions(snapshot?.activeRaid, {
      includeFullLogEntries: false,
      knownLogCount,
    }),
  };
}

function buildRaidFinishedProjections(snapshot) {
  return {
    loadout: buildLoadoutProjection(snapshot),
    luckRun: buildLuckRunProjection(snapshot),
    raid: null,
  };
}

function resolveRaidEventType(action) {
  if (COMBAT_ACTIONS.has(action)) {
    return "CombatResolved";
  }

  if (LOOT_ACTIONS.has(action)) {
    return "LootResolved";
  }

  return "EncounterAdvanced";
}

function getKnownLogCount(payload) {
  if (!payload || typeof payload !== "object") {
    return 0;
  }

  return Number.isInteger(payload.knownLogCount) ? payload.knownLogCount : 0;
}
