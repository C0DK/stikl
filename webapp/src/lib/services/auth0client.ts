import { type GetUsers200ResponseOneOfInner, ManagementClient } from 'auth0';
import { User } from '@auth0/auth0-spa-js';
import type { Profile } from '$lib/types';

export class Auth0client {
	client: ManagementClient;

	constructor(domain: string, clientId: string, clientSecret: string) {
		this.client = new ManagementClient({
			domain: domain,
			clientId: clientId,
			clientSecret: clientSecret
		});
	}

	async getUsers() {
		const response = await this.client.users.getAll();

		return response.data.map(mapUser);
	}
}

export function mapUser(user: User | GetUsers200ResponseOneOfInner): Profile {
	return {
		userName: user.username,
		// TODO: get position.
		position: {
			latitude: 0,
			longitude: 0,
			label: 'Aalborg'
		},
		profileImg: user.picture || null,
		fullName: getFullName(user),
		firstName: user.given_name || 'N/A'
	};
}

function getFullName(user: User | GetUsers200ResponseOneOfInner): string {
	// TODO: handle undefined?
	return `${user.name}`;
}
