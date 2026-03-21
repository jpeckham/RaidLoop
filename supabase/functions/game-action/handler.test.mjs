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
