import test from "node:test";
import assert from "node:assert/strict";

import { createGameActionHandler } from "./handler.mjs";

test("game-action responds to preflight requests", async () => {
  const handler = createGameActionHandler({
    dispatchAction: async () => {
      throw new Error("should not execute");
    },
  });

  const response = await handler(new Request("https://example.test/game-action", {
    method: "OPTIONS",
  }));

  assert.equal(response.status, 200);
  assert.equal(response.headers.get("Access-Control-Allow-Origin"), "*");
});

test("game-action rejects missing bearer token", async () => {
  const handler = createGameActionHandler({
    dispatchAction: async () => {
      throw new Error("should not execute");
    },
  });

  const response = await handler(new Request("https://example.test/game-action", {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify({ action: "sell-stash-item", payload: { stashIndex: 0 } }),
  }));

  assert.equal(response.status, 401);
  assert.deepEqual(await response.json(), { error: "Authentication required." });
});

test("game-action rejects malformed action envelope", async () => {
  const handler = createGameActionHandler({
    dispatchAction: async () => {
      throw new Error("should not execute");
    },
  });

  const response = await handler(new Request("https://example.test/game-action", {
    method: "POST",
    headers: {
      Authorization: "Bearer token-123",
      "Content-Type": "application/json",
    },
    body: JSON.stringify({ payload: {} }),
  }));

  assert.equal(response.status, 400);
  assert.deepEqual(await response.json(), { error: "Action and payload are required." });
});

test("game-action returns profile-mutated projections for sell-stash-item", async () => {
  const handler = createGameActionHandler({
    dispatchAction: async (accessToken, action, payload) => {
      assert.equal(accessToken, "token-123");
      assert.equal(action, "sell-stash-item");
      assert.equal(payload.stashIndex, 0);
      return {
        money: 999,
        mainStash: [{ name: "Rusty Knife", type: 0, value: 0, slots: 1, rarity: 0, displayRarity: 1 }],
        onPersonItems: [],
        randomCharacterAvailableAt: "0001-01-01T00:00:00+00:00",
        randomCharacter: null,
        activeRaid: null,
      };
    },
  });

  const response = await handler(new Request("https://example.test/game-action", {
    method: "POST",
    headers: {
      Authorization: "Bearer token-123",
      "Content-Type": "application/json",
    },
    body: JSON.stringify({
      action: "sell-stash-item",
      payload: { stashIndex: 0 },
    }),
  }));

  assert.equal(response.status, 200);
  const body = await response.json();
  assert.equal(body.eventType, "ProfileMutated");
  assert.equal(body.projections.economy.money, 999);
  assert.equal(body.projections.stash.mainStash[0].name, "Rusty Knife");
  assert.ok(body.snapshot);
});

test("game-action returns profile-mutated projections for buy-from-shop", async () => {
  const handler = createGameActionHandler({
    dispatchAction: async (accessToken, action, payload) => {
      assert.equal(accessToken, "token-123");
      assert.equal(action, "buy-from-shop");
      assert.equal(payload.itemName, "Medkit");
      return {
        money: 380,
        mainStash: [],
        onPersonItems: [{ item: { name: "Medkit", type: 3, value: 30, slots: 1, rarity: 0, displayRarity: 1 }, isEquipped: false }],
        randomCharacterAvailableAt: "0001-01-01T00:00:00+00:00",
        randomCharacter: null,
        activeRaid: null,
      };
    },
  });

  const response = await handler(new Request("https://example.test/game-action", {
    method: "POST",
    headers: {
      Authorization: "Bearer token-123",
      "Content-Type": "application/json",
    },
    body: JSON.stringify({
      action: "buy-from-shop",
      payload: { itemName: "Medkit" },
    }),
  }));

  const body = await response.json();
  assert.equal(body.eventType, "ProfileMutated");
  assert.equal(body.projections.economy.money, 380);
  assert.equal(body.projections.loadout.onPersonItems[0].item.name, "Medkit");
});

