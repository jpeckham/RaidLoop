function normalizePath(path) {
  return path.replace(/^\/+|\/+$/g, "");
}

function isTransientFetchError(error) {
  if (!(error instanceof Error)) {
    return false;
  }

  const message = error.message.toLowerCase();
  return message.includes("connection reset by peer")
    || message.includes("client error (connect)")
    || message.includes("socket hang up");
}

export function createProfileRpcRepository({
  supabaseUrl,
  publishableKey,
  fetchImpl = fetch,
} = {}) {
  if (!supabaseUrl) {
    throw new Error("SUPABASE_URL is required.");
  }

  if (!publishableKey) {
    throw new Error("SUPABASE_PUBLISHABLE_KEY is required.");
  }

  const baseUrl = `${supabaseUrl.replace(/\/+$/g, "")}/rest/v1/rpc`;

  async function invoke(path, accessToken, payload) {
    const requestUrl = `${baseUrl}/${normalizePath(path)}`;
    const requestInit = {
      method: "POST",
      headers: {
        Authorization: `Bearer ${accessToken}`,
        apikey: publishableKey,
        "Content-Type": "application/json",
      },
      body: JSON.stringify(payload),
    };

    let response;
    try {
      response = await fetchImpl(requestUrl, requestInit);
    } catch (error) {
      if (!isTransientFetchError(error)) {
        throw error;
      }

      response = await fetchImpl(requestUrl, requestInit);
    }

    if (!response.ok) {
      const message = await response.text();
      throw new Error(message || `Supabase RPC ${path} failed with ${response.status}.`);
    }

    return await response.json();
  }

  return {
    bootstrapProfile(accessToken) {
      return invoke("profile_bootstrap", accessToken, {});
    },
    saveProfile(accessToken, snapshot) {
      return invoke("profile_save", accessToken, { snapshot });
    },
    dispatchAction(accessToken, action, payload) {
      return invoke("game_action", accessToken, { action, payload });
    },
  };
}
