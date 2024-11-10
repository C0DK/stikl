import { join } from 'path';

import aspectRatio from '@tailwindcss/aspect-ratio';
import containerQueries from '@tailwindcss/container-queries';
import forms from '@tailwindcss/forms';
import { skeleton } from '@skeletonlabs/tw-plugin';
import type { Config } from 'tailwindcss';

export default {
	darkMode: 'selector',
	content: [
		'./src/**/*.{html,js,svelte,ts}',

		join(require.resolve('@skeletonlabs/skeleton'), '../**/*.{html,js,svelte,ts}')
	],

	theme: {
		extend: {
			colors: {
				// Light mode colors
				primary: {
					DEFAULT: '#228B22', // Forest Green
					light: '#32CD32' // Leafy Green
				},
				secondary: {
					DEFAULT: '#D2B48C', // Clay Brown
					light: '#F5F5DC' // Soft Beige
				},
				accent: {
					DEFAULT: '#FFD700', // Golden Sunflower
					muted: '#708090' // Sage Gray
				},
				neutral: {
					dark: '#333333', // Charcoal for text and dark elements
					light: '#FAFAFA' // Soft White for backgrounds
				},

				// Dark mode colors
				dark: {
					primary: '#6CBF6C', // A lighter forest green for dark mode
					secondary: '#A78E74', // Softer clay brown
					accent: '#FFEB85', // Lighter golden for contrast on dark
					neutral: '#1A1A1A' // Almost black for dark backgrounds
				}
			}
		}
	},

	plugins: [
		forms,
		containerQueries,
		aspectRatio,
		skeleton({
			themes: { preset: ['vintage'] }
		})
	]
} satisfies Config;
