import { Auth0Client, createAuth0Client, User as Auth0User } from '@auth0/auth0-spa-js';
import type { Position, User } from '$lib/types';
import { error } from '@sveltejs/kit';
import { getDistanceInKm } from '$lib/utils/distance';

export class AuthService {
	currentUser: User | null | undefined = $state(undefined);
	private client?: Auth0Client = $state(undefined);
	isInitialized = $derived(this.client !== undefined);

	async initClient(domain: string, clientId: string) {
		this.client = await createAuth0Client({
			domain,
			clientId
		});
	}

	async updateUser() {
		this.currentUser = await this.fetchUser();
	}

	// TODO derived?
	distanceTo(position: Position) {
		if (!this.currentUser) return undefined;

		return getDistanceInKm(this.currentUser.position, position);
	}

	async fetchUser(): Promise<User | null> {
		this.assertIsInitialized();
		const auth0User = await this.client!.getUser();

		console.log(auth0User);
		if (auth0User) {
			return {
				userName: getUsername(auth0User),
				// TODO: get position.
				position: {
					latitude: 0,
					longitude: 0,
					label: 'Aalborg'
				},
				profileImg: auth0User.picture || null,
				fullName: getFullName(auth0User),
				firstName: auth0User.name || 'N/A',
				has: [],
				needs: []
			};
		}
		return null;
	}

	async loginWithPopup() {
		this.assertIsInitialized();

		try {
			await this.client!.loginWithPopup();

			this.updateUser();
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

function getFullName(user: Auth0User): string {
	// TODO: handle undefined?
	return `${user.name} ${user.family_name}`;
}

function getUsername(user: Auth0User): string {
	if (!user.sub) {
		error(400, 'Auth0 user does not have expected subject');
	}
	const splitSub = user.sub.split('|');
	if (splitSub.length != 2) {
		error(400, `Auth0 subject should be of format \`auth0|id\` but was ${sub}`);
	}

	return splitSub[1];
}

export default new AuthService();
