import { type GetUsers200ResponseOneOfInner, ManagementClient } from 'auth0';
import { User } from '@auth0/auth0-spa-js';
import type { Profile } from '$lib/types';
import { error } from '@sveltejs/kit';

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
		userName: getUsername(user),
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

function getUsername(user: User | GetUsers200ResponseOneOfInner): string {
	if (!user.user_id) {
		error(400, `Auth0 user does not have a user_id`);
	}
	const splitSub = user.user_id.split('|');
	if (splitSub.length != 2) {
		error(400, `Auth0 subject should be of format \`auth0|id\` but was ${user.user_id}`);
	}

	return splitSub[1];
}
