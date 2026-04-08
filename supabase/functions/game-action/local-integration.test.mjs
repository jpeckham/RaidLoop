import test from "node:test";
import assert from "node:assert/strict";

import { createProfileRpcRepository } from "../_shared/profile-rpc.mjs";

const DEFAULT_SUPABASE_URL = "http://127.0.0.1:54321";
const DEFAULT_PUBLISHABLE_KEY = "sb_publishable_ACJWlzQHlZjBrEguHvfOxg_3BJgxAaH";

test("local game_action routes full-auto through the authoritative raid action path", async () => {
  const supabaseUrl = Deno.env.get("SUPABASE_URL") ?? DEFAULT_SUPABASE_URL;
  const publishableKey = Deno.env.get("SUPABASE_PUBLISHABLE_KEY") ?? DEFAULT_PUBLISHABLE_KEY;
  const repository = createProfileRpcRepository({ supabaseUrl, publishableKey, fetchImpl: fetch });
  const { accessToken, userId } = await signUpLocalUser(supabaseUrl, publishableKey);

  await seedMainCharacterProfile(
    repository,
    supabaseUrl,
    publishableKey,
    accessToken,
    userId,
    createItem("Makarov", 0, 12, 1, 0, 1, 2),
  );
  const startedRaid = await repository.dispatchAction(accessToken, "start-main-raid", {});
  assert.ok(startedRaid.activeRaid);

  const raidRow = await getSingleRow(supabaseUrl, publishableKey, accessToken, "raid_sessions", userId);
  const combatRaidPayload = structuredClone(raidRow.payload);
  combatRaidPayload.encounterType = "Combat";
  combatRaidPayload.encounterTitle = "Roadside Contact";
  combatRaidPayload.encounterDescription = "A scav steps into your lane.";
  combatRaidPayload.enemyName = "Scav";
  combatRaidPayload.enemyHealth = 10;
  combatRaidPayload.enemyDexterity = 8;
  combatRaidPayload.enemyConstitution = 8;
  combatRaidPayload.enemyStrength = 8;
  combatRaidPayload.enemyLoadout = [createItem("Rusty Knife", 0, 5, 1, 0, 0, 1)];
  combatRaidPayload.contactState = "MutualContact";
  combatRaidPayload.surpriseSide = "None";
  combatRaidPayload.initiativeWinner = "Player";
  combatRaidPayload.openingActionsRemaining = 0;
  combatRaidPayload.surprisePersistenceEligible = false;
  combatRaidPayload.ammo = 8;
  combatRaidPayload.logEntries = ["Raid started on server."];

  await updateRow(supabaseUrl, publishableKey, accessToken, "raid_sessions", userId, { payload: combatRaidPayload });

  const snapshot = await repository.dispatchAction(accessToken, "full-auto", {
    target: "enemy",
    knownLogCount: 1,
  });

  assert.equal(snapshot.activeRaid.ammo, 8);
  assert.equal(snapshot.activeRaid.logEntries[0], "Raid started on server.");
  assert.equal(snapshot.activeRaid.logEntries[1], "Weapon does not support full auto.");
  assert.match(snapshot.activeRaid.logEntries[2], /^Scav (hits|misses) you/);
  assert.equal(snapshot.activeRaid.logEntries.length, 3);
});

test("local profile_bootstrap re-derives stale raid max encumbrance and max health from accepted stats", async () => {
  const supabaseUrl = Deno.env.get("SUPABASE_URL") ?? DEFAULT_SUPABASE_URL;
  const publishableKey = Deno.env.get("SUPABASE_PUBLISHABLE_KEY") ?? DEFAULT_PUBLISHABLE_KEY;
  const repository = createProfileRpcRepository({ supabaseUrl, publishableKey, fetchImpl: fetch });
  const { accessToken, userId } = await signUpLocalUser(supabaseUrl, publishableKey);

  await seedMainCharacterProfile(repository, supabaseUrl, publishableKey, accessToken, userId);
  const startedRaid = await repository.dispatchAction(accessToken, "start-main-raid", {});
  assert.ok(startedRaid.activeRaid);

  const saveRow = await getSingleRow(supabaseUrl, publishableKey, accessToken, "game_saves", userId);
  const staleSavePayload = structuredClone(saveRow.payload);
  staleSavePayload.playerConstitution = 8;
  staleSavePayload.playerMaxHealth = 26;
  staleSavePayload.activeRaid.acceptedStats = {
    strength: 8,
    dexterity: 8,
    constitution: 8,
    intelligence: 8,
    wisdom: 8,
    charisma: 8,
  };
  staleSavePayload.activeRaid.maxEncumbrance = 80;
  staleSavePayload.activeRaid.encumbranceTier = "Heavy";

  await updateRow(supabaseUrl, publishableKey, accessToken, "game_saves", userId, { payload: staleSavePayload });

  const bootstrap = await repository.bootstrapProfile(accessToken);

  assert.equal(bootstrap.playerMaxHealth, 34);
  assert.equal(bootstrap.playerConstitution, 12);
  assert.equal(bootstrap.activeRaid.maxEncumbrance, 175);
});

