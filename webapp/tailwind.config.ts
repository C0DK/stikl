import aspectRatio from '@tailwindcss/aspect-ratio';
import containerQueries from '@tailwindcss/container-queries';
import forms from '@tailwindcss/forms';
import type { Config } from 'tailwindcss';

export default {
	darkMode: 'selector',
	content: ['./src/**/*.{html,js,svelte,ts}'],

	theme: {
		extend: {
			keyframes: {
				wiggle: {
					'0%': { transform: 'skewY(3deg)' },
					'25%': { transform: 'skewY(-3deg)' },
					'50%': { transform: 'skewY(2deg)' },
					'75%': { transform: 'skewY(-5deg)' },
					'100%': { transform: 'skewY(3deg)' }
				},
				'pulse-size': {
					'0%, 100%': { transform: 'translateY(-4px)' },
					'50%': { transform: 'translateY(0px)' }
				}
			},
			animation: {
				wiggle: 'wiggle 1s ease-in-out infinite',
				'pulse-size': 'pulse-size 1s ease-in-out infinite'
			}
		}
	},

	plugins: [forms, containerQueries, aspectRatio]
} satisfies Config;
