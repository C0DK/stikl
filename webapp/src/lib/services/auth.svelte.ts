import { Auth0Client, createAuth0Client } from '@auth0/auth0-spa-js';
import type { Position, Profile } from '$lib/types';
import { error } from '@sveltejs/kit';
import { getDistanceInKm } from '$lib/utils/distance';
import { mapUser } from '$lib/clients/auth0';

export class AuthService {
	currentUser: Profile | null | undefined = $state(undefined);
	distanceTo = $derived(
		(position: Position) =>
			(this.currentUser && getDistanceInKm(this.currentUser.position, position)) || undefined
	);
	private client?: Auth0Client = $state(undefined);
	isInitialized = $derived(this.client !== undefined);

	async initClient(domain: string, clientId: string) {
		this.client = await createAuth0Client({
			domain,
			clientId
		});
	}

	async updateUser() {
		// TODO figure out how to refresh token if not exist.
		await this.client!.isAuthenticated();
		this.currentUser = await this.fetchUser();
	}

	async fetchUser(): Promise<Profile | null> {
		this.assertIsInitialized();
		const user = await this.client!.getUser();

		return (user && mapUser(user)) || null;
	}

	async loginWithPopup() {
		this.assertIsInitialized();

		try {
			await this.client!.loginWithPopup();

			await this.updateUser();
		} catch (e) {
			// TODO: better error
			console.error(e);
		}
	}

	async logout() {
		if (!this.client) error(400, 'Auth Client not initialized!');

		await this.client.logout();

		this.currentUser = null;
	}

	private assertIsInitialized() {
		if (!this.isInitialized) error(400, 'Auth Client not initialized!');
	}
}

export default new AuthService();
