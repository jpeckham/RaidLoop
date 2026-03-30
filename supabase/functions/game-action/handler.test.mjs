import test from "node:test";
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";

import { createGameActionHandler } from "./handler.mjs";

test("surprise encounter migration writes authoritative opening phase fields", async () => {
  const migration = readFileSync(
    new URL("../../migrations/2026032501_add_authored_surprise_encounter_styles.sql", import.meta.url),
    "utf8",
  );

  assert.match(migration, /jsonb_set\(updated_payload, '\{contactState\}', to_jsonb\(coalesce\(selected_entry\.contact_state, 'MutualContact'::text\)\), true\)/);
  assert.match(migration, /selected_entry\.contact_state = 'PlayerAmbush'/);
  assert.match(migration, /selected_entry\.contact_state = 'EnemyAmbush'/);
  assert.match(migration, /random\(\) < 0\.5/);
  assert.match(migration, /openingActionsRemaining/);
  assert.match(migration, /surprisePersistenceEligible/);
});

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
  assert.equal(body.snapshot, undefined);
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

test("game-action returns profile-mutated player projections for accept-stats", async () => {
  const handler = createGameActionHandler({
    dispatchAction: async (accessToken, action, payload) => {
      assert.equal(accessToken, "token-123");
      assert.equal(action, "accept-stats");
      assert.equal(payload.draftStats.dexterity, 14);
      return {
        money: 500,
        mainStash: [],
        onPersonItems: [{ item: { name: "AK74", type: 0, value: 320, slots: 1, rarity: 2, displayRarity: 3 }, isEquipped: true }],
        acceptedStats: { strength: 8, dexterity: 14, constitution: 12, intelligence: 10, wisdom: 9, charisma: 16 },
        draftStats: { strength: 8, dexterity: 14, constitution: 12, intelligence: 10, wisdom: 9, charisma: 16 },
        playerConstitution: 12,
        playerMaxHealth: 34,
        availableStatPoints: 0,
        statsAccepted: true,
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
      action: "accept-stats",
      payload: { draftStats: { dexterity: 14 } },
    }),
  }));

  const body = await response.json();
  assert.equal(body.eventType, "ProfileMutated");
  assert.equal(body.projections.player.acceptedStats.dexterity, 14);
  assert.equal(body.projections.player.playerConstitution, 12);
  assert.equal(body.projections.player.playerMaxHealth, 34);
  assert.equal(body.projections.player.availableStatPoints, 0);
  assert.equal(body.projections.player.statsAccepted, true);
  assert.equal(body.snapshot, undefined);
});