test("game-action returns profile-mutated projections for move-stash-to-on-person", async () => {
  const handler = createGameActionHandler({
    dispatchAction: async (accessToken, action, payload) => {
      assert.equal(accessToken, "token-123");
      assert.equal(action, "move-stash-to-on-person");
      assert.equal(payload.stashIndex, 0);
      return {
        money: 500,
        mainStash: [{ name: "Bandage", type: 4, value: 15, slots: 1, rarity: 0, displayRarity: 0 }],
        onPersonItems: [{ item: { name: "AK74", type: 0, value: 320, slots: 1, rarity: 2, displayRarity: 3 }, isEquipped: true }],
        randomCharacterAvailableAt: "0001-01-01T00:00:00+00:00",
        randomCharacter: null,
        activeRaid: null,
      };
    },
  });

  const response = await handler(new Request("https://example.test/game-action", {
    method: "POST",
    headers: {
      Authorization: "Bearer token-123",
      "Content-Type": "application/json",
    },
    body: JSON.stringify({
      action: "move-stash-to-on-person",
      payload: { stashIndex: 0 },
    }),
  }));

  const body = await response.json();
  assert.equal(body.eventType, "ProfileMutated");
  assert.equal(body.projections.stash.mainStash[0].name, "Bandage");
  assert.equal(body.projections.loadout.onPersonItems[0].item.name, "AK74");
});

test("game-action returns profile-mutated projections for sell-luck-run-item", async () => {
  const handler = createGameActionHandler({
    dispatchAction: async (accessToken, action, payload) => {
      assert.equal(accessToken, "token-123");
      assert.equal(action, "sell-luck-run-item");
      assert.equal(payload.luckIndex, 0);
      return {
        money: 520,
        mainStash: [],
        onPersonItems: [],
        randomCharacterAvailableAt: "2026-03-18T06:00:00+00:00",
        randomCharacter: null,
        activeRaid: null,
      };
    },
  });

  const response = await handler(new Request("https://example.test/game-action", {
    method: "POST",
    headers: {
      Authorization: "Bearer token-123",
      "Content-Type": "application/json",
    },
    body: JSON.stringify({
      action: "sell-luck-run-item",
      payload: { luckIndex: 0 },
    }),
  }));

  const body = await response.json();
  assert.equal(body.eventType, "ProfileMutated");
  assert.equal(body.projections.economy.money, 520);
  assert.equal(body.projections.luckRun.randomCharacterAvailableAt, "2026-03-18T06:00:00+00:00");
  assert.equal(body.projections.luckRun.randomCharacter, null);
});

test("game-action returns raid-started projections for start-main-raid", async () => {
  const handler = createGameActionHandler({
    dispatchAction: async (accessToken, action, payload) => {
      assert.equal(accessToken, "token-123");
      assert.equal(action, "start-main-raid");
      assert.deepEqual(payload, {});
      return {
        money: 500,
        mainStash: [],
        onPersonItems: [
          { item: { name: "AK74", type: 0, value: 320, slots: 1, rarity: 2, displayRarity: 3 }, isEquipped: true },
        ],
        randomCharacterAvailableAt: "0001-01-01T00:00:00+00:00",
        randomCharacter: null,
        activeRaid: {
          health: 27,
          backpackCapacity: 3,
          ammo: 9,
          weaponMalfunction: false,
          medkits: 1,
          lootSlots: 0,
          extractProgress: 1,
          extractRequired: 3,
          encounterType: "Combat",
          encounterTitle: "Combat Encounter",
          encounterDescription: "Enemy contact on your position.",
          enemyName: "Scav",
          enemyHealth: 17,
          lootContainer: "",
          awaitingDecision: false,
          discoveredLoot: [],
          carriedLoot: [],
          equippedItems: [
            { name: "AK74", type: 0, value: 320, slots: 1, rarity: 2, displayRarity: 3 },
          ],
          logEntries: ["Raid started as Main Character."],
        },
      };
    },
  });

  const response = await handler(new Request("https://example.test/game-action", {
    method: "POST",
    headers: {
      Authorization: "Bearer token-123",
      "Content-Type": "application/json",
    },
    body: JSON.stringify({
      action: "start-main-raid",
      payload: {},
    }),
  }));

  assert.equal(response.status, 200);
  const body = await response.json();
  assert.equal(body.eventType, "RaidStarted");
  assert.deepEqual(body.event, { action: "start-main-raid" });
  assert.equal(body.projections.raid.health, 27);
  assert.equal(body.projections.raid.ammo, 9);
  assert.equal(body.projections.raid.equippedItems[0].name, "AK74");
  assert.ok(body.snapshot);
});

