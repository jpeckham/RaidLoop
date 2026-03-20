import {
  json,
  methodNotAllowed,
  ok,
} from "../_shared/http.mjs";

export function createProfileSaveHandler() {
  return async function handleProfileSave(request) {
    if (request.method === "OPTIONS") {
      return ok();
    }

    if (request.method !== "POST") {
      return methodNotAllowed();
    }

    return json(
      { error: "Profile saving is no longer supported. Use action endpoints." },
      { status: 410 },
    );
  };
}