test("game-action returns profile-mutated player projections for reallocate-stats", async () => {
  const handler = createGameActionHandler({
    dispatchAction: async (accessToken, action, payload) => {
      assert.equal(accessToken, "token-123");
      assert.equal(action, "reallocate-stats");
      assert.deepEqual(payload, {});
      return {
        money: 5000,
        mainStash: [],
        onPersonItems: [],
        acceptedStats: { strength: 8, dexterity: 8, constitution: 8, intelligence: 8, wisdom: 8, charisma: 8 },
        draftStats: { strength: 8, dexterity: 8, constitution: 8, intelligence: 8, wisdom: 8, charisma: 8 },
        availableStatPoints: 27,
        statsAccepted: false,
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
      action: "reallocate-stats",
      payload: {},
    }),
  }));

  const body = await response.json();
  assert.equal(body.eventType, "ProfileMutated");
  assert.equal(body.projections.economy.money, 5000);
  assert.equal(body.projections.player.availableStatPoints, 27);
  assert.equal(body.projections.player.statsAccepted, false);
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
        playerConstitution: 12,
        playerMaxHealth: 34,
        activeRaid: {
          health: 27,
          backpackCapacity: 3,
          encumbrance: 40,
          maxEncumbrance: 175,
          ammo: 9,
          weaponMalfunction: false,
          medkits: 1,
          lootSlots: 0,
          challenge: 0,
          distanceFromExtract: 3,
          encounterType: "Combat",
          encounterTitle: "Combat Encounter",
          encounterDescription: "Enemy contact on your position.",
          enemyName: "Scav",
          enemyHealth: 17,
          enemyDexterity: 11,
          enemyConstitution: 12,
          enemyStrength: 10,
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
  assert.equal(body.projections.player.playerConstitution, 12);
  assert.equal(body.projections.player.playerMaxHealth, 34);
  assert.equal(body.projections.raid.health, 27);
  assert.equal(body.projections.raid.encumbrance, 40);
  assert.equal(body.projections.raid.maxEncumbrance, 175);
  assert.equal(body.projections.raid.challenge, 0);
  assert.equal(body.projections.raid.distanceFromExtract, 3);
  assert.equal(body.projections.raid.ammo, 9);
  assert.equal(body.projections.raid.weaponMalfunction, false);
  assert.equal(body.projections.raid.contactState, "None");
  assert.equal(body.projections.raid.surpriseSide, "None");
  assert.equal(body.projections.raid.initiativeWinner, "None");
  assert.equal(body.projections.raid.openingActionsRemaining, 0);
  assert.equal(body.projections.raid.surprisePersistenceEligible, false);
  assert.equal(body.projections.raid.enemyDexterity, 11);
  assert.equal(body.projections.raid.enemyConstitution, 12);
  assert.equal(body.projections.raid.enemyStrength, 10);
  assert.equal("extractProgress" in body.projections.raid, false);
  assert.equal("extractRequired" in body.projections.raid, false);
  assert.equal(body.projections.raid.equippedItems[0].name, "AK74");
  assert.equal(body.snapshot, undefined);
});

