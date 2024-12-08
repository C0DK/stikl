import { error } from '@sveltejs/kit';
import { UserService } from '$lib/services/user';
import type { PageServerLoad } from './$types';
// TODO: dry the auth0client
import { PUBLIC_AUTH0_DOMAIN } from '$env/static/public';
import { AUTH0_CLIENT_ID, AUTH0_CLIENT_SECRET } from '$env/static/private';
import { StiklClient } from '$lib/clients/stikl';
import { Auth0Client } from '$lib/clients/auth0';

export const load: PageServerLoad = async ({ params }) => {
	const auth0Client = new Auth0Client(PUBLIC_AUTH0_DOMAIN, AUTH0_CLIENT_ID, AUTH0_CLIENT_SECRET);
	const service = new UserService(auth0Client, new StiklClient());
	const user = await service.get(params.id);
	if (user !== null) {
		return { user };
	}

	error(404, 'Not found');
};
