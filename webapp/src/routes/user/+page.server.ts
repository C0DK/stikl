import { PUBLIC_AUTH0_DOMAIN } from '$env/static/public';
import { AUTH0_CLIENT_ID, AUTH0_CLIENT_SECRET } from '$env/static/private';
import type { PageServerLoad } from './$types';
import { Auth0client } from '$lib/services/auth0client';

export const load: PageServerLoad = async () => {
	const client = new Auth0client(PUBLIC_AUTH0_DOMAIN, AUTH0_CLIENT_ID, AUTH0_CLIENT_SECRET);

	return { users: await client.getUsers() };
};
