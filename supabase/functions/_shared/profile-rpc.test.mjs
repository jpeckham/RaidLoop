import test from "node:test";
import assert from "node:assert/strict";

import { createProfileRpcRepository } from "./profile-rpc.mjs";

test("dispatchAction retries a transient fetch reset once", async () => {
  let attempts = 0;
  const repository = createProfileRpcRepository({
    supabaseUrl: "https://example.supabase.co",
    publishableKey: "publishable-key",
    fetchImpl: async () => {
      attempts++;
      if (attempts === 1) {
        throw new TypeError("client error (Connect): Connection reset by peer (os error 104)");
      }

      return new Response(JSON.stringify({ ok: true }), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      });
    },
  });

  const result = await repository.dispatchAction("token-123", "attack", { target: "enemy" });

  assert.equal(attempts, 2);
  assert.deepEqual(result, { ok: true });
});