test("game-action round-trips enemy constitution and strength in raid projections", async () => {
  const handler = createGameActionHandler({
    dispatchAction: async (accessToken, action, payload) => {
      assert.equal(accessToken, "token-123");
      assert.equal(action, "start-main-raid");
      assert.deepEqual(payload, {});
      return {
        money: 500,
        mainStash: [],
        onPersonItems: [],
        randomCharacterAvailableAt: "0001-01-01T00:00:00+00:00",
        randomCharacter: null,
        activeRaid: {
          health: 27,
          backpackCapacity: 3,
          ammo: 9,
          weaponMalfunction: false,
          medkits: 1,
          lootSlots: 0,
          challenge: 0,
          distanceFromExtract: 3,
          encounterType: "Combat",
          encounterTitle: "Combat Encounter",
          encounterDescription: "Enemy contact on your position.",
          enemyName: "Scav",
          enemyHealth: 17,
          enemyConstitution: 12,
          enemyStrength: 7,
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

  const body = await response.json();
  assert.equal(body.projections.raid.enemyConstitution, 12);
  assert.equal(body.projections.raid.enemyStrength, 7);
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
          challenge: 0,
          distanceFromExtract: 3,
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
  assert.equal(body.projections.player.playerConstitution, 0);
  assert.equal(body.projections.player.playerMaxHealth, 0);
  assert.equal(body.projections.luckRun.randomCharacter.name, "Ghost-101");
  assert.equal(body.projections.raid.encounterType, "Loot");
  assert.equal(body.projections.raid.challenge, 0);
  assert.equal(body.projections.raid.distanceFromExtract, 3);
  assert.equal(body.projections.raid.contactState, "None");
  assert.equal(body.projections.raid.surpriseSide, "None");
  assert.equal(body.projections.raid.initiativeWinner, "None");
  assert.equal(body.projections.raid.openingActionsRemaining, 0);
  assert.equal(body.projections.raid.surprisePersistenceEligible, false);
  assert.equal("extractProgress" in body.projections.raid, false);
  assert.equal("extractRequired" in body.projections.raid, false);
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
          challenge: 1,
          distanceFromExtract: 3,
          encounterType: "Combat",
          encounterTitle: "Combat Encounter",
          encounterDescription: "Enemy contact on your position.",
          contactState: "PlayerAmbush",
          surpriseSide: "Player",
          initiativeWinner: "None",
          openingActionsRemaining: 1,
          surprisePersistenceEligible: false,
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
  assert.equal(body.projections.raid.contactState, "PlayerAmbush");
  assert.equal(body.projections.raid.surpriseSide, "Player");
  assert.equal(body.projections.raid.initiativeWinner, "None");
  assert.equal(body.projections.raid.openingActionsRemaining, 1);
  assert.equal(body.projections.raid.surprisePersistenceEligible, false);
  assert.equal("extractProgress" in body.projections.raid, false);
  assert.equal("extractRequired" in body.projections.raid, false);
  assert.deepEqual(body.projections.raid.logEntriesAdded, [
    "You hit Scav for 4.",
    "Scav hits you for 3.",
  ]);
  assert.equal(body.projections.raid.logEntries, undefined);
});

test("game-action returns mutual-contact combat projections with initiative winner", async () => {
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
          health: 26,
          backpackCapacity: 3,
          ammo: 7,
          weaponMalfunction: false,
          medkits: 1,
          lootSlots: 0,
          challenge: 1,
          distanceFromExtract: 3,
          encounterType: "Combat",
          encounterTitle: "Combat Encounter",
          encounterDescription: "You and a patrol notice each other at nearly the same moment.",
          contactState: "MutualContact",
          surpriseSide: "None",
          initiativeWinner: "Enemy",
          openingActionsRemaining: 0,
          surprisePersistenceEligible: false,
          enemyName: "Patrol Guard",
          enemyHealth: 11,
          lootContainer: "",
          awaitingDecision: false,
          discoveredLoot: [],
          carriedLoot: [],
          equippedItems: [],
          logEntries: [
            "Raid started as Main Character.",
            "You hit Patrol Guard for 3.",
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
  assert.equal(body.projections.raid.contactState, "MutualContact");
  assert.equal(body.projections.raid.surpriseSide, "None");
  assert.equal(body.projections.raid.initiativeWinner, "Enemy");
  assert.equal(body.projections.raid.openingActionsRemaining, 0);
  assert.equal(body.projections.raid.surprisePersistenceEligible, false);
  assert.equal("extractProgress" in body.projections.raid, false);
  assert.equal("extractRequired" in body.projections.raid, false);
});

test("game-action treats full-auto as a combat action", async () => {
  const handler = createGameActionHandler({
    dispatchAction: async (accessToken, action, payload) => {
      assert.equal(accessToken, "token-123");
      assert.equal(action, "full-auto");
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
          ammo: 20,
          weaponMalfunction: false,
          medkits: 1,
          lootSlots: 0,
          challenge: 1,
          distanceFromExtract: 3,
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
            "Not enough ammo for Full Auto.",
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
      action: "full-auto",
      payload: { target: "enemy", knownLogCount: 1 },
    }),
  }));

  const body = await response.json();
  assert.equal(body.eventType, "CombatResolved");
  assert.deepEqual(body.event, { action: "full-auto" });
  assert.equal(body.projections.raid.ammo, 20);
  assert.equal("extractProgress" in body.projections.raid, false);
  assert.equal("extractRequired" in body.projections.raid, false);
  assert.deepEqual(body.projections.raid.logEntriesAdded, ["Not enough ammo for Full Auto."]);
  assert.equal(body.snapshot, undefined);
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
          challenge: 1,
          distanceFromExtract: 3,
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
  assert.equal("extractProgress" in body.projections.raid, false);
  assert.equal("extractRequired" in body.projections.raid, false);
  assert.deepEqual(body.projections.raid.logEntriesAdded, ["Looted Bandage."]);
});

test("game-action returns encounter-advanced projections for go-deeper", async () => {
  const handler = createGameActionHandler({
    dispatchAction: async (accessToken, action, payload) => {
      assert.equal(accessToken, "token-123");
      assert.equal(action, "go-deeper");
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
          challenge: 4,
          distanceFromExtract: 5,
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
      action: "go-deeper",
      payload: { knownLogCount: 1 },
    }),
  }));

  const body = await response.json();
  assert.equal(body.eventType, "EncounterAdvanced");
  assert.equal(body.projections.raid.encounterType, "Extraction");
  assert.equal(body.projections.raid.challenge, 4);
  assert.equal(body.projections.raid.distanceFromExtract, 5);
  assert.equal("extractProgress" in body.projections.raid, false);
  assert.equal("extractRequired" in body.projections.raid, false);
  assert.deepEqual(body.projections.raid.logEntriesAdded, ["Extraction point located."]);
});

test("game-action returns encounter-advanced projections for move-toward-extract", async () => {
  const handler = createGameActionHandler({
    dispatchAction: async (accessToken, action, payload) => {
      assert.equal(accessToken, "token-123");
      assert.equal(action, "move-toward-extract");
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
          challenge: 3,
          distanceFromExtract: 0,
          encounterType: "Neutral",
          encounterTitle: "Travel Encounter",
          encounterDescription: "You move toward the extraction route.",
          enemyName: "",
          enemyHealth: 0,
          lootContainer: "",
          awaitingDecision: false,
          discoveredLoot: [],
          carriedLoot: [],
          equippedItems: [],
          logEntries: [
            "Raid started as Main Character.",
            "Moved one step closer to extract.",
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
      action: "move-toward-extract",
      payload: { knownLogCount: 1 },
    }),
  }));

  const body = await response.json();
  assert.equal(body.eventType, "EncounterAdvanced");
  assert.equal(body.projections.raid.challenge, 3);
  assert.equal(body.projections.raid.distanceFromExtract, 0);
  assert.equal("extractProgress" in body.projections.raid, false);
  assert.equal("extractRequired" in body.projections.raid, false);
  assert.deepEqual(body.projections.raid.logEntriesAdded, ["Moved one step closer to extract."]);
});

test("game-action returns encounter-advanced projections for stay-at-extract", async () => {
  const handler = createGameActionHandler({
    dispatchAction: async (accessToken, action, payload) => {
      assert.equal(accessToken, "token-123");
      assert.equal(action, "stay-at-extract");
      assert.equal(payload.knownLogCount, 0);
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
          challenge: 5,
          distanceFromExtract: 1,
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
      action: "stay-at-extract",
      payload: { knownLogCount: 0 },
    }),
  }));

  const body = await response.json();
  assert.equal(body.eventType, "EncounterAdvanced");
  assert.equal(body.projections.raid.encounterType, "Extraction");
  assert.equal(body.projections.raid.challenge, 5);
  assert.equal(body.projections.raid.distanceFromExtract, 1);
  assert.equal("extractProgress" in body.projections.raid, false);
  assert.equal("extractRequired" in body.projections.raid, false);
});

