import {
	dvaergTidsel,
	lavender,
	pepperMint,
	rosemary,
	sommerfugleBusk,
	thyme,
	winterSquash
} from '$lib/services/plant';
import type { Plant, PlantKind } from '$lib/types';

export class ExchangeClient {
	users: { userName: string; has: Plant[]; needs: PlantKind[] }[] = [
		{
			userName: 'cabang',
			has: [
				{
					plant: lavender,
					kind: 'seedling',
					comment: 'Skal selv klippes af'
				},
				{
					plant: sommerfugleBusk,
					kind: 'full-grown',
					comment: 'Skal selv graves op'
				},
				{
					plant: winterSquash,
					kind: 'seed'
				},
				{
					plant: dvaergTidsel,
					kind: 'full-grown'
				},
				{
					plant: rosemary,
					kind: 'seedling'
				}
			],
			needs: [thyme, pepperMint]
		},
		{
			userName: 'bob',
			has: [],
			needs: [lavender, winterSquash, sommerfugleBusk, rosemary, pepperMint, thyme]
		},
		{
			userName: 'alice',
			has: [
				{
					plant: dvaergTidsel,
					kind: 'full-grown'
				},
				{
					plant: sommerfugleBusk,
					kind: 'sapling'
				},
				{
					plant: rosemary,
					kind: 'seedling',
					comment: 'Skal selv klippe af'
				}
			],
			needs: [lavender, winterSquash]
		}
	];

	async get(username: string) {
		return this.users.find((user) => user.userName == username) || null;
	}

	async getAll() {
		return this.users;
	}
}
