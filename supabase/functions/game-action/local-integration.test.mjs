import test from "node:test";
import assert from "node:assert/strict";

import { createProfileRpcRepository } from "../_shared/profile-rpc.mjs";

const DEFAULT_SUPABASE_URL = "http://127.0.0.1:54321";
const DEFAULT_PUBLISHABLE_KEY = "sb_publishable_ACJWlzQHlZjBrEguHvfOxg_3BJgxAaH";

test("local game_action routes full-auto through the authoritative raid action path", async () => {
  const supabaseUrl = Deno.env.get("SUPABASE_URL") ?? DEFAULT_SUPABASE_URL;
  const publishableKey = Deno.env.get("SUPABASE_PUBLISHABLE_KEY") ?? DEFAULT_PUBLISHABLE_KEY;
  const repository = createProfileRpcRepository({ supabaseUrl, publishableKey, fetchImpl: fetch });

  for (let attempt = 0; attempt < 10; attempt++) {
    const { accessToken } = await signUpLocalUser(supabaseUrl, publishableKey);
    const startedRaid = await repository.dispatchAction(accessToken, "start-random-raid", {});

    if (startedRaid.activeRaid?.encounterType !== "Combat") {
      continue;
    }

    const snapshot = await repository.dispatchAction(accessToken, "full-auto", {
      target: "enemy",
      knownLogCount: 1,
    });

    assert.equal(snapshot.activeRaid.ammo, 8);
    assert.match(snapshot.activeRaid.logEntries[0], /^Raid started as .+\.$/);
    assert.equal(snapshot.activeRaid.logEntries[1], "Weapon does not support full auto.");
    assert.match(snapshot.activeRaid.logEntries[2], /^(Scavenger|Patrol Guard) (hits|misses) you/);
    assert.equal(snapshot.activeRaid.logEntries.length, 3);
    return;
  }

  assert.fail("Could not obtain a combat encounter from start-random-raid after 10 attempts.");
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
  };
}
