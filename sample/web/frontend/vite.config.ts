import { defineConfig, loadEnv } from 'vite';
import { svelte } from '@sveltejs/vite-plugin-svelte';

export default defineConfig(({ mode }) => {
  const apiTarget = loadEnv(mode, '.', 'RAGENT_').RAGENT_API_URL || 'http://127.0.0.1:3000';

  return {
  plugins: [svelte()],
  server: {
    host: '127.0.0.1',
    port: 5173,
    strictPort: true,
    proxy: {
      '/api': {
        target: apiTarget,
        changeOrigin: true
      }
    }
  },
  build: {
    outDir: '../backend/AgentStudio.Api/wwwroot',
    emptyOutDir: true
  }
  };
});
