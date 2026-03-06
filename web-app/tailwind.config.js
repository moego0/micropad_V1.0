/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{js,ts,jsx,tsx}'],
  darkMode: 'class',
  theme: {
    extend: {
      colors: {
        brand: {
          blue: '#0078d4',
          accent: '#0078d4'
        },
        surface: {
          primary: '#1e1e1e',
          secondary: '#252526',
          tertiary: '#2a2a2c',
          input: '#3c3c3c'
        },
        border: { DEFAULT: '#3c3c3c' },
        text: {
          primary: '#cccccc',
          secondary: '#9d9d9d',
          tertiary: '#6d6d6d'
        },
        success: '#28a745',
        error: '#dc3545'
      }
    }
  },
  plugins: []
};
