/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  darkMode: 'class',
  theme: {
    extend: {
      colors: {
        opure: {
          dark: '#050505',
          glass: 'rgba(20, 20, 25, 0.6)',
          border: 'rgba(255, 255, 255, 0.08)',
          accent: '#3b82f6', 
        }
      },
      animation: {
        'spin-slow': 'spin 8s linear infinite',
      }
    },
  },
  plugins: [],
}