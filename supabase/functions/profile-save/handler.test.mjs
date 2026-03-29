import test from "node:test";
import assert from "node:assert/strict";

import { createProfileSaveHandler } from "./handler.mjs";

test("profile-save rejects non-POST requests", async () => {
  const handler = createProfileSaveHandler();

  const response = await handler(new Request("https://example.test/profile-save", { method: "GET" }));

  assert.equal(response.status, 405);
  assert.equal(await response.text(), "Method Not Allowed");
});

test("profile-save is disabled in favor of action endpoints", async () => {
  const handler = createProfileSaveHandler();
  const response = await handler(new Request("https://example.test/profile-save", {
    method: "POST",
    headers: {
      Authorization: "Bearer token-456",
      "Content-Type": "application/json",
    },
    body: JSON.stringify({
      snapshot: {
        money: 725,
        mainStash: [{ itemKey: "field_carbine", name: "AK74", type: 0, value: 34, slots: 1, rarity: 2, displayRarity: 3 }],
        onPersonItems: [],
        randomCharacterAvailableAt: "2026-03-18T05:00:00+00:00",
        randomCharacter: null,
        activeRaid: null,
      },
    }),
  }));

  assert.equal(response.status, 410);
  assert.deepEqual(await response.json(), {
    error: "Profile saving is no longer supported. Use action endpoints.",
  });
});