test("game-action returns raid-started projections for start-random-raid", async () => {
  const handler = createGameActionHandler({
    dispatchAction: async (accessToken, action, payload) => {
      assert.equal(accessToken, "token-123");
      assert.equal(action, "start-random-raid");
      assert.deepEqual(payload, {});
      return {
        money: 500,
        mainStash: [],
        onPersonItems: [],
        randomCharacterAvailableAt: "0001-01-01T00:00:00+00:00",
        randomCharacter: {
          name: "Ghost-101",
          inventory: [
            { name: "Makarov", type: 0, value: 60, slots: 1, rarity: 0, displayRarity: 1 },
          ],
        },
        activeRaid: {
          health: 28,
          backpackCapacity: 3,
          ammo: 8,
          weaponMalfunction: false,
          medkits: 1,
          lootSlots: 0,
          extractProgress: 1,
          extractRequired: 3,
          encounterType: "Loot",
          encounterTitle: "Loot Encounter",
          encounterDescription: "A searchable container appears.",
          enemyName: "",
          enemyHealth: 0,
          lootContainer: "Dead Body",
          awaitingDecision: false,
          discoveredLoot: [],
          carriedLoot: [],
          equippedItems: [
            { name: "Makarov", type: 0, value: 60, slots: 1, rarity: 0, displayRarity: 1 },
          ],
          logEntries: ["Raid started as Ghost-101."],
        },
      };
    },
  });

  const response = await handler(new Request("https://example.test/game-action", {
    method: "POST",
    headers: {
      Authorization: "Bearer token-123",
      "Content-Type": "application/json",
    },
    body: JSON.stringify({
      action: "start-random-raid",
      payload: {},
    }),
  }));

  const body = await response.json();
  assert.equal(body.eventType, "RaidStarted");
  assert.deepEqual(body.event, { action: "start-random-raid" });
  assert.equal(body.projections.luckRun.randomCharacter.name, "Ghost-101");
  assert.equal(body.projections.raid.encounterType, "Loot");
  assert.equal(body.projections.raid.logEntries[0], "Raid started as Ghost-101.");
});

test("game-action returns combat-resolved projections with appended log entries", async () => {
  const handler = createGameActionHandler({
    dispatchAction: async (accessToken, action, payload) => {
      assert.equal(accessToken, "token-123");
      assert.equal(action, "attack");
      assert.equal(payload.target, "enemy");
      assert.equal(payload.knownLogCount, 1);
      return {
        money: 500,
        mainStash: [],
        onPersonItems: [],
        randomCharacterAvailableAt: "0001-01-01T00:00:00+00:00",
        randomCharacter: null,
        activeRaid: {
          health: 24,
          backpackCapacity: 3,
          ammo: 7,
          weaponMalfunction: false,
          medkits: 1,
          lootSlots: 0,
          extractProgress: 1,
          extractRequired: 3,
          encounterType: "Combat",
          encounterTitle: "Combat Encounter",
          encounterDescription: "Enemy contact on your position.",
          enemyName: "Scav",
          enemyHealth: 8,
          lootContainer: "",
          awaitingDecision: false,
          discoveredLoot: [],
          carriedLoot: [],
          equippedItems: [
            { name: "AK74", type: 0, value: 320, slots: 1, rarity: 2, displayRarity: 3 },
          ],
          logEntries: [
            "Raid started as Main Character.",
            "You hit Scav for 4.",
            "Scav hits you for 3.",
          ],
        },
      };
    },
  });

  const response = await handler(new Request("https://example.test/game-action", {
    method: "POST",
    headers: {
      Authorization: "Bearer token-123",
      "Content-Type": "application/json",
    },
    body: JSON.stringify({
      action: "attack",
      payload: { target: "enemy", knownLogCount: 1 },
    }),
  }));

  const body = await response.json();
  assert.equal(body.eventType, "CombatResolved");
  assert.deepEqual(body.event, { action: "attack" });
  assert.equal(body.projections.raid.health, 24);
  assert.equal(body.projections.raid.enemyHealth, 8);
  assert.equal(body.projections.raid.ammo, 7);
  assert.deepEqual(body.projections.raid.logEntriesAdded, [
    "You hit Scav for 4.",
    "Scav hits you for 3.",
  ]);
  assert.equal(body.projections.raid.logEntries, undefined);
});

