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

test("game-action dispatches authorized mutations and returns authoritative snapshot", async () => {
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
  assert.equal(body.snapshot.money, 999);
  assert.equal(body.snapshot.mainStash[0].name, "Rusty Knife");
});
