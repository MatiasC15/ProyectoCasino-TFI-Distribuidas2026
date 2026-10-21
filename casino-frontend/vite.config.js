import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

const api = 'https://localhost:7298'

export default defineConfig({
  plugins: [react()],
  server: {
    host: 'localhost',
    port: 5173,
    strictPort: true,
    proxy: {
      '/api': { target: api, secure: false },
      '/hubs': { target: api, secure: false, ws: true },
    },
  },
})