test("game-action returns loot-resolved projections for take-loot", async () => {
  const handler = createGameActionHandler({
    dispatchAction: async (accessToken, action, payload) => {
      assert.equal(accessToken, "token-123");
      assert.equal(action, "take-loot");
      assert.equal(payload.itemName, "Bandage");
      assert.equal(payload.knownLogCount, 1);
      return {
        money: 500,
        mainStash: [],
        onPersonItems: [],
        randomCharacterAvailableAt: "0001-01-01T00:00:00+00:00",
        randomCharacter: null,
        activeRaid: {
          health: 24,
          backpackCapacity: 3,
          ammo: 8,
          weaponMalfunction: false,
          medkits: 1,
          lootSlots: 1,
          extractProgress: 1,
          extractRequired: 3,
          encounterType: "Loot",
          encounterTitle: "Loot Encounter",
          encounterDescription: "A searchable container appears.",
          enemyName: "",
          enemyHealth: 0,
          lootContainer: "Dead Body",
          awaitingDecision: false,
          discoveredLoot: [],
          carriedLoot: [
            { name: "Bandage", type: 4, value: 15, slots: 1, rarity: 0, displayRarity: 0 },
          ],
          equippedItems: [],
          logEntries: [
            "Raid started as Main Character.",
            "Looted Bandage.",
          ],
        },
      };
    },
  });

  const response = await handler(new Request("https://example.test/game-action", {
    method: "POST",
    headers: {
      Authorization: "Bearer token-123",
      "Content-Type": "application/json",
    },
    body: JSON.stringify({
      action: "take-loot",
      payload: { itemName: "Bandage", knownLogCount: 1 },
    }),
  }));

  const body = await response.json();
  assert.equal(body.eventType, "LootResolved");
  assert.equal(body.projections.raid.carriedLoot[0].name, "Bandage");
  assert.deepEqual(body.projections.raid.logEntriesAdded, ["Looted Bandage."]);
});

test("game-action returns encounter-advanced projections for continue-searching", async () => {
  const handler = createGameActionHandler({
    dispatchAction: async (accessToken, action, payload) => {
      assert.equal(accessToken, "token-123");
      assert.equal(action, "continue-searching");
      assert.equal(payload.knownLogCount, 1);
      return {
        money: 500,
        mainStash: [],
        onPersonItems: [],
        randomCharacterAvailableAt: "0001-01-01T00:00:00+00:00",
        randomCharacter: null,
        activeRaid: {
          health: 24,
          backpackCapacity: 3,
          ammo: 8,
          weaponMalfunction: false,
          medkits: 1,
          lootSlots: 0,
          extractProgress: 2,
          extractRequired: 3,
          encounterType: "Extraction",
          encounterTitle: "Extraction Opportunity",
          encounterDescription: "You are near the extraction route.",
          enemyName: "",
          enemyHealth: 0,
          lootContainer: "",
          awaitingDecision: false,
          discoveredLoot: [],
          carriedLoot: [],
          equippedItems: [],
          logEntries: [
            "Raid started as Main Character.",
            "Extraction point located.",
          ],
        },
      };
    },
  });

  const response = await handler(new Request("https://example.test/game-action", {
    method: "POST",
    headers: {
      Authorization: "Bearer token-123",
      "Content-Type": "application/json",
    },
    body: JSON.stringify({
      action: "continue-searching",
      payload: { knownLogCount: 1 },
    }),
  }));

  const body = await response.json();
  assert.equal(body.eventType, "EncounterAdvanced");
  assert.equal(body.projections.raid.encounterType, "Extraction");
  assert.equal(body.projections.raid.extractProgress, 2);
  assert.deepEqual(body.projections.raid.logEntriesAdded, ["Extraction point located."]);
});

test("game-action returns raid-finished projections for attempt-extract", async () => {
  const handler = createGameActionHandler({
    dispatchAction: async (accessToken, action, payload) => {
      assert.equal(accessToken, "token-123");
      assert.equal(action, "attempt-extract");
      assert.equal(payload.knownLogCount, 2);
      return {
        money: 500,
        mainStash: [],
        onPersonItems: [
          { item: { name: "AK74", type: 0, value: 320, slots: 1, rarity: 2, displayRarity: 3 }, isEquipped: true },
          { item: { name: "Bandage", type: 4, value: 15, slots: 1, rarity: 0, displayRarity: 0 }, isEquipped: false },
        ],
        randomCharacterAvailableAt: "0001-01-01T00:00:00+00:00",
        randomCharacter: null,
        activeRaid: null,
      };
    },
  });

  const response = await handler(new Request("https://example.test/game-action", {
    method: "POST",
    headers: {
      Authorization: "Bearer token-123",
      "Content-Type": "application/json",
    },
    body: JSON.stringify({
      action: "attempt-extract",
      payload: { knownLogCount: 2 },
    }),
  }));

  const body = await response.json();
  assert.equal(body.eventType, "RaidFinished");
  assert.deepEqual(body.event, { action: "attempt-extract" });
  assert.equal(body.projections.raid, null);
  assert.equal(body.projections.loadout.onPersonItems[0].item.name, "AK74");
  assert.equal(body.projections.loadout.onPersonItems[1].item.name, "Bandage");
  assert.ok(body.snapshot);
});
