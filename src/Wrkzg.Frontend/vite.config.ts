import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    port: 5173,
    proxy: {
      '/api': 'http://localhost:5000',      // zur Kestrel API
      '/hubs': {
        target: 'http://localhost:5000',
        ws: true                            // WebSocket für SignalR
      }
    }
  },
  build: {
    outDir: '../Wrkzg.Api/wwwroot',     // Build-Artefakt direkt in wwwroot
    emptyOutDir: true
  }
})