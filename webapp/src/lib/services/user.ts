// TODO: consider utilizing state
import type { Distance, Plant, PlantKind, Position, User } from '../types';
import {
	dvaergTidsel,
	lavender,
	pepperMint,
	rosemary,
	sommerfugleBusk,
	thyme,
	winterSquash
} from '$lib/services/plant';
import { getDistanceInKm } from '$lib/utils/distance';

export interface PlantQueryResult {
	plant: Plant;
	owner: User;
}

export class UserService {
	users: User[] = [
		{
			userName: 'cabang',
			firstName: 'Casper',
			profileImg: 'https://images.gr-assets.com/users/1639240435p8/129022892.jpg',
			fullName: 'Casper Bang',
			position: {
				label: 'Aalborg, Nordjylland',
				latitude: 57.0421,
				longitude: 9.9145
			},
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
			userName: 'alice',
			firstName: 'Alice',
			fullName: 'Alice Bobish',
			profileImg:
				'https://s.gr-assets.com/assets/nophoto/user/m_225x300-d890464beadb13e578061584eaaaa1dd.png',
			position: {
				label: 'Viby J, Midtjylland',
				latitude: 56.1247663,
				longitude: 10.1249256
			},
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

	getSelf() {
		// TODO should not return nil.
		return this.get('cabang');
	}

	get(username: string) {
		return this.users.find((user) => user.userName == username) || null;
	}

	// TODO which service should contain these functions?
	getPlantsWithin(position: Position, maxDistanceKM: Distance): PlantQueryResult[] {
		// TODO assume same unit
		return this.users.flatMap((user) =>
			getDistanceInKm(user.position, position).amount < maxDistanceKM.amount
				? user.has.map((plant) => ({
						plant,
						owner: user
					}))
				: []
		);
	}

	getUsersWithPlant(kind: PlantKind): PlantQueryResult[] {
		return this.users.flatMap((user) =>
			user.has
				.filter((plant) => plant.plant == kind)
				.map((plant) => ({
					plant,
					owner: user
				}))
		);
	}

	getUsers() {
		return this.users;
	}
}
