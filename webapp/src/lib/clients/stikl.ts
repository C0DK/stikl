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
import { error } from '@sveltejs/kit';

interface UserDto {
	id: string;
	has: Plant[];
	needs: PlantKind[];
}

export interface PlantQueryResult {
	plant: Plant;
	owner: string;
}

interface IStiklClient {
	getUser(username: string): Promise<UserDto | null>;

	getUsersWithPlant(plantId: string): Promise<PlantQueryResult[]>;

	getPlant(plantId: string): Promise<PlantKind | null>;
}

// TODO move this around and improve structure
export class InMemoryStiklClient implements IStiklClient {
	users: UserDto[] = [
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

	plants: PlantKind[] = [
		lavender,
		sommerfugleBusk,
		pepperMint,
		winterSquash,
		dvaergTidsel,
		rosemary,
		thyme
	];

	async getUser(username: string) {
		return this.users.find((user) => user.userName == username) || null;
	}

	async getUsersWithPlant(plantId: string) {
		return this.users.flatMap((user) =>
			user.has
				.filter((plant) => plant.plant.id == plantId)
				.map((plant): PlantQueryResult => ({ plant, owner: user.userName }))
		);
	}

	async getPlant(plantId: string) {
		return this.plants.find((plant) => plant.id == plantId) || null;
	}
}

export class StiklApiClient implements IStiklClient {
	baseUrl = 'http://localhost:5213';

	async getUser(username: string): Promise<UserDto | null> {
		const response = await this.get(`User/${username}`);

		if (response.status == 404) return null;

		const user: UserDto = await response.json();
		if (user != null && user.userName == null) {
			// TODO: correct the DTO either here on API!
			console.log(user);
			error(500, 'Invalid user returned!');
		}
		return user;
	}

	async getPlant(plantId: string) {
		return await this.get(`Plant/${plantId}`).then((response) => {
			if (response.status == 404) return null;

			return response.json();
		});
	}

	async getUsersWithPlant(plantId: string): Promise<PlantQueryResult[]> {
		const res = await this.get(`Plant/${plantId}/seeders`).then((response) => {
			if (response.status != 200) error(500, 'Could not get seeders!');

			return response.json();
		});
		if (res == null) {
			errorInvalidResponse();
		}
		return res;
	}

	private async get(url: string) {
		url = `${this.baseUrl}/${url}`;
		console.log(`GET ${url}`);
		const response = await fetch(url, { method: 'get' });
		if (response.status != 200 && response.status != 404) {
			error(500, `Invalid response from api (${response})`);
		}
		return response;
	}
}

function errorInvalidResponse() {
	error(500, 'API dumb');
}

// TODO: do DI and composition.
export { StiklApiClient as StiklClient };
