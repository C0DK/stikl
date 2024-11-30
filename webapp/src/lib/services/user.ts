// TODO: consider utilizing state
import type { Plant, PlantKind, Profile, User } from '../types';
import type { ExchangeClient } from '$lib/clients/exchange';
import type { Auth0Client } from '$lib/clients/auth0';

export interface PlantQueryResult {
	plant: Plant;
	owner: User;
}

export class UserService {
	authClient: Auth0Client;
	exchangeClient: ExchangeClient;

	constructor(authClient: Auth0Client, exchangeClient: ExchangeClient) {
		this.authClient = authClient;
		this.exchangeClient = exchangeClient;
	}

	async get(username: string): Promise<User> {
		const profile = await this.authClient.get(username);
		const exchangeData = await this.exchangeClient.get(username);

		return {
			...profile,
			has: exchangeData?.has || [],
			needs: exchangeData?.needs || []
		};
	}

	async getAll(): Promise<Profile[]> {
		return await this.authClient.getAll();
	}

	async getUsersWithPlant(kind: PlantKind): Promise<PlantQueryResult[]> {
		return Promise.all(
			(await this.exchangeClient.getAll()).flatMap((user) =>
				user.has.filter(ofKind(kind)).map(async (plant) => ({
					plant,
					owner: {
						...(await this.authClient.get(user.userName)),
						has: user.has,
						needs: user.needs
					}
				}))
			)
		);
	}
}

const ofKind = (kind: PlantKind) => (plant: Plant) => plant.plant == kind;

function joinUserProfile(profile: Profile, user: { has: Plant[]; needs: PlantKind[] }): User {
	return {
		...profile,
		has: user.has,
		needs: user.needs
	};
}
