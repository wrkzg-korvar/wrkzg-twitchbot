import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    port: 5173,
    proxy: {
      '/api': 'http://localhost:5050',      // to the Kestrel API
      '/auth': 'http://localhost:5050',     // OAuth + setup endpoints
      '/hubs': {
        target: 'http://localhost:5050',
        ws: true                            // WebSocket for SignalR
      }
    }
  },
  build: {
    outDir: '../Wrkzg.Api/wwwroot',     // build artifact directly into wwwroot
    emptyOutDir: true
  }
})