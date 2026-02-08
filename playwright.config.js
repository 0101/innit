import { defineConfig } from "@playwright/test";

export default defineConfig({
  testDir: "./e2e",
  timeout: 120_000,
  expect: { timeout: 10_000 },
  use: {
    baseURL: "http://localhost:4173",
    viewport: { width: 1024, height: 768 },
    headless: true,
  },
  webServer: {
    command: "npx vite preview --port 4173 --strict-port",
    port: 4173,
    reuseExistingServer: false,
  },
  projects: [
    {
      name: "chromium",
      use: { browserName: "chromium" },
    },
  ],
});