test("local profile_bootstrap exposes itemDefId identities for legacy saved items", async () => {
  const supabaseUrl = Deno.env.get("SUPABASE_URL") ?? DEFAULT_SUPABASE_URL;
  const publishableKey = Deno.env.get("SUPABASE_PUBLISHABLE_KEY") ?? DEFAULT_PUBLISHABLE_KEY;
  const repository = createProfileRpcRepository({ supabaseUrl, publishableKey, fetchImpl: fetch });
  const { accessToken, userId } = await signUpLocalUser(supabaseUrl, publishableKey);

  await seedMainCharacterProfile(repository, supabaseUrl, publishableKey, accessToken, userId);

  const saveRow = await getSingleRow(supabaseUrl, publishableKey, accessToken, "game_saves", userId);
  const legacyPayload = structuredClone(saveRow.payload);
  legacyPayload.mainStash = [
    createItem("Makarov", 0, 12, 1, 0, 1, 2),
    createItem("6B2 body armor", 1, 14, 1, 0, 1, 9),
  ];
  legacyPayload.onPersonItems = [
    { item: createItem("AK74", 0, 34, 1, 2, 3, 7), isEquipped: true },
  ];

  await updateRow(supabaseUrl, publishableKey, accessToken, "game_saves", userId, { payload: legacyPayload });

  const bootstrap = await repository.bootstrapProfile(accessToken);

  assert.deepEqual(
    bootstrap.mainStash.map((item) => item.itemDefId).sort((left, right) => left - right),
    [2, 8],
  );
  assert.equal(bootstrap.onPersonItems[0].item.itemDefId, 4);
});

test("local profile_bootstrap prefers authoritative raid session log entries over stale save payload nulls", async () => {
  const supabaseUrl = Deno.env.get("SUPABASE_URL") ?? DEFAULT_SUPABASE_URL;
  const publishableKey = Deno.env.get("SUPABASE_PUBLISHABLE_KEY") ?? DEFAULT_PUBLISHABLE_KEY;
  const repository = createProfileRpcRepository({ supabaseUrl, publishableKey, fetchImpl: fetch });
  const { accessToken, userId } = await signUpLocalUser(supabaseUrl, publishableKey);

  await seedMainCharacterProfile(repository, supabaseUrl, publishableKey, accessToken, userId);
  const startedRaid = await repository.dispatchAction(accessToken, "start-main-raid", {});
  assert.ok(startedRaid.activeRaid);

  const saveRow = await getSingleRow(supabaseUrl, publishableKey, accessToken, "game_saves", userId);
  const staleSavePayload = structuredClone(saveRow.payload);
  staleSavePayload.activeRaid.logEntries = null;

  await updateRow(supabaseUrl, publishableKey, accessToken, "game_saves", userId, { payload: staleSavePayload });

  const bootstrap = await repository.bootstrapProfile(accessToken);

  assert.ok(bootstrap.activeRaid);
  assert.deepEqual(bootstrap.activeRaid.logEntries, startedRaid.activeRaid.logEntries);
});

test("local start-main-raid uses accepted stats for raid max health and encumbrance", async () => {
  const supabaseUrl = Deno.env.get("SUPABASE_URL") ?? DEFAULT_SUPABASE_URL;
  const publishableKey = Deno.env.get("SUPABASE_PUBLISHABLE_KEY") ?? DEFAULT_PUBLISHABLE_KEY;
  const repository = createProfileRpcRepository({ supabaseUrl, publishableKey, fetchImpl: fetch });
  const { accessToken, userId } = await signUpLocalUser(supabaseUrl, publishableKey);

  await seedMainCharacterProfile(repository, supabaseUrl, publishableKey, accessToken, userId);

  const startedRaid = await repository.dispatchAction(accessToken, "start-main-raid", {});

  assert.equal(startedRaid.playerMaxHealth, 34);
  assert.ok(startedRaid.activeRaid);
  assert.equal(startedRaid.activeRaid.health, 34);
  assert.equal(startedRaid.activeRaid.maxEncumbrance, 175);
  assert.deepEqual(startedRaid.activeRaid.acceptedStats, {
    strength: 14,
    dexterity: 8,
    constitution: 12,
    intelligence: 10,
    wisdom: 9,
    charisma: 16,
  });
});

