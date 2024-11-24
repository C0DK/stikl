import { error } from '@sveltejs/kit';
import { UserService } from '$lib/services/user';
import type { PageLoad } from './$types';

export const load: PageLoad = ({ params }) => {
	const service = new UserService();
	const user = service.get(params.id);
	if (user !== null) {
		return { user };
	}

	error(404, 'Not found');
};
