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

const ITEM_KEY_BY_LEGACY_NAME = new Map([
  ["Rusty Knife", "rusty_knife"],
  ["Light Pistol", "light_pistol"],
  ["Makarov", "light_pistol"],
  ["Drum SMG", "drum_smg"],
  ["PPSH", "drum_smg"],
  ["Field Carbine", "field_carbine"],
  ["AK74", "field_carbine"],
  ["Battle Rifle", "battle_rifle"],
  ["AK47", "battle_rifle"],
  ["Marksman Rifle", "marksman_rifle"],
  ["SVDS", "marksman_rifle"],
  ["Support Machine Gun", "support_machine_gun"],
  ["PKP", "support_machine_gun"],
  ["Soft Armor Vest", "soft_armor_vest"],
  ["6B2 body armor", "soft_armor_vest"],
  ["Reinforced Vest", "reinforced_vest"],
  ["BNTI Kirasa-N", "reinforced_vest"],
  ["Light Plate Carrier", "light_plate_carrier"],
  ["6B13 assault armor", "light_plate_carrier"],
  ["Medium Plate Carrier", "medium_plate_carrier"],
  ["FORT Defender-2", "medium_plate_carrier"],
  ["Heavy Plate Carrier", "heavy_plate_carrier"],
  ["6B43 Zabralo-Sh body armor", "heavy_plate_carrier"],
  ["Assault Plate Carrier", "assault_plate_carrier"],
  ["NFM THOR", "assault_plate_carrier"],
  ["Small Backpack", "small_backpack"],
  ["Large Backpack", "large_backpack"],
  ["Tactical Backpack", "tactical_backpack"],
  ["Hiking Backpack", "hiking_backpack"],
  ["Tasmanian Tiger Trooper 35", "hiking_backpack"],
  ["Raid Backpack", "raid_backpack"],
  ["6Sh118", "raid_backpack"],
  ["Medkit", "medkit"],
  ["Bandage", "bandage"],
  ["Ammo Box", "ammo_box"],
  ["Scrap Metal", "scrap_metal"],
  ["Rare Scope", "rare_scope"],
  ["Legendary Trigger Group", "legendary_trigger_group"],
]);

function normalizeItemKeys(value) {
  if (Array.isArray(value)) {
    return value.map(normalizeItemKeys);
  }

  if (value && typeof value === "object") {
    const normalized = Object.fromEntries(
      Object.entries(value).map(([key, entryValue]) => [key, normalizeItemKeys(entryValue)]),
    );

    if (isItemLike(normalized) && typeof normalized.itemKey !== "string") {
      const itemKey = ITEM_KEY_BY_LEGACY_NAME.get(normalized.name);
      if (itemKey) {
        normalized.itemKey = itemKey;
      }
    }

    return normalized;
  }

  return value;
}

function isItemLike(value) {
  return Boolean(value)
    && typeof value === "object"
    && typeof value.name === "string"
    && typeof value.type === "number"
    && typeof value.value === "number"
    && typeof value.slots === "number";
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
        snapshot: normalizeItemKeys(toCamelCase(snapshot)),
      });
    } catch (error) {
      return serverError(error instanceof Error ? error.message : undefined);
    }
  };
}