test("authored enemy and loot generation still works after itemDefId FK migration", async () => {
  const supabaseUrl = Deno.env.get("SUPABASE_URL") ?? DEFAULT_SUPABASE_URL;
  const publishableKey = Deno.env.get("SUPABASE_PUBLISHABLE_KEY") ?? DEFAULT_PUBLISHABLE_KEY;
  const repository = createProfileRpcRepository({ supabaseUrl, publishableKey, fetchImpl: fetch });

  let sawCombatLoadout = false;
  let sawLootItems = false;

  for (let attempt = 0; attempt < 20 && (!sawCombatLoadout || !sawLootItems); attempt += 1) {
    const { accessToken, userId } = await signUpLocalUser(supabaseUrl, publishableKey);
    await seedMainCharacterProfile(repository, supabaseUrl, publishableKey, accessToken, userId);

    let snapshot = await repository.dispatchAction(accessToken, "start-main-raid", {});
    assert.ok(snapshot.activeRaid);

    for (let step = 0; step < 6 && snapshot.activeRaid && (!sawCombatLoadout || !sawLootItems); step += 1) {
      if (snapshot.activeRaid.encounterType === "Combat" && Array.isArray(snapshot.activeRaid.enemyLoadout) && snapshot.activeRaid.enemyLoadout.length > 0) {
        sawCombatLoadout = true;
        assert.ok(snapshot.activeRaid.enemyLoadout.every((item) => Number.isInteger(item.itemDefId) && item.itemDefId > 0));
      }

      if (snapshot.activeRaid.encounterType === "Loot" && Array.isArray(snapshot.activeRaid.discoveredLoot) && snapshot.activeRaid.discoveredLoot.length > 0) {
        sawLootItems = true;
        assert.ok(snapshot.activeRaid.discoveredLoot.every((item) => Number.isInteger(item.itemDefId) && item.itemDefId > 0));
      }

      if (!sawCombatLoadout || !sawLootItems) {
        snapshot = await repository.dispatchAction(accessToken, "go-deeper", {
          knownLogCount: Array.isArray(snapshot.activeRaid.logEntries) ? snapshot.activeRaid.logEntries.length : 0,
        });
      }
    }
  }

  assert.equal(sawCombatLoadout, true);
  assert.equal(sawLootItems, true);
});

test("local game_action accepts itemDefId for buy-from-shop", async () => {
  const supabaseUrl = Deno.env.get("SUPABASE_URL") ?? DEFAULT_SUPABASE_URL;
  const publishableKey = Deno.env.get("SUPABASE_PUBLISHABLE_KEY") ?? DEFAULT_PUBLISHABLE_KEY;
  const repository = createProfileRpcRepository({ supabaseUrl, publishableKey, fetchImpl: fetch });
  const { accessToken, userId } = await signUpLocalUser(supabaseUrl, publishableKey);

  await seedMainCharacterProfile(repository, supabaseUrl, publishableKey, accessToken, userId);

  const updated = await repository.dispatchAction(accessToken, "buy-from-shop", {
    itemDefId: 19,
  });

  assert.ok(Array.isArray(updated.onPersonItems));
  assert.equal(updated.onPersonItems.at(-1).item.itemDefId, 19);
});

