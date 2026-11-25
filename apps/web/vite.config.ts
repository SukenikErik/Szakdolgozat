import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    // The browser only talks to Vite, so no CORS setup is needed in development.
    proxy: { '/api': process.env.API_URL ?? 'http://localhost:5065' },
  },
})
