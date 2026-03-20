import fs from "node:fs/promises";
import path from "node:path";
import { chromium } from "playwright";

const appUrl = process.env.APP_URL ?? "http://localhost:5214/";
const outputPath = path.resolve("playwright-auth-debug-output.json");

const results = {
  appUrl,
  startedAt: new Date().toISOString(),
  pageErrors: [],
  consoleMessages: [],
  requests: [],
  responses: [],
  snapshots: [],
};

function redactBearer(headerValue) {
  if (!headerValue) {
    return null;
  }

  const prefix = "Bearer ";
  if (!headerValue.startsWith(prefix)) {
    return headerValue;
  }

  const token = headerValue.slice(prefix.length);
  if (token.length <= 35) {
    return `${prefix}${token}`;
  }

  return `${prefix}${token.slice(0, 25)}...${token.slice(-10)}`;
}

async function captureStorage(page, label) {
  const snapshot = await page.evaluate(() => ({
    href: window.location.href,
    localStorage: Object.fromEntries(
      Array.from({ length: window.localStorage.length }, (_, index) => {
        const key = window.localStorage.key(index);
        return [key, key ? window.localStorage.getItem(key) : null];
      }),
    ),
    sessionStorage: Object.fromEntries(
      Array.from({ length: window.sessionStorage.length }, (_, index) => {
        const key = window.sessionStorage.key(index);
        return [key, key ? window.sessionStorage.getItem(key) : null];
      }),
    ),
  }));

  results.snapshots.push({
    label,
    capturedAt: new Date().toISOString(),
    ...snapshot,
  });
}

const browser = await chromium.launch({
  headless: false,
  slowMo: 150,
});

const context = await browser.newContext();
const page = await context.newPage();

page.on("pageerror", (error) => {
  results.pageErrors.push({
    message: error.message,
    stack: error.stack ?? null,
  });
});

page.on("console", (message) => {
  results.consoleMessages.push({
    type: message.type(),
    text: message.text(),
  });
});

page.on("request", async (request) => {
  if (!request.url().includes("/functions/v1/profile-bootstrap")) {
    return;
  }

  results.requests.push({
    method: request.method(),
    url: request.url(),
    headers: {
      authorization: redactBearer(request.headers().authorization ?? null),
      apikey: request.headers().apikey ?? null,
      origin: request.headers().origin ?? null,
      contentType: request.headers()["content-type"] ?? null,
    },
    postData: request.postData() ?? null,
    timestamp: new Date().toISOString(),
  });
});

page.on("response", async (response) => {
  if (!response.url().includes("/functions/v1/profile-bootstrap")) {
    return;
  }

  let bodyText = null;
  try {
    bodyText = await response.text();
  } catch {
    bodyText = null;
  }

  results.responses.push({
    url: response.url(),
    status: response.status(),
    statusText: response.statusText(),
    headers: {
      accessControlAllowOrigin: response.headers()["access-control-allow-origin"] ?? null,
      contentType: response.headers()["content-type"] ?? null,
    },
    bodyText,
    timestamp: new Date().toISOString(),
  });
});

await page.goto(appUrl, { waitUntil: "networkidle" });
await captureStorage(page, "after-initial-load");

const googleButton = page.getByRole("button", { name: "Sign in with Google" });
if (await googleButton.isVisible().catch(() => false)) {
  await googleButton.click();
}

console.log("");
console.log("Playwright auth debug browser is open.");
console.log("Complete the Google login manually in that browser window.");
console.log("The script will wait up to 3 minutes for the profile-bootstrap response.");
console.log("");

try {
  await page.waitForResponse(
    (response) =>
      response.url().includes("/functions/v1/profile-bootstrap") &&
      response.request().method() === "POST",
    { timeout: 180000 },
  );
} catch (error) {
  results.pageErrors.push({
    message: error instanceof Error ? error.message : String(error),
    stack: error instanceof Error ? error.stack ?? null : null,
  });
}

await page.waitForLoadState("networkidle").catch(() => {});
await captureStorage(page, "after-login-attempt");

results.finishedAt = new Date().toISOString();

await fs.writeFile(outputPath, `${JSON.stringify(results, null, 2)}\n`, "utf8");

console.log(`Debug output written to ${outputPath}`);
console.log("The browser will close automatically in 20 seconds.");

await page.waitForTimeout(20000);
await browser.close();