test("persisted save payload strips itemKey and item name for authored items after profile writes", async () => {
  const supabaseUrl = Deno.env.get("SUPABASE_URL") ?? DEFAULT_SUPABASE_URL;
  const publishableKey = Deno.env.get("SUPABASE_PUBLISHABLE_KEY") ?? DEFAULT_PUBLISHABLE_KEY;
  const repository = createProfileRpcRepository({ supabaseUrl, publishableKey, fetchImpl: fetch });
  const { accessToken, userId } = await signUpLocalUser(supabaseUrl, publishableKey);

  await seedMainCharacterProfile(repository, supabaseUrl, publishableKey, accessToken, userId);

  const saveRow = await getSingleRow(supabaseUrl, publishableKey, accessToken, "game_saves", userId);
  const payload = structuredClone(saveRow.payload);
  payload.mainStash = [
    {
      itemDefId: 2,
      itemKey: "makarov",
      name: "Makarov",
      type: 0,
      value: 60,
      slots: 1,
      rarity: 0,
      displayRarity: 1,
      weight: 2,
    },
  ];
  payload.draftStats = payload.acceptedStats;

  await updateRow(supabaseUrl, publishableKey, accessToken, "game_saves", userId, { payload });
  await repository.dispatchAction(accessToken, "accept-stats", {
    draftStats: payload.acceptedStats,
  });

  const updatedRow = await getSingleRow(supabaseUrl, publishableKey, accessToken, "game_saves", userId);
  const persistedItem = updatedRow.payload.mainStash[0];

  assert.equal(persistedItem.itemDefId, 2);
  assert.equal("itemKey" in persistedItem, false);
  assert.equal("name" in persistedItem, false);
});

test("persisted raid payload strips itemKey and item name for authored items after raid writes", async () => {
  const supabaseUrl = Deno.env.get("SUPABASE_URL") ?? DEFAULT_SUPABASE_URL;
  const publishableKey = Deno.env.get("SUPABASE_PUBLISHABLE_KEY") ?? DEFAULT_PUBLISHABLE_KEY;
  const repository = createProfileRpcRepository({ supabaseUrl, publishableKey, fetchImpl: fetch });
  const { accessToken, userId } = await signUpLocalUser(supabaseUrl, publishableKey);

  await seedMainCharacterProfile(
    repository,
    supabaseUrl,
    publishableKey,
    accessToken,
    userId,
    {
      itemDefId: 2,
      itemKey: "makarov",
      name: "Makarov",
      type: 0,
      value: 60,
      slots: 1,
      rarity: 0,
      displayRarity: 1,
      weight: 2,
    },
  );

  const startedRaid = await repository.dispatchAction(accessToken, "start-main-raid", {});
  assert.ok(startedRaid.activeRaid);

  const raidRow = await getSingleRow(supabaseUrl, publishableKey, accessToken, "raid_sessions", userId);
  const equippedWeapon = raidRow.payload.equippedItems.find((item) => item.itemDefId === 2);

  assert.ok(equippedWeapon);
  assert.equal("itemKey" in equippedWeapon, false);
  assert.equal("name" in equippedWeapon, false);
});

test("local game_action accepts itemDefId for take-loot", async () => {
  const supabaseUrl = Deno.env.get("SUPABASE_URL") ?? DEFAULT_SUPABASE_URL;
  const publishableKey = Deno.env.get("SUPABASE_PUBLISHABLE_KEY") ?? DEFAULT_PUBLISHABLE_KEY;
  const repository = createProfileRpcRepository({ supabaseUrl, publishableKey, fetchImpl: fetch });
  const { accessToken, userId } = await signUpLocalUser(supabaseUrl, publishableKey);

  await seedMainCharacterProfile(repository, supabaseUrl, publishableKey, accessToken, userId);
  const startedRaid = await repository.dispatchAction(accessToken, "start-main-raid", {});
  assert.ok(startedRaid.activeRaid);

  const raidRow = await getSingleRow(supabaseUrl, publishableKey, accessToken, "raid_sessions", userId);
  const lootRaidPayload = structuredClone(raidRow.payload);
  lootRaidPayload.encounterType = "Loot";
  lootRaidPayload.encounterTitle = "Loot Encounter";
  lootRaidPayload.encounterDescription = "A searchable container appears.";
  lootRaidPayload.lootContainer = "Dead Body";
  lootRaidPayload.awaitingDecision = false;
  lootRaidPayload.discoveredLoot = [createItem("Bandage", 4, 15, 1, 0, 0, 1)];
  lootRaidPayload.carriedLoot = [];
  lootRaidPayload.logEntries = ["Raid started on server."];

  await updateRow(supabaseUrl, publishableKey, accessToken, "raid_sessions", userId, { payload: lootRaidPayload });

  const updated = await repository.dispatchAction(accessToken, "take-loot", {
    itemDefId: 20,
    knownLogCount: 1,
  });

  assert.equal(updated.activeRaid.discoveredLoot.length, 0);
  assert.equal(updated.activeRaid.carriedLoot[0].itemDefId, 20);
});

test("raid item actions work when persisted raid items only store itemDefId", async () => {
  const supabaseUrl = Deno.env.get("SUPABASE_URL") ?? DEFAULT_SUPABASE_URL;
  const publishableKey = Deno.env.get("SUPABASE_PUBLISHABLE_KEY") ?? DEFAULT_PUBLISHABLE_KEY;
  const repository = createProfileRpcRepository({ supabaseUrl, publishableKey, fetchImpl: fetch });
  const { accessToken, userId } = await signUpLocalUser(supabaseUrl, publishableKey);

  await seedMainCharacterProfile(repository, supabaseUrl, publishableKey, accessToken, userId, {
    itemDefId: 2,
    type: 0,
    value: 60,
    slots: 1,
    rarity: 0,
    displayRarity: 1,
    weight: 2,
  });

  const startedRaid = await repository.dispatchAction(accessToken, "start-main-raid", {});
  assert.ok(startedRaid.activeRaid);

  const raidRow = await getSingleRow(supabaseUrl, publishableKey, accessToken, "raid_sessions", userId);
  const lootRaidPayload = structuredClone(raidRow.payload);
  lootRaidPayload.encounterType = "Loot";
  lootRaidPayload.encounterTitle = "Loot Encounter";
  lootRaidPayload.encounterDescription = "A searchable container appears.";
  lootRaidPayload.lootContainer = "Dead Body";
  lootRaidPayload.awaitingDecision = false;
  lootRaidPayload.discoveredLoot = [{ itemDefId: 20, type: 4, value: 15, slots: 1, rarity: 0, displayRarity: 0, weight: 1 }];
  lootRaidPayload.carriedLoot = [];
  lootRaidPayload.equippedItems = lootRaidPayload.equippedItems.map((item) => ({ itemDefId: item.itemDefId }));
  lootRaidPayload.logEntries = ["Raid started on server."];

  await updateRow(supabaseUrl, publishableKey, accessToken, "raid_sessions", userId, { payload: lootRaidPayload });

  const updated = await repository.dispatchAction(accessToken, "take-loot", {
    itemDefId: 20,
    knownLogCount: 1,
  });

  assert.equal(updated.activeRaid.discoveredLoot.length, 0);
  assert.equal(updated.activeRaid.carriedLoot[0].itemDefId, 20);

  const updatedRaidRow = await getSingleRow(supabaseUrl, publishableKey, accessToken, "raid_sessions", userId);
  assert.equal(updatedRaidRow.payload.carriedLoot[0].itemDefId, 20);
  assert.equal("itemKey" in updatedRaidRow.payload.carriedLoot[0], false);
  assert.equal("name" in updatedRaidRow.payload.carriedLoot[0], false);
});

test("stale carried medkits do not block looting slot-based items", async () => {
  const supabaseUrl = Deno.env.get("SUPABASE_URL") ?? DEFAULT_SUPABASE_URL;
  const publishableKey = Deno.env.get("SUPABASE_PUBLISHABLE_KEY") ?? DEFAULT_PUBLISHABLE_KEY;
  const repository = createProfileRpcRepository({ supabaseUrl, publishableKey, fetchImpl: fetch });
  const { accessToken, userId } = await signUpLocalUser(supabaseUrl, publishableKey);

  await seedMainCharacterProfile(repository, supabaseUrl, publishableKey, accessToken, userId);
  const startedRaid = await repository.dispatchAction(accessToken, "start-main-raid", {});
  assert.ok(startedRaid.activeRaid);

  const raidRow = await getSingleRow(supabaseUrl, publishableKey, accessToken, "raid_sessions", userId);
  const staleRaidPayload = structuredClone(raidRow.payload);
  staleRaidPayload.encounterType = "Loot";
  staleRaidPayload.encounterTitle = "Loot Encounter";
  staleRaidPayload.encounterDescription = "A searchable container appears.";
  staleRaidPayload.lootContainer = "Dead Body";
  staleRaidPayload.awaitingDecision = false;
  staleRaidPayload.equippedItems = [
    { itemDefId: 4, type: 0, value: 320, slots: 1, rarity: 2, displayRarity: 2, weight: 7 },
    { itemDefId: 16, type: 2, value: 75, slots: 2, rarity: 2, displayRarity: 2, weight: 2 },
  ];
  staleRaidPayload.backpackCapacity = 6;
  staleRaidPayload.discoveredLoot = [{ itemDefId: 4, type: 0, value: 320, slots: 1, rarity: 2, displayRarity: 2, weight: 7 }];
  staleRaidPayload.carriedLoot = Array.from({ length: 6 }, () => ({ itemDefId: 19, type: 3, value: 30, slots: 1, rarity: 0, displayRarity: 0, weight: 1 }));
  staleRaidPayload.medkits = 0;
  staleRaidPayload.logEntries = ["Raid started on server."];

  await updateRow(supabaseUrl, publishableKey, accessToken, "raid_sessions", userId, { payload: staleRaidPayload });

  const updated = await repository.dispatchAction(accessToken, "take-loot", {
    itemDefId: 4,
    knownLogCount: 1,
  });

  assert.equal(updated.activeRaid.discoveredLoot.length, 0);
  assert.ok(updated.activeRaid.carriedLoot.some((item) => item.itemDefId === 4));
  assert.equal(updated.activeRaid.medkits, 6);

  const updatedRaidRow = await getSingleRow(supabaseUrl, publishableKey, accessToken, "raid_sessions", userId);
  assert.ok(updatedRaidRow.payload.carriedLoot.some((item) => item.itemDefId === 4));
  assert.equal(updatedRaidRow.payload.carriedLoot.some((item) => item.itemDefId === 19), false);
  assert.equal(updatedRaidRow.payload.medkits, 6);
});

