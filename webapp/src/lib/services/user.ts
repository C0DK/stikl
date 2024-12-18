// TODO: consider utilizing state
import type { Plant, PlantKind, Profile, User } from '../types';
import type { StiklClient } from '$lib/clients/stikl';
import type { Auth0Client } from '$lib/clients/auth0';

export interface PlantQueryResult {
	plant: Plant;
	owner: Profile;
}

export class UserService {
	authClient: Auth0Client;
	stiklClient: StiklClient;

	constructor(authClient: Auth0Client, exchangeClient: StiklClient) {
		this.authClient = authClient;
		this.stiklClient = exchangeClient;
	}

	async get(username: string): Promise<User> {
		const profile = await this.authClient.get(username);
		const stiklData = await this.stiklClient.getUser(username);

		if (stiklData == null) {
			console.warn(`We have no data on user ${username}!`);
		}

		return {
			...profile,
			has: stiklData?.has || [],
			needs: stiklData?.needs || []
		};
	}

	async getAll(): Promise<Profile[]> {
		return await this.authClient.getAll();
	}

	async getUsersWithPlant(kind: PlantKind): Promise<PlantQueryResult[]> {
		const result = await this.stiklClient.getUsersWithPlant(kind.id);

		return await Promise.all(
			result.map(async (dto) => ({
				plant: dto.plant,
				owner: await this.authClient.get(dto.owner)
			}))
		);
	}
}
