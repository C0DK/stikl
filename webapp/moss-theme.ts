
import type { CustomThemeConfig } from '@skeletonlabs/tw-plugin';

export const mossTheme: CustomThemeConfig = {
	name: 'moss',
	properties: {
// =~= Theme Properties =~=
		"--theme-font-family-base": `system-ui`,
		"--theme-font-family-heading": `system-ui`,
		"--theme-font-color-base": "0 0 0",
		"--theme-font-color-dark": "255 255 255",
		"--theme-rounded-base": "9999px",
		"--theme-rounded-container": "8px",
		"--theme-border-base": "1px",
		// =~= Theme On-X Colors =~=
		"--on-primary": "255 255 255",
		"--on-secondary": "255 255 255",
		"--on-tertiary": "0 0 0",
		"--on-success": "0 0 0",
		"--on-warning": "0 0 0",
		"--on-error": "255 255 255",
		"--on-surface": "255 255 255",
		// =~= Theme Colors  =~=
		// primary | #609528
		"--color-primary-50": "231 239 223", // #e7efdf
		"--color-primary-100": "223 234 212", // #dfead4
		"--color-primary-200": "215 229 201", // #d7e5c9
		"--color-primary-300": "191 213 169", // #bfd5a9
		"--color-primary-400": "144 181 105", // #90b569
		"--color-primary-500": "96 149 40", // #609528
		"--color-primary-600": "86 134 36", // #568624
		"--color-primary-700": "72 112 30", // #48701e
		"--color-primary-800": "58 89 24", // #3a5918
		"--color-primary-900": "47 73 20", // #2f4914
		// secondary | #E35336
		"--color-secondary-50": "251 229 225", // #fbe5e1
		"--color-secondary-100": "249 221 215", // #f9ddd7
		"--color-secondary-200": "248 212 205", // #f8d4cd
		"--color-secondary-300": "244 186 175", // #f4baaf
		"--color-secondary-400": "235 135 114", // #eb8772
		"--color-secondary-500": "227 83 54", // #E35336
		"--color-secondary-600": "204 75 49", // #cc4b31
		"--color-secondary-700": "170 62 41", // #aa3e29
		"--color-secondary-800": "136 50 32", // #883220
		"--color-secondary-900": "111 41 26", // #6f291a
		// tertiary | #087e8b
		"--color-tertiary-50": "218 236 238", // #daecee
		"--color-tertiary-100": "206 229 232", // #cee5e8
		"--color-tertiary-200": "193 223 226", // #c1dfe2
		"--color-tertiary-300": "156 203 209", // #9ccbd1
		"--color-tertiary-400": "82 165 174", // #52a5ae
		"--color-tertiary-500": "8 126 139", // #087e8b
		"--color-tertiary-600": "7 113 125", // #07717d
		"--color-tertiary-700": "6 95 104", // #065f68
		"--color-tertiary-800": "5 76 83", // #054c53
		"--color-tertiary-900": "4 62 68", // #043e44
		// success | #84cc16
		"--color-success-50": "237 247 220", // #edf7dc
		"--color-success-100": "230 245 208", // #e6f5d0
		"--color-success-200": "224 242 197", // #e0f2c5
		"--color-success-300": "206 235 162", // #ceeba2
		"--color-success-400": "169 219 92", // #a9db5c
		"--color-success-500": "132 204 22", // #84cc16
		"--color-success-600": "119 184 20", // #77b814
		"--color-success-700": "99 153 17", // #639911
		"--color-success-800": "79 122 13", // #4f7a0d
		"--color-success-900": "65 100 11", // #41640b
		// warning | #EAB308
		"--color-warning-50": "252 244 218", // #fcf4da
		"--color-warning-100": "251 240 206", // #fbf0ce
		"--color-warning-200": "250 236 193", // #faecc1
		"--color-warning-300": "247 225 156", // #f7e19c
		"--color-warning-400": "240 202 82", // #f0ca52
		"--color-warning-500": "234 179 8", // #EAB308
		"--color-warning-600": "211 161 7", // #d3a107
		"--color-warning-700": "176 134 6", // #b08606
		"--color-warning-800": "140 107 5", // #8c6b05
		"--color-warning-900": "115 88 4", // #735804
		// error | #D41976
		"--color-error-50": "249 221 234", // #f9ddea
		"--color-error-100": "246 209 228", // #f6d1e4
		"--color-error-200": "244 198 221", // #f4c6dd
		"--color-error-300": "238 163 200", // #eea3c8
		"--color-error-400": "225 94 159", // #e15e9f
		"--color-error-500": "212 25 118", // #D41976
		"--color-error-600": "191 23 106", // #bf176a
		"--color-error-700": "159 19 89", // #9f1359
		"--color-error-800": "127 15 71", // #7f0f47
		"--color-error-900": "104 12 58", // #680c3a
		// surface | #466D1D
		"--color-surface-50": "227 233 221", // #e3e9dd
		"--color-surface-100": "218 226 210", // #dae2d2
		"--color-surface-200": "209 219 199", // #d1dbc7
		"--color-surface-300": "181 197 165", // #b5c5a5
		"--color-surface-400": "126 153 97", // #7e9961
		"--color-surface-500": "70 109 29", // #466D1D
		"--color-surface-600": "63 98 26", // #3f621a
		"--color-surface-700": "53 82 22", // #355216
		"--color-surface-800": "42 65 17", // #2a4111
		"--color-surface-900": "34 53 14", // #22350e

	}
}