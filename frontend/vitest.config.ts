import { defineConfig } from 'vitest/config';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  test: {
    globals: true,                        
    environment: 'jsdom',                 
    setupFiles: './src/test/setup.ts',    
    maxThreads: 1,
    minThreads: 1,
    deps: {
        inline: ['@mui/icons-material'],
    },
    coverage: {
      reporter: ['text', 'json', 'html'], 
    },
  },
});