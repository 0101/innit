import { defineConfig } from "vite";

export default defineConfig({
  build: {
    outDir: "deploy",
  },
  publicDir: "public",
  server: {
    host: "0.0.0.0",
    port: 8080,
  },
});
