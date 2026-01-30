import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import path from 'path'
import { fileURLToPath } from 'url'
import type { Plugin } from 'vite'

const __dirname = path.dirname(fileURLToPath(import.meta.url))

// Certificate configuration - mkcert may generate files with different naming
// Vite will try the first path, then fall back to HTTP if files don't exist
const httpsConfig = {
  key: './.certs/_wildcard.localtest.me+1-key.pem',
  cert: './.certs/_wildcard.localtest.me+1.pem'
}

// Custom plugin to show only the URLs we care about
const customServerLogger = (): Plugin => {
  return {
    name: 'custom-server-logger',
    configureServer(server) {
      const originalPrintUrls = server.printUrls
      server.printUrls = () => {
        const info = server.config.logger.info
        const port = server.config.server.port
        const base = server.config.base
        
        // Extract backend port from proxy config
        const proxyConfig = server.config.server.proxy
        const proxyKey = Object.keys(proxyConfig || {})[0]
        const backendTarget = proxyConfig?.[proxyKey]?.target || 'http://localhost:7458'
        const backendPort = new URL(backendTarget).port
        
        info('')
        info(`  ${'\x1b[32m➜\x1b[0m'}  ${'\x1b[1mFrontend\x1b[0m'}:  https://demo.localtest.me:${port}${base}`)
        info(`  ${'\x1b[32m➜\x1b[0m'}  ${'\x1b[1mSwagger\x1b[0m'}:   http://localhost:${backendPort}/swagger`)
        info('')
      }
    }
  }
}

export default defineConfig({
  plugins: [react(), customServerLogger()],
  // Base path for the app - this value is exposed as import.meta.env.BASE_URL
  // and used by src/utils/api.ts to dynamically build API paths.
  // When deploying a new app, change this to match your app name (e.g., '/myapp/')
  base: '/sfab-gl07-gateway/',
  server: {
    port: 7358,
    host: true,  // Listen on all network interfaces but only show relevant URLs
    strictPort: true,  // Fail if port is already in use
    https: httpsConfig,
    cors: true,
    open: 'https://demo.localtest.me:7358/sfab-gl07-gateway/',
    proxy: {
      '/sfab-gl07-gateway/api': {
        target: 'http://localhost:7458',
        changeOrigin: true,
        secure: false,
        rewrite: (path) => path.replace(/^\/sfab-gl07-gateway\/api/, '/api'),
        configure: (proxy) => {
          proxy.on('error', (err) => {
            console.log('API Proxy error:', err);
          });
          proxy.on('proxyReq', (proxyReq, req, _res) => {
            const originalHost = req.headers['host'];
            if (originalHost) {
              proxyReq.setHeader('X-Forwarded-Host', originalHost);
              const [, forwardPort] = originalHost.split(':');
              proxyReq.setHeader('X-Forwarded-Proto', 'https');
              proxyReq.setHeader('X-Forwarded-Port', forwardPort ?? '7300');
              proxyReq.setHeader('X-Forwarded-Prefix', '/sfab-gl07-gateway');
              proxyReq.setHeader('Host', originalHost);
            }
            const remoteAddress = req.socket.remoteAddress;
            if (remoteAddress) {
              proxyReq.setHeader('X-Forwarded-For', remoteAddress);
            }
            console.log('Proxying API request:', req.method, req.url, '-> http://localhost:7458' + req.url);
          });
        }
      }
    }
  },
  build: {
    outDir: 'dist',
    assetsDir: 'assets',
    sourcemap: true,
    commonjsOptions: {
      include: [/node_modules/],
    },
    rollupOptions: {
      output: {
        manualChunks: {
          vendor: ['react', 'react-dom', 'react-router-dom'],
          mui: ['@mui/material', '@mui/icons-material', '@emotion/react', '@emotion/styled'],
        }
      }
    }
  },
  resolve: {
    dedupe: ['react', 'react-dom'],
    alias: {
      '@': path.resolve(__dirname, 'src'),
      '@components': path.resolve(__dirname, 'src/components'),
      '@services': path.resolve(__dirname, 'src/services'),
      '@hooks': path.resolve(__dirname, 'src/hooks'),
      '@types': path.resolve(__dirname, 'src/types'),
      '@utils': path.resolve(__dirname, 'src/utils'),
      '@config': path.resolve(__dirname, 'src/config')
    }
  },
  optimizeDeps: {
    include: [
      'react',
      'react-dom',
      'react-router-dom',
      'axios',
      'keycloak-js',
      '@mui/material',
      '@mui/icons-material',
      '@emotion/react',
      '@emotion/styled'
    ],
    force: true
  }
})
