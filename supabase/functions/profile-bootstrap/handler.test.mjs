import test from "node:test";
import assert from "node:assert/strict";

import { createProfileBootstrapHandler } from "./handler.mjs";

test("profile-bootstrap responds to preflight requests", async () => {
  const handler = createProfileBootstrapHandler({
    bootstrapProfile: async () => {
      throw new Error("should not execute");
    },
  });

  const response = await handler(new Request("https://example.test/profile-bootstrap", {
    method: "OPTIONS",
  }));

  assert.equal(response.status, 200);
  assert.equal(response.headers.get("Access-Control-Allow-Origin"), "*");
});

test("profile-bootstrap rejects non-POST requests", async () => {
  const handler = createProfileBootstrapHandler({
    bootstrapProfile: async () => {
      throw new Error("should not execute");
    },
  });

  const response = await handler(new Request("https://example.test/profile-bootstrap", { method: "GET" }));

  assert.equal(response.status, 405);
  assert.equal(await response.text(), "Method Not Allowed");
});

test("profile-bootstrap rejects missing bearer token", async () => {
  const handler = createProfileBootstrapHandler({
    bootstrapProfile: async () => {
      throw new Error("should not execute");
    },
  });

  const response = await handler(new Request("https://example.test/profile-bootstrap", { method: "POST" }));

  assert.equal(response.status, 401);
  assert.deepEqual(await response.json(), { error: "Authentication required." });
});

test("profile-bootstrap returns hydrated snapshot payload with runtime itemDefId and rules catalog only", async () => {
  const accessToken = [
    "eyJhbGciOiJub25lIn0",
    "eyJlbWFpbCI6InJhaWRlckBleGFtcGxlLmNvbSJ9",
    "signature",
  ].join(".");

  const handler = createProfileBootstrapHandler({
    bootstrapProfile: async (token) => {
      assert.equal(token, accessToken);
      return {
        Money: 500,
        ItemRules: [
          { ItemDefId: 1, Type: 0, Weight: 1, Slots: 1, Rarity: 0 },
          { ItemDefId: 2, Type: 0, Weight: 2, Slots: 1, Rarity: 0 },
          { ItemDefId: 19, Type: 3, Weight: 1, Slots: 1, Rarity: 0 },
        ],
        MainStash: [{ ItemDefId: 2 }],
        OnPersonItems: [],
        ShopStock: [{ ItemDefId: 2, Price: 60, Stock: 1 }],
        RandomCharacterAvailableAt: "0001-01-01T00:00:00+00:00",
        RandomCharacter: null,
      };
    },
  });

  const response = await handler(new Request("https://example.test/profile-bootstrap", {
    method: "POST",
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
  }));

  assert.equal(response.status, 200);
  const body = await response.json();
  assert.equal(body.isAuthenticated, true);
  assert.equal(body.userEmail, "raider@example.com");
  assert.equal(body.snapshot.money, 500);
  assert.equal(body.snapshot.mainStash[0].itemDefId, 2);
  assert.equal("name" in body.snapshot.mainStash[0], false);
  assert.equal("itemKey" in body.snapshot.mainStash[0], false);
  assert.equal("type" in body.snapshot.mainStash[0], false);
  assert.equal("weight" in body.snapshot.mainStash[0], false);
  assert.equal("slots" in body.snapshot.mainStash[0], false);
  assert.equal(body.snapshot.shopStock[0].itemDefId, 2);
  assert.equal(body.snapshot.shopStock[0].price, 60);
  assert.equal(body.snapshot.itemRules.length, 3);
  assert.equal(body.snapshot.itemRules[1].itemDefId, 2);
  assert.equal(body.snapshot.itemRules[1].weight, 2);
});

test("profile-bootstrap does not emit legacy runtime name or itemKey fields", async () => {
  const accessToken = [
    "eyJhbGciOiJub25lIn0",
    "eyJlbWFpbCI6InJhaWRlckBleGFtcGxlLmNvbSJ9",
    "signature",
  ].join(".");

  const handler = createProfileBootstrapHandler({
    bootstrapProfile: async () => ({
      Money: 500,
      ItemRules: [
          { ItemDefId: 2, Type: 0, Weight: 2, Slots: 1, Rarity: 0 },
      ],
      MainStash: [{ ItemDefId: 2 }],
      OnPersonItems: [],
      ShopStock: [{ ItemDefId: 2, Price: 60, Stock: 1 }],
      RandomCharacterAvailableAt: "0001-01-01T00:00:00+00:00",
      RandomCharacter: null,
    }),
  });

  const response = await handler(new Request("https://example.test/profile-bootstrap", {
    method: "POST",
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
  }));

  const body = await response.json();

  assert.equal("name" in body.snapshot.mainStash[0], false);
  assert.equal("itemKey" in body.snapshot.mainStash[0], false);
  assert.equal("name" in body.snapshot.shopStock[0], false);
  assert.equal("itemKey" in body.snapshot.shopStock[0], false);
});

