import { error } from '@sveltejs/kit';
import { UserService } from '$lib/services/user';
import type { PageServerLoad } from './$types';
import { PlantService } from '$lib/services/plant';

export const load: PageServerLoad = ({ params }) => {
	const userService = new UserService();
	const plantService = new PlantService();

	const kind = plantService.get(params.id);
	if (kind !== null) {
		return { kind, plants: userService.getUsersWithPlant(kind) };
	}

	error(404, 'Not found');
};
