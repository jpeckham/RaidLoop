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
  "accept-stats",
  "reallocate-stats",
]);

const RAID_START_ACTIONS = new Set([
  "start-main-raid",
  "start-random-raid",
]);

const COMBAT_ACTIONS = new Set([
  "attack",
  "burst-fire",
  "full-auto",
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
  "go-deeper",
  "move-toward-extract",
  "stay-at-extract",
  "start-extract-hold",
  "resolve-extract-hold",
  "cancel-extract-hold",
  "attempt-extract",
]);

const IN_RAID_ACTIONS = new Set([
  ...COMBAT_ACTIONS,
  ...LOOT_ACTIONS,
  ...ENCOUNTER_ACTIONS,
]);

const ITEM_DEF_ID_BY_LEGACY_NAME = new Map([
  ["Rusty Knife", 1],
  ["Light Pistol", 2],
  ["Makarov", 2],
  ["Drum SMG", 3],
  ["PPSH", 3],
  ["Field Carbine", 4],
  ["AK74", 4],
  ["Battle Rifle", 5],
  ["AK47", 5],
  ["Marksman Rifle", 6],
  ["SVDS", 6],
  ["Support Machine Gun", 7],
  ["PKP", 7],
  ["Soft Armor Vest", 8],
  ["6B2 body armor", 8],
  ["Reinforced Vest", 9],
  ["BNTI Kirasa-N", 9],
  ["Light Plate Carrier", 10],
  ["6B13 assault armor", 10],
  ["Medium Plate Carrier", 11],
  ["FORT Defender-2", 11],
  ["Heavy Plate Carrier", 12],
  ["6B43 Zabralo-Sh body armor", 12],
  ["Assault Plate Carrier", 13],
  ["NFM THOR", 13],
  ["Small Backpack", 14],
  ["Large Backpack", 15],
  ["Tactical Backpack", 16],
  ["Hiking Backpack", 17],
  ["Tasmanian Tiger Trooper 35", 17],
  ["Raid Backpack", 18],
  ["6Sh118", 18],
  ["Medkit", 19],
  ["Bandage", 20],
  ["Ammo Box", 21],
  ["Scrap Metal", 22],
  ["Rare Scope", 23],
  ["Legendary Trigger Group", 24],
]);

const DEFAULT_ITEM_RULES = [
  { itemDefId: 1, type: 0, weight: 1, slots: 1, rarity: 0 },
  { itemDefId: 2, type: 0, weight: 2, slots: 1, rarity: 0 },
  { itemDefId: 3, type: 0, weight: 12, slots: 1, rarity: 1 },
  { itemDefId: 4, type: 0, weight: 7, slots: 1, rarity: 2 },
  { itemDefId: 5, type: 0, weight: 10, slots: 1, rarity: 2 },
  { itemDefId: 6, type: 0, weight: 10, slots: 1, rarity: 3 },
  { itemDefId: 7, type: 0, weight: 18, slots: 1, rarity: 4 },
  { itemDefId: 8, type: 1, weight: 9, slots: 1, rarity: 0 },
  { itemDefId: 9, type: 1, weight: 7, slots: 1, rarity: 1 },
  { itemDefId: 10, type: 1, weight: 7, slots: 1, rarity: 2 },
  { itemDefId: 11, type: 1, weight: 22, slots: 1, rarity: 3 },
  { itemDefId: 12, type: 1, weight: 28, slots: 1, rarity: 4 },
  { itemDefId: 13, type: 1, weight: 19, slots: 1, rarity: 4 },
  { itemDefId: 14, type: 2, weight: 1, slots: 1, rarity: 0 },
  { itemDefId: 15, type: 2, weight: 1, slots: 1, rarity: 1 },
  { itemDefId: 16, type: 2, weight: 2, slots: 2, rarity: 2 },
  { itemDefId: 17, type: 2, weight: 2, slots: 3, rarity: 3 },
  { itemDefId: 18, type: 2, weight: 8, slots: 4, rarity: 4 },
  { itemDefId: 19, type: 3, weight: 1, slots: 1, rarity: 0 },
  { itemDefId: 20, type: 4, weight: 1, slots: 1, rarity: 0 },
  { itemDefId: 21, type: 4, weight: 4, slots: 1, rarity: 0 },
  { itemDefId: 22, type: 5, weight: 10, slots: 1, rarity: 0 },
  { itemDefId: 23, type: 5, weight: 1, slots: 1, rarity: 2 },
  { itemDefId: 24, type: 5, weight: 1, slots: 1, rarity: 4 },
];

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
  const normalizedSnapshot = normalizeRuntimeContracts(snapshot);

  if (PROFILE_MUTATION_ACTIONS.has(action)) {
    return {
      eventType: "ProfileMutated",
      event: {
        action,
      },
      projections: buildProfileMutationProjections(action, normalizedSnapshot),
      message: null,
    };
  }

  if (RAID_START_ACTIONS.has(action)) {
    return {
      eventType: "RaidStarted",
      event: {
        action,
      },
      projections: buildRaidStartProjections(action, normalizedSnapshot),
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
        projections: buildRaidFinishedProjections(normalizedSnapshot),
        message: buildRaidFinishedMessage(action),
      };
    }

    return {
      eventType: resolveRaidEventType(action),
      event: {
        action,
      },
      projections: buildInRaidProjections(normalizedSnapshot, getKnownLogCount(payload)),
      message: null,
    };
  }

  return {
    snapshot: normalizedSnapshot,
    message: null,
  };
}

