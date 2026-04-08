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

function normalizeActiveRaidPresentation(activeRaid) {
  if (!activeRaid || typeof activeRaid !== "object") {
    return activeRaid;
  }

  return {
    ...activeRaid,
    encounterTitle: "",
    encounterDescription: "",
    encounterDescriptionKey: resolveEncounterDescriptionKey(
      activeRaid.encounterDescription,
      activeRaid.encounterType,
      activeRaid.extractHoldActive,
    ),
    enemyName: "",
    enemyKey: resolveEnemyKey(activeRaid.enemyName),
  };
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
      const snapshot = ensureItemRulesCatalog(await bootstrapProfile(accessToken));
      return json({
        isAuthenticated: true,
        userEmail: decodeJwtEmail(accessToken),
        snapshot: normalizeRuntimeContracts(toCamelCase(snapshot)),
      });
    } catch (error) {
      return serverError(error instanceof Error ? error.message : undefined);
    }
  };
}

function ensureItemRulesCatalog(snapshot) {
  if (!snapshot || typeof snapshot !== "object") {
    return snapshot;
  }

  const normalizedSnapshot = {
    ...snapshot,
    ActiveRaid: normalizeActiveRaidPresentation(snapshot.ActiveRaid),
  };

  if (Array.isArray(snapshot.ItemRules) && snapshot.ItemRules.length > 0) {
    return normalizedSnapshot;
  }

  return {
    ...normalizedSnapshot,
    ItemRules: DEFAULT_ITEM_RULES,
  };
}
