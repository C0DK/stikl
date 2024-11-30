import { type GetUsers200ResponseOneOfInner, ManagementClient } from 'auth0';
import { User } from '@auth0/auth0-spa-js';
import type { Profile } from '$lib/types';
import { error } from '@sveltejs/kit';

export class Auth0Client {
	client: ManagementClient;

	constructor(domain: string, clientId: string, clientSecret: string) {
		this.client = new ManagementClient({
			domain: domain,
			clientId: clientId,
			clientSecret: clientSecret
		});
	}

	async get(username: string) {
		let response;
		try {
			response = await this.client.users.getAll({ q: `username:${username}` });
		} catch (e) {
			console.error(e);
			error(500, 'Auth0 connecton failed!');
		}

		if (response.data.length > 1) error(500, 'More than one user with that username!');
		if (response.data.length == 0) error(404, `Could not find a user with ${username}!`);

		return mapUser(response.data[0]);
	}

	async getAll() {
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
		firstName: user.given_name || user.username
	};
}

function getFullName(user: User | GetUsers200ResponseOneOfInner): string {
	return user.name || user.username;
}
