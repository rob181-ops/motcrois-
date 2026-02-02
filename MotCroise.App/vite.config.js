import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';
import vuetify from 'vite-plugin-vuetify';

export default defineConfig({
  plugins: [vue(), vuetify({ autoImport: true })],
  resolve: {
    // Fix Windows junction/symlink builds (e.g. C:\motcroisé -> E:\projets\motcroisé)
    // where path.relative() across drives can produce absolute paths and break the HTML build.
    preserveSymlinks: true,
  },
});