test("selling the last luck-run item returns an offset-qualified cooldown timestamp", async () => {
  const supabaseUrl = Deno.env.get("SUPABASE_URL") ?? DEFAULT_SUPABASE_URL;
  const publishableKey = Deno.env.get("SUPABASE_PUBLISHABLE_KEY") ?? DEFAULT_PUBLISHABLE_KEY;
  const repository = createProfileRpcRepository({ supabaseUrl, publishableKey, fetchImpl: fetch });
  const { accessToken, userId } = await signUpLocalUser(supabaseUrl, publishableKey);

  await seedMainCharacterProfile(repository, supabaseUrl, publishableKey, accessToken, userId);
  const saveRow = await getSingleRow(supabaseUrl, publishableKey, accessToken, "game_saves", userId);
  const savePayload = structuredClone(saveRow.payload);
  savePayload.randomCharacter = {
    name: "Ghost-101",
    inventory: [{ itemDefId: 19, type: 3, value: 30, slots: 1, rarity: 0, displayRarity: 0, weight: 1 }],
  };
  savePayload.randomCharacterAvailableAt = "0001-01-01T00:00:00+00:00";

  await updateRow(supabaseUrl, publishableKey, accessToken, "game_saves", userId, { payload: savePayload });

  const updated = await repository.dispatchAction(accessToken, "sell-luck-run-item", { luckIndex: 0 });

  assert.equal(updated.randomCharacter, null);
  assert.match(updated.randomCharacterAvailableAt, /\+00:00$/);

  const updatedSaveRow = await getSingleRow(supabaseUrl, publishableKey, accessToken, "game_saves", userId);
  assert.equal(updatedSaveRow.payload.randomCharacter, null);
  assert.match(updatedSaveRow.payload.randomCharacterAvailableAt, /\+00:00$/);
});

test("local game_action re-derives stale raid session encumbrance from accepted stats", async () => {
  const supabaseUrl = Deno.env.get("SUPABASE_URL") ?? DEFAULT_SUPABASE_URL;
  const publishableKey = Deno.env.get("SUPABASE_PUBLISHABLE_KEY") ?? DEFAULT_PUBLISHABLE_KEY;
  const repository = createProfileRpcRepository({ supabaseUrl, publishableKey, fetchImpl: fetch });
  const { accessToken, userId } = await signUpLocalUser(supabaseUrl, publishableKey);

  await seedMainCharacterProfile(repository, supabaseUrl, publishableKey, accessToken, userId);
  const startedRaid = await repository.dispatchAction(accessToken, "start-main-raid", {});
  assert.ok(startedRaid.activeRaid);

  const raidRow = await getSingleRow(supabaseUrl, publishableKey, accessToken, "raid_sessions", userId);
  const staleRaidPayload = structuredClone(raidRow.payload);
  staleRaidPayload.acceptedStats = {
    strength: 8,
    dexterity: 8,
    constitution: 8,
    intelligence: 8,
    wisdom: 8,
    charisma: 8,
  };
  staleRaidPayload.maxEncumbrance = 80;
  staleRaidPayload.encumbranceTier = "Heavy";

  await updateRow(supabaseUrl, publishableKey, accessToken, "raid_sessions", userId, { payload: staleRaidPayload });

  const updated = await repository.dispatchAction(accessToken, "move-toward-extract", {
    knownLogCount: startedRaid.activeRaid.logEntries.length,
  });

  assert.equal(updated.activeRaid.maxEncumbrance, 175);
  assert.equal(updated.playerMaxHealth, 34);
});

