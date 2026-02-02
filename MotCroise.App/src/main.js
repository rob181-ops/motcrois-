import { createApp } from 'vue';
import { createVuetify } from 'vuetify';
import App from './App.vue';
import 'vuetify/styles';
import '@mdi/font/css/materialdesignicons.css';
import '@fontsource-variable/manrope/index.css';
import './styles.css';

const vuetify = createVuetify({
  defaults: {
    VBtn: { rounded: 'lg' },
    VCard: { rounded: 'xl' },
    VChip: { rounded: 'lg' },
    VTextField: { rounded: 'lg' },
  },
  theme: {
    defaultTheme: 'motcroise',
    themes: {
      motcroise: {
        dark: true,
        colors: {
          background: '#0b0a10',
          surface: '#111025',
          primary: '#ff4fd8',
          secondary: '#7c5cff',
          info: '#ffd56b',
          success: '#32d583',
          warning: '#fdb022',
          error: '#f97066',
        },
      },
    },
  },
  icons: {
    defaultSet: 'mdi',
  },
});

createApp(App).use(vuetify).mount('#root');
