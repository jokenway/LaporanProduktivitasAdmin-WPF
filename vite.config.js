import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [react()],
  base: './', // important for electron relative paths
  server: {
    port: 5173,
    strictPort: false // automatically switch to next free port if occupied
  },
  preview: {
    port: 4173,
    strictPort: false // automatically switch to next free port if 4173 is in use
  },
  build: {
    outDir: 'dist',
    emptyOutDir: true,
    chunkSizeWarningLimit: 3000
  }
});