async function signUpLocalUser(supabaseUrl, publishableKey) {
  const password = "password-123";

  for (let attempt = 1; attempt <= 3; attempt += 1) {
    const email = `integration-${crypto.randomUUID()}@example.test`;
    const response = await fetch(`${supabaseUrl}/auth/v1/signup`, {
      method: "POST",
      headers: {
        apikey: publishableKey,
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        email,
        password,
      }),
    });

    if (response.status === 200) {
      const body = await response.json();
      assert.ok(body.access_token);
      assert.ok(body.user?.id);

      return {
        accessToken: body.access_token,
        userId: body.user.id,
      };
    }

    if (attempt === 3 || response.status < 500) {
      assert.equal(response.status, 200);
    }

    await new Promise(resolve => setTimeout(resolve, 250 * attempt));
  }
}

async function seedMainCharacterProfile(repository, supabaseUrl, publishableKey, accessToken, userId, weapon = createItem("Rusty Knife", 0, 5, 1, 0, 0, 1)) {
  await retryAsync(async () => {
    await repository.bootstrapProfile(accessToken);
  });

  await updateRow(supabaseUrl, publishableKey, accessToken, "game_saves", userId, {
    payload: {
      money: 500,
      mainStash: [],
      onPersonItems: [
        { item: weapon, isEquipped: true },
        { item: createItem("Small Backpack", 2, 18, 1, 1, 2, 1), isEquipped: true },
        { item: createItem("Bandage", 4, 4, 1, 0, 0, 1), isEquipped: false },
        { item: createItem("Ammo Box", 4, 6, 1, 0, 0, 4), isEquipped: false },
      ],
      playerConstitution: 12,
      playerMaxHealth: 34,
      randomCharacterAvailableAt: "0001-01-01T00:00:00+00:00",
      randomCharacter: null,
      activeRaid: null,
      acceptedStats: { strength: 14, dexterity: 8, constitution: 12, intelligence: 10, wisdom: 9, charisma: 16 },
      draftStats: { strength: 14, dexterity: 8, constitution: 12, intelligence: 10, wisdom: 9, charisma: 16 },
      availableStatPoints: 0,
      statsAccepted: true,
    },
  });
}

async function retryAsync(action, attempts = 3) {
  let lastError;
  for (let attempt = 1; attempt <= attempts; attempt += 1) {
    try {
      return await action();
    } catch (error) {
      lastError = error;
      if (!(error instanceof Error) || !error.message.includes("JWT issued at future") || attempt === attempts) {
        throw error;
      }

      await new Promise(resolve => setTimeout(resolve, 250 * attempt));
    }
  }

  throw lastError;
}

function createItem(name, type, value, slots, rarity, displayRarity, weight) {
  return { name, type, value, slots, rarity, displayRarity, weight };
}

async function getSingleRow(supabaseUrl, publishableKey, accessToken, tableName, userId) {
  const response = await fetch(
    `${supabaseUrl}/rest/v1/${tableName}?select=*&user_id=eq.${userId}`,
    {
      headers: {
        Authorization: `Bearer ${accessToken}`,
        apikey: publishableKey,
      },
    },
  );

  assert.equal(response.status, 200);
  const rows = await response.json();
  assert.equal(rows.length, 1);
  return rows[0];
}

async function updateRow(supabaseUrl, publishableKey, accessToken, tableName, userId, patch) {
  const response = await fetch(
    `${supabaseUrl}/rest/v1/${tableName}?user_id=eq.${userId}`,
    {
      method: "PATCH",
      headers: {
        Authorization: `Bearer ${accessToken}`,
        apikey: publishableKey,
        "Content-Type": "application/json",
        Prefer: "return=representation",
      },
      body: JSON.stringify(patch),
    },
  );

  assert.equal(response.status, 200);
  const rows = await response.json();
  assert.equal(rows.length, 1);
  return rows[0];
}