test("game-action returns encounter-advanced projections for start-extract-hold", async () => {
  const handler = createGameActionHandler({
    dispatchAction: async (accessToken, action, payload) => {
      assert.equal(accessToken, "token-123");
      assert.equal(action, "start-extract-hold");
      assert.equal(payload.knownLogCount, 0);
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
          medkits: 1,
          lootSlots: 0,
          challenge: 2,
          distanceFromExtract: 0,
          extractHoldActive: true,
          holdAtExtractUntil: "2026-03-28T18:30:00Z",
          encounterType: "Extraction",
          encounterTitle: "Extraction Opportunity",
          encounterDescription: "Holding at extract.",
          enemyName: "",
          enemyHealth: 0,
          lootContainer: "",
          awaitingDecision: false,
          discoveredLoot: [],
          carriedLoot: [],
          equippedItems: [],
          logEntries: [
            "You begin holding at extract.",
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
      action: "start-extract-hold",
      payload: { knownLogCount: 0 },
    }),
  }));

  const body = await response.json();
  assert.equal(body.eventType, "EncounterAdvanced");
  assert.deepEqual(body.event, { action: "start-extract-hold" });
  assert.equal(body.projections.raid.extractHoldActive, true);
  assert.equal(body.projections.raid.holdAtExtractUntil, "2026-03-28T18:30:00Z");
  assert.equal(body.projections.raid.encounterType, "Extraction");
  assert.equal(body.projections.raid.challenge, 2);
  assert.equal(body.projections.raid.distanceFromExtract, 0);
  assert.equal("extractProgress" in body.projections.raid, false);
  assert.equal("extractRequired" in body.projections.raid, false);
  assert.deepEqual(body.projections.raid.logEntriesAdded, ["You begin holding at extract."]);
});

