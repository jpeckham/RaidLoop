(function () {
  const telemetryState = {
    initPromise: null,
    posthog: null,
    config: null
  };

  function resolveAppSettingsUrl() {
    return new URL("appsettings.json", document.baseURI).toString();
  }

  function resolvePostHogScriptUrl() {
    return "https://cdn.jsdelivr.net/npm/posthog-js@1.335.0/dist/array.js";
  }

  function asErrorDetails(reason) {
    if (reason instanceof Error) {
      return {
        message: reason.message,
        stack: reason.stack ?? ""
      };
    }

    if (typeof reason === "string") {
      return {
        message: reason,
        stack: ""
      };
    }

    try {
      return {
        message: JSON.stringify(reason),
        stack: ""
      };
    } catch {
      return {
        message: String(reason),
        stack: ""
      };
    }
  }

  function consoleReport(eventName, payload) {
    console.error(`[RaidLoop telemetry] ${eventName}`, payload);
  }

  function capture(eventName, payload) {
    consoleReport(eventName, payload);

    try {
      telemetryState.posthog?.capture(eventName, payload);
    } catch (error) {
      console.error("[RaidLoop telemetry] PostHog capture failed", error);
    }
  }

  function loadScript(src) {
    return new Promise((resolve, reject) => {
      const existing = document.querySelector(`script[data-raidloop-src="${src}"]`);
      if (existing) {
        resolve();
        return;
      }

      const script = document.createElement("script");
      script.async = true;
      script.defer = true;
      script.src = src;
      script.dataset.raidloopSrc = src;
      script.onload = () => resolve();
      script.onerror = () => reject(new Error(`Failed to load script: ${src}`));
      document.head.appendChild(script);
    });
  }

  async function loadConfig() {
    if (telemetryState.config) {
      return telemetryState.config;
    }

    const response = await fetch(resolveAppSettingsUrl(), { cache: "no-store" });
    if (!response.ok) {
      throw new Error(`Unable to load app settings: ${response.status}`);
    }

    const settings = await response.json();
    const posthog = settings.PostHog ?? settings.posthog ?? {};
    const config = {
      projectKey: posthog.ProjectKey ?? posthog.projectKey ?? "",
      host: posthog.Host ?? posthog.host ?? "https://us.i.posthog.com",
      sessionReplayEnabled: posthog.SessionReplayEnabled ?? posthog.sessionReplayEnabled ?? false,
      appVersion: settings.Version ?? settings.version ?? ""
    };

    if (!config.projectKey) {
      throw new Error("PostHog project key is missing from app settings.");
    }

    telemetryState.config = config;
    return config;
  }

  async function initialize() {
    if (telemetryState.initPromise) {
      return telemetryState.initPromise;
    }

    telemetryState.initPromise = (async () => {
      try {
        const config = await loadConfig();
        await loadScript(resolvePostHogScriptUrl());

        const posthog = window.posthog;
        if (!posthog || typeof posthog.init !== "function") {
          throw new Error("PostHog SDK failed to load.");
        }

        posthog.init(config.projectKey, {
          api_host: config.host,
          capture_pageview: true,
          disable_session_recording: !config.sessionReplayEnabled,
          loaded: loadedPosthog => {
            telemetryState.posthog = loadedPosthog;
          }
        });

        telemetryState.posthog = posthog;
      } catch (error) {
        console.error("[RaidLoop telemetry] initialization failed", error);
      }
    })();

    return telemetryState.initPromise;
  }

  function reportGlobalError(eventName, details) {
    capture(eventName, {
      source: "global-js",
      severity: "error",
      url: window.location.href,
      userAgent: navigator.userAgent,
      timestamp: new Date().toISOString(),
      appVersion: telemetryState.config?.appVersion ?? "",
      ...details
    });
  }

  window.onerror = (message, source, lineno, colno, error) => {
    const normalizedError = error instanceof Error ? error : null;
    reportGlobalError("client_unhandled_error", {
      message: String(message || normalizedError?.message || "Unhandled browser error."),
      stack: normalizedError?.stack ?? "",
      filename: source ?? "",
      lineno: lineno ?? 0,
      colno: colno ?? 0
    });
    return false;
  };

  window.addEventListener("unhandledrejection", event => {
    const reason = asErrorDetails(event.reason);
    reportGlobalError("client_unhandled_rejection", {
      message: reason.message || "Unhandled promise rejection.",
      stack: reason.stack
    });
  });

  window.RaidLoopTelemetry = {
    reportError: (message, details) => {
      capture("client_error", {
        source: details?.source ?? "handled-ui",
        severity: details?.severity ?? "error",
        message,
        stack: details?.stack ?? "",
        url: window.location.href,
        userAgent: navigator.userAgent,
        timestamp: new Date().toISOString(),
        appVersion: telemetryState.config?.appVersion ?? "",
        context: details?.context ?? {}
      });
    }
  };

  void initialize();
})();
