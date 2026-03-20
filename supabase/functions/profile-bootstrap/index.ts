import { createProfileRpcRepository } from "../_shared/profile-rpc.mjs";
import { createProfileBootstrapHandler } from "./handler.mjs";

const repository = createProfileRpcRepository({
  supabaseUrl: Deno.env.get("SUPABASE_URL"),
  publishableKey: Deno.env.get("SUPABASE_ANON_KEY") ?? Deno.env.get("SUPABASE_PUBLISHABLE_KEY"),
});

const handler = createProfileBootstrapHandler(repository);

Deno.serve((request) => handler(request));