test("profile-bootstrap normalizes legacy named items into itemDefId-only runtime payloads", async () => {
  const accessToken = [
    "eyJhbGciOiJub25lIn0",
    "eyJlbWFpbCI6InJhaWRlckBleGFtcGxlLmNvbSJ9",
    "signature",
  ].join(".");

  const handler = createProfileBootstrapHandler({
    bootstrapProfile: async () => ({
      Money: 500,
      MainStash: [{ Name: "Makarov", Type: 0, Value: 60, Slots: 1, Rarity: 0, DisplayRarity: 1, Weight: 2 }],
      OnPersonItems: [],
      ShopStock: [{ Name: "Medkit", Type: 3, Value: 30, Slots: 1, Rarity: 0, DisplayRarity: 1, Weight: 1 }],
      RandomCharacterAvailableAt: "0001-01-01T00:00:00+00:00",
      RandomCharacter: null,
    }),
  });

  const response = await handler(new Request("https://example.test/profile-bootstrap", {
    method: "POST",
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
  }));

  const body = await response.json();

  assert.equal(body.snapshot.mainStash[0].itemDefId, 2);
  assert.equal("name" in body.snapshot.mainStash[0], false);
  assert.equal("itemKey" in body.snapshot.mainStash[0], false);
  assert.equal("type" in body.snapshot.mainStash[0], false);
  assert.equal("weight" in body.snapshot.mainStash[0], false);
  assert.equal(body.snapshot.shopStock[0].itemDefId, 19);
  assert.equal("name" in body.snapshot.shopStock[0], false);
  assert.equal("itemKey" in body.snapshot.shopStock[0], false);
});

test("profile-bootstrap accepts persisted authored items with itemDefId only", async () => {
  const accessToken = [
    "eyJhbGciOiJub25lIn0",
    "eyJlbWFpbCI6InJhaWRlckBleGFtcGxlLmNvbSJ9",
    "signature",
  ].join(".");

  const handler = createProfileBootstrapHandler({
    bootstrapProfile: async () => ({
      Money: 500,
      MainStash: [{ ItemDefId: 2 }],
      OnPersonItems: [{ Item: { ItemDefId: 4 }, IsEquipped: true }],
      ShopStock: [{ ItemDefId: 19, Price: 60, Stock: 1 }],
      RandomCharacterAvailableAt: "0001-01-01T00:00:00+00:00",
      RandomCharacter: null,
    }),
  });

  const response = await handler(new Request("https://example.test/profile-bootstrap", {
    method: "POST",
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
  }));

  assert.equal(response.status, 200);
  const body = await response.json();
  assert.equal(body.snapshot.mainStash[0].itemDefId, 2);
  assert.equal("name" in body.snapshot.mainStash[0], false);
  assert.equal("itemKey" in body.snapshot.mainStash[0], false);
  assert.equal(body.snapshot.onPersonItems[0].item.itemDefId, 4);
  assert.equal("name" in body.snapshot.onPersonItems[0].item, false);
  assert.equal("itemKey" in body.snapshot.onPersonItems[0].item, false);
  assert.equal(body.snapshot.shopStock[0].itemDefId, 19);
  assert.equal(body.snapshot.shopStock[0].price, 60);
  assert.equal(body.snapshot.shopStock[0].stock, 1);
  assert.equal("name" in body.snapshot.shopStock[0], false);
  assert.equal("itemKey" in body.snapshot.shopStock[0], false);
});

test("profile-bootstrap does not resolve itemKey-only authored payloads", async () => {
  const accessToken = [
    "eyJhbGciOiJub25lIn0",
    "eyJlbWFpbCI6InJhaWRlckBleGFtcGxlLmNvbSJ9",
    "signature",
  ].join(".");

  const handler = createProfileBootstrapHandler({
    bootstrapProfile: async () => ({
      Money: 500,
      MainStash: [{ ItemKey: "makarov", Type: 0, Value: 60, Slots: 1, Rarity: 0, DisplayRarity: 1, Weight: 2 }],
      OnPersonItems: [],
      ShopStock: [],
      RandomCharacterAvailableAt: "0001-01-01T00:00:00+00:00",
      RandomCharacter: null,
    }),
  });

  const response = await handler(new Request("https://example.test/profile-bootstrap", {
    method: "POST",
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
  }));

  const body = await response.json();
  assert.equal("itemDefId" in body.snapshot.mainStash[0], false);
});

test("profile-bootstrap injects the full item rules catalog when backend snapshot omits it", async () => {
  const accessToken = [
    "eyJhbGciOiJub25lIn0",
    "eyJlbWFpbCI6InJhaWRlckBleGFtcGxlLmNvbSJ9",
    "signature",
  ].join(".");

  const handler = createProfileBootstrapHandler({
    bootstrapProfile: async () => ({
      Money: 500,
      MainStash: [{ ItemDefId: 2 }],
      OnPersonItems: [],
      ShopStock: [{ ItemDefId: 2, Price: 60, Stock: 1 }],
      RandomCharacterAvailableAt: "0001-01-01T00:00:00+00:00",
      RandomCharacter: null,
    }),
  });

  const response = await handler(new Request("https://example.test/profile-bootstrap", {
    method: "POST",
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
  }));

  const body = await response.json();

  assert.ok(Array.isArray(body.snapshot.itemRules));
  assert.equal(body.snapshot.itemRules.length, 24);
  assert.deepEqual(body.snapshot.itemRules[0], { itemDefId: 1, type: 0, weight: 1, slots: 1, rarity: 0 });
  assert.deepEqual(body.snapshot.itemRules.at(-1), { itemDefId: 24, type: 5, weight: 1, slots: 1, rarity: 4 });
});