test("game-action returns encounter-advanced projections for resolve-extract-hold", async () => {
  const handler = createGameActionHandler({
    dispatchAction: async (accessToken, action, payload) => {
      assert.equal(accessToken, "token-123");
      assert.equal(action, "resolve-extract-hold");
      assert.equal(payload.knownLogCount, 0);
      assert.equal(payload.holdAtExtractUntil, "2026-03-28T18:30:00Z");
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
          medkits: 1,
          lootSlots: 0,
          challenge: 3,
          distanceFromExtract: 0,
          extractHoldActive: false,
          holdAtExtractUntil: null,
          encounterType: "Combat",
          encounterTitle: "Combat Encounter",
          encounterDescription: "Hunter contact.",
          enemyName: "Extract Hunter",
          enemyHealth: 14,
          lootContainer: "",
          awaitingDecision: false,
          discoveredLoot: [],
          carriedLoot: [],
          equippedItems: [],
          logEntries: [
            "You finish holding at extract.",
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
      action: "resolve-extract-hold",
      payload: { knownLogCount: 0, holdAtExtractUntil: "2026-03-28T18:30:00Z" },
    }),
  }));

  const body = await response.json();
  assert.equal(body.eventType, "EncounterAdvanced");
  assert.deepEqual(body.event, { action: "resolve-extract-hold" });
  assert.equal(body.projections.raid.extractHoldActive, false);
  assert.equal(body.projections.raid.holdAtExtractUntil, null);
  assert.equal(body.projections.raid.encounterType, "Combat");
});

test("game-action returns encounter-advanced projections for cancel-extract-hold", async () => {
  const handler = createGameActionHandler({
    dispatchAction: async (accessToken, action, payload) => {
      assert.equal(accessToken, "token-123");
      assert.equal(action, "cancel-extract-hold");
      assert.equal(payload.knownLogCount, 0);
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
          medkits: 1,
          lootSlots: 0,
          challenge: 2,
          distanceFromExtract: 0,
          extractHoldActive: false,
          holdAtExtractUntil: null,
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
            "You stop holding at extract.",
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
      action: "cancel-extract-hold",
      payload: { knownLogCount: 0 },
    }),
  }));

  const body = await response.json();
  assert.equal(body.eventType, "EncounterAdvanced");
  assert.deepEqual(body.event, { action: "cancel-extract-hold" });
  assert.equal(body.projections.raid.extractHoldActive, false);
  assert.equal(body.projections.raid.holdAtExtractUntil, null);
  assert.equal(body.projections.raid.encounterType, "Extraction");
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
  assert.equal(body.message, "Extracted successfully.");
  assert.equal(body.snapshot, undefined);
});

test("game-action returns concise death message when combat ends the raid", async () => {
  const handler = createGameActionHandler({
    dispatchAction: async (accessToken, action, payload) => {
      assert.equal(accessToken, "token-123");
      assert.equal(action, "attack");
      assert.equal(payload.knownLogCount, 4);
      return {
        money: 120,
        mainStash: [],
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
      action: "attack",
      payload: { knownLogCount: 4 },
    }),
  }));

  const body = await response.json();
  assert.equal(body.eventType, "RaidFinished");
  assert.equal(body.message, "Killed in raid.");
  assert.equal(body.projections.raid, null);
});
