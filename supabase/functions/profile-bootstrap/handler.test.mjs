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

test("profile-bootstrap returns hydrated snapshot payload", async () => {
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
        MainStash: [{ Name: "Makarov", Type: 0, Value: 12, Slots: 1, Rarity: 0, DisplayRarity: 1 }],
        OnPersonItems: [],
        ShopStock: [{ Name: "Makarov", Type: 0, Value: 12, Slots: 1, Rarity: 0, DisplayRarity: 1 }],
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
  assert.equal(body.snapshot.mainStash[0].name, "Makarov");
  assert.equal(body.snapshot.shopStock[0].name, "Makarov");
});

test("profile-bootstrap returns keyed item identities in hydrated snapshot payload", async () => {
  const accessToken = [
    "eyJhbGciOiJub25lIn0",
    "eyJlbWFpbCI6InJhaWRlckBleGFtcGxlLmNvbSJ9",
    "signature",
  ].join(".");

  const handler = createProfileBootstrapHandler({
    bootstrapProfile: async () => ({
      Money: 500,
      MainStash: [{ Name: "Makarov", Type: 0, Value: 12, Slots: 1, Rarity: 0, DisplayRarity: 1 }],
      OnPersonItems: [],
      ShopStock: [{ Name: "Makarov", Type: 0, Value: 12, Slots: 1, Rarity: 0, DisplayRarity: 1 }],
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

  assert.equal(body.snapshot.mainStash[0].itemKey, "light_pistol");
  assert.equal(body.snapshot.shopStock[0].itemKey, "light_pistol");
});
