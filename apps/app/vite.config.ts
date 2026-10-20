import tailwindcss from "@tailwindcss/vite";
import { tanstackRouter } from "@tanstack/router-plugin/vite";
import react from "@vitejs/plugin-react";
import { defineConfig } from "vite";

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    // Generates src/routeTree.gen.ts from src/routes. Must come before the React plugin.
    tanstackRouter({ target: "react", autoCodeSplitting: true, quoteStyle: "double" }),
    react(),
    tailwindcss(),
  ],
  server: {
    // The app calls the API on its own origin (src/api/client.ts), so the API needs no CORS in development.
    proxy: {
      "/api": {
        target: "http://localhost:5238",
        rewrite: (path) => path.replace(/^\/api/, ""),
      },
    },
  },
});
