window.raidLoopStorage = {
  load: function (key) {
    return window.localStorage.getItem(key);
  },
  save: function (key, value) {
    window.localStorage.setItem(key, value);
  },
  remove: function (key) {
    window.localStorage.removeItem(key);
  }
};

(function () {
  const authDebugKey = "raidloop.auth.debug.v1";
  const gameActionDebugKey = "raidloop.game-action.debug.v1";
  const originalFetch = window.fetch.bind(window);

  function redactBearer(value) {
    if (!value || !value.startsWith("Bearer ")) {
      return value;
    }

    const token = value.slice("Bearer ".length);
    if (token.length <= 35) {
      return value;
    }

    return "Bearer " + token.slice(0, 25) + "..." + token.slice(-10);
  }

  function saveDebugRecord(key, record) {
    try {
      window.localStorage.setItem(key, JSON.stringify(record, null, 2));
      console.log(key, record);
    } catch (error) {
      console.warn("raidloop debug save failed", error);
    }
  }

  window.fetch = async function (input, init) {
    const request = input instanceof Request ? input : new Request(input, init);
    const url = request.url || "";
    const isProfileBootstrap = url.includes("/functions/v1/profile-bootstrap");
    const isGameAction = url.includes("/functions/v1/game-action");

    if (!isProfileBootstrap && !isGameAction) {
      return originalFetch(input, init);
    }

    const authHeader = request.headers.get("authorization");
    const apikeyHeader = request.headers.get("apikey");
    const debugKey = isProfileBootstrap ? authDebugKey : gameActionDebugKey;
    let requestBody = null;

    try {
      requestBody = await request.clone().text();
    } catch {
      requestBody = null;
    }

    try {
      let authUserProbe = null;
      if (isProfileBootstrap && authHeader && apikeyHeader) {
        try {
          const probeResponse = await originalFetch("https://dblgbpzlrglcdwqyagnx.supabase.co/auth/v1/user", {
            method: "GET",
            headers: {
              authorization: authHeader,
              apikey: apikeyHeader
            }
          });

          let probeBodyText = null;
          try {
            probeBodyText = await probeResponse.text();
          } catch {
            probeBodyText = null;
          }

          authUserProbe = {
            status: probeResponse.status,
            statusText: probeResponse.statusText,
            bodyText: probeBodyText
          };
        } catch (probeError) {
          authUserProbe = {
            error: probeError instanceof Error ? probeError.message : String(probeError)
          };
        }
      }

      const response = await originalFetch(input, init);
      const cloned = response.clone();
      let bodyText = null;

      try {
        bodyText = await cloned.text();
      } catch {
        bodyText = null;
      }

      saveDebugRecord(debugKey, {
        timestamp: new Date().toISOString(),
        url: url,
        method: request.method,
        request: {
          authorization: redactBearer(authHeader),
          authorizationLength: authHeader ? authHeader.length : null,
          apikey: apikeyHeader,
          href: window.location.href,
          bodyText: requestBody
        },
        response: {
          status: response.status,
          statusText: response.statusText,
          bodyText: bodyText
        },
        authUserProbe: authUserProbe
      });

      return response;
    } catch (error) {
      saveDebugRecord(debugKey, {
        timestamp: new Date().toISOString(),
        url: url,
        method: request.method,
        request: {
          authorization: redactBearer(authHeader),
          authorizationLength: authHeader ? authHeader.length : null,
          apikey: apikeyHeader,
          href: window.location.href,
          bodyText: requestBody
        },
        error: error instanceof Error ? error.message : String(error)
      });

      throw error;
    }
  };
})();
