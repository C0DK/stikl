import type { PageServerLoad } from './$types';
// TODO: dry the auth0client
import { PUBLIC_AUTH0_DOMAIN } from '$env/static/public';
import { AUTH0_CLIENT_ID, AUTH0_CLIENT_SECRET } from '$env/static/private';
import { StiklClient } from '$lib/clients/stikl';
import { Auth0Client } from '$lib/clients/auth0';
import { UserService } from '$lib/services/user';

export const load: PageServerLoad = async () => {
	const auth0Client = new Auth0Client(PUBLIC_AUTH0_DOMAIN, AUTH0_CLIENT_ID, AUTH0_CLIENT_SECRET);
	const service = new UserService(auth0Client, new StiklClient());

	return { users: await service.getAll() };
};
