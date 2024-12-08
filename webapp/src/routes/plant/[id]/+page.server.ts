import { error } from '@sveltejs/kit';
import { UserService } from '$lib/services/user';
import type { PageServerLoad } from './$types';
import { PlantService } from '$lib/services/plant';
import { Auth0Client } from '$lib/clients/auth0';
// TODO: dry the auth0client
import { PUBLIC_AUTH0_DOMAIN } from '$env/static/public';
import { AUTH0_CLIENT_ID, AUTH0_CLIENT_SECRET } from '$env/static/private';
import { StiklClient } from '$lib/clients/stikl';

export const load: PageServerLoad = async ({ params }) => {
	const auth0Client = new Auth0Client(PUBLIC_AUTH0_DOMAIN, AUTH0_CLIENT_ID, AUTH0_CLIENT_SECRET);
	const userService = new UserService(auth0Client, new StiklClient());
	const plantService = new PlantService();

	const kind = await plantService.get(params.id);
	if (kind !== null) {
		return { kind, plants: await userService.getUsersWithPlant(kind) };
	}

	error(404, 'Not found');
};
