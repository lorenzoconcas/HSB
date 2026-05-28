import type { Config } from 'tailwindcss'

export default {
  content: ['./index.html', './src/**/*.{vue,ts}'],
  theme: {
    extend: {
      colors: {
        ink: '#0f172a',
        mist: '#f8fafc',
        paper: '#ffffff',
        line: '#dbe4f0',
        accent: '#0f766e',
        accentSoft: '#ccfbf1',
        warning: '#b45309',
        danger: '#b91c1c',
      },
      boxShadow: {
        panel: '0 14px 36px -18px rgba(15, 23, 42, 0.18)',
      },
    },
  },
  plugins: [],
} satisfies Config