function buildProfileMutationProjections(action, snapshot) {
  const projections = {};

  if (action === "sell-stash-item"
    || action === "move-stash-to-on-person"
    || action === "sell-on-person-item"
    || action === "buy-from-shop"
    || action === "sell-luck-run-item"
    || action === "reallocate-stats") {
    projections.economy = buildEconomyProjection(snapshot);
  }

  if (action === "sell-stash-item"
    || action === "move-stash-to-on-person"
    || action === "sell-on-person-item"
    || action === "stash-on-person-item"
    || action === "equip-on-person-item"
    || action === "unequip-on-person-item"
    || action === "buy-from-shop"
    || action === "move-luck-run-item-to-on-person"
    || action === "accept-stats"
    || action === "reallocate-stats") {
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

  if (action === "accept-stats" || action === "reallocate-stats") {
    projections.player = buildPlayerProjection(snapshot);
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

function buildPlayerProjection(snapshot) {
  return {
    acceptedStats: snapshot?.acceptedStats ?? null,
    draftStats: snapshot?.draftStats ?? null,
    playerConstitution: snapshot?.playerConstitution ?? 0,
    playerMaxHealth: snapshot?.playerMaxHealth ?? 0,
    availableStatPoints: snapshot?.availableStatPoints ?? 0,
    statsAccepted: snapshot?.statsAccepted ?? false,
  };
}

function buildRaidStartProjections(action, snapshot) {
  const projections = {
    player: buildPlayerProjection(snapshot),
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
  const encounterDescriptionKey = resolveEncounterDescriptionKey(
    activeRaid?.encounterDescription,
    activeRaid?.encounterType,
    activeRaid?.extractHoldActive,
  );
  const enemyKey = resolveEnemyKey(activeRaid?.enemyName);
  const projection = {
    health: activeRaid?.health ?? 0,
    backpackCapacity: activeRaid?.backpackCapacity ?? 0,
    encumbrance: activeRaid?.encumbrance ?? 0,
    maxEncumbrance: activeRaid?.maxEncumbrance ?? 0,
    ammo: activeRaid?.ammo ?? 0,
    weaponMalfunction: activeRaid?.weaponMalfunction ?? false,
    medkits: activeRaid?.medkits ?? 0,
    lootSlots: activeRaid?.lootSlots ?? 0,
    challenge: activeRaid?.challenge ?? 0,
    distanceFromExtract: activeRaid?.distanceFromExtract ?? 0,
    encounterType: activeRaid?.encounterType ?? "Neutral",
    encounterTitle: "",
    encounterDescription: "",
    encounterDescriptionKey,
    contactState: normalizeOpeningStateText(activeRaid?.contactState),
    surpriseSide: normalizeOpeningStateText(activeRaid?.surpriseSide),
    initiativeWinner: normalizeOpeningStateText(activeRaid?.initiativeWinner),
    openingActionsRemaining: normalizeOpeningActionsRemaining(activeRaid?.openingActionsRemaining),
    surprisePersistenceEligible: activeRaid?.surprisePersistenceEligible ?? false,
    extractHoldActive: activeRaid?.extractHoldActive ?? false,
    holdAtExtractUntil: activeRaid?.holdAtExtractUntil ?? null,
    enemyName: "",
    enemyKey,
    enemyHealth: activeRaid?.enemyHealth ?? 0,
    enemyDexterity: activeRaid?.enemyDexterity ?? 0,
    enemyConstitution: activeRaid?.enemyConstitution ?? 0,
    enemyStrength: activeRaid?.enemyStrength ?? 0,
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

function buildRaidFinishedMessage(action) {
  return action === "attempt-extract"
    ? "Extracted successfully."
    : "Killed in raid.";
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

function normalizeOpeningStateText(value) {
  return typeof value === "string" && value.trim().length > 0 ? value : "None";
}

function normalizeOpeningActionsRemaining(value) {
  return Number.isInteger(value) && value >= 0 ? value : 0;
}

function normalizeRuntimeContracts(value, parentKey = "") {
  if (Array.isArray(value)) {
    return value.map((entry) => normalizeRuntimeContracts(entry, parentKey));
  }

  if (value && typeof value === "object") {
    const normalized = Object.fromEntries(
      Object.entries(value).map(([key, entryValue]) => [key, normalizeRuntimeContracts(entryValue, key)]),
    );

    if (isRuleItemLike(normalized) && parentKey === "itemRules") {
      const itemDefId = resolveItemDefId(normalized);
      if (itemDefId > 0) {
        normalized.itemDefId = itemDefId;
      }

      delete normalized.itemKey;
      delete normalized.name;
    } else if (isShopOfferLike(normalized) && parentKey === "shopStock") {
      const itemDefId = resolveItemDefId(normalized);
      if (itemDefId > 0) {
        normalized.itemDefId = itemDefId;
      }

      keepOnly(normalized, ["itemDefId", "price", "stock"]);
    } else if (isRuntimeItemLike(normalized)) {
      const itemDefId = resolveItemDefId(normalized);
      if (itemDefId > 0) {
        normalized.itemDefId = itemDefId;
      }

      keepOnly(normalized, ["itemDefId"]);
    }

    return normalized;
  }

  return value;
}

function isRuntimeItemLike(value) {
  return Boolean(value)
    && typeof value === "object"
    && typeof value.type === "number"
    && typeof value.slots === "number"
    && (typeof value.value === "number" || typeof value.itemDefId === "number" || typeof value.name === "string");
}

function isRuleItemLike(value) {
  return Boolean(value)
    && typeof value === "object"
    && typeof value.type === "number"
    && typeof value.weight === "number"
    && typeof value.slots === "number"
    && (typeof value.itemDefId === "number" || typeof value.name === "string");
}

function isShopOfferLike(value) {
  return Boolean(value)
    && typeof value === "object"
    && (typeof value.price === "number" || typeof value.stock === "number" || typeof value.itemDefId === "number" || typeof value.name === "string");
}

function resolveItemDefId(value) {
  if (Number.isInteger(value?.itemDefId) && value.itemDefId > 0) {
    return value.itemDefId;
  }

  if (typeof value?.name === "string" && ITEM_DEF_ID_BY_LEGACY_NAME.has(value.name)) {
    return ITEM_DEF_ID_BY_LEGACY_NAME.get(value.name);
  }

  return 0;
}

function keepOnly(value, allowedKeys) {
  const allowed = new Set(allowedKeys);
  for (const key of Object.keys(value)) {
    if (!allowed.has(key)) {
      delete value[key];
    }
  }
}

function resolveEnemyKey(enemyName) {
  if (typeof enemyName !== "string" || enemyName.trim().length === 0) {
    return "";
  }

  if (/scav/i.test(enemyName)) {
    return "scavenger";
  }

  if (/extract hunter/i.test(enemyName)) {
    return "extract_hunter";
  }

  if (/guard/i.test(enemyName)) {
    return "guard";
  }

  return "";
}

function resolveEncounterDescriptionKey(description, encounterType, extractHoldActive) {
  if (extractHoldActive) {
    return "extract_hold";
  }

  if (typeof description !== "string" || description.trim().length === 0) {
    return encounterType === "Loot"
      ? "loot_container"
      : encounterType === "Extraction"
        ? "extract_ready"
        : encounterType === "Combat"
          ? "combat_contact"
          : "neutral_travel";
  }

  if (/hunter contact/i.test(description)) {
    return "combat_hunter_contact";
  }

  if (/ambushed while moving/i.test(description)) {
    return "combat_extract_ambush";
  }

  if (/spot each other/i.test(description)) {
    return "combat_mutual_contact";
  }

  if (/searchable container/i.test(description) || /server loot/i.test(description)) {
    return "loot_container";
  }

  if (/holding at extract/i.test(description)) {
    return "extract_hold";
  }

  if (/near the extraction route/i.test(description)) {
    return "extract_ready";
  }

  if (/move toward the extraction route/i.test(description)) {
    return "neutral_travel";
  }

  if (/enemy contact/i.test(description) || /server combat/i.test(description)) {
    return "combat_contact";
  }

  return "";
}
