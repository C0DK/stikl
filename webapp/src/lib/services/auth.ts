import { Auth0Client, createAuth0Client } from '@auth0/auth0-spa-js';
import type { User } from '$lib/types';

export class AuthService {
	private client: Auth0Client;

	constructor(client: Auth0Client) {
		this.client = client;
	}

	static async create(domain: string, clientId: string) {
		return new AuthService(
			await createAuth0Client({
				domain,
				clientId
			})
		);
	}

	async getUser(): Promise<User | null> {
		const user = await this.client.getUser();

		console.log(user);
		if (user) {
			return {
				// TODO: is this best for username?
				userName: user.sub || user.preferred_username,
				position: {
					latitude: 0,
					longitude: 0,
					label: 'Aalborg'
				},
				profileImg: user.picture,
				fullName: `${user.name} ${user.family_name}`,
				// TODO: handle undefined?
				firstName: user.name,
				has: [],
				needs: []
			};
		}
		return null;
	}

	async loginWithPopup() {
		try {
			await this.client.loginWithPopup();

			return await this.getUser();
		} catch (e) {
			// TODO: better error
			console.error(e);
			return null;
		}
	}

	logout() {
		return this.client.logout();
	}
}
