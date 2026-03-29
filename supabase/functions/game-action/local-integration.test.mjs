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

test("local profile_bootstrap exposes keyed item identities for legacy saved items", async () => {
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

  assert.equal(bootstrap.mainStash[0].itemKey, "makarov");
  assert.equal(bootstrap.mainStash[1].itemKey, "6b2_body_armor");
  assert.equal(bootstrap.onPersonItems[0].item.itemKey, "ak74");
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
  const email = `integration-${crypto.randomUUID()}@example.test`;
  const password = "password-123";
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

  assert.equal(response.status, 200);
  const body = await response.json();
  assert.ok(body.access_token);
  assert.ok(body.user?.id);

  return {
    accessToken: body.access_token,
    userId: body.user.id,
  };
}

async function seedMainCharacterProfile(repository, supabaseUrl, publishableKey, accessToken, userId, weapon = createItem("Rusty Knife", 0, 5, 1, 0, 0, 1)) {
  await repository.bootstrapProfile(accessToken);

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
