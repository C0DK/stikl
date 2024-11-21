import { error } from '@sveltejs/kit';
import { UserService } from '$lib/user';
import type { PageLoad } from './$types';

export const load: PageLoad = ({ params }) => {
	const service = new UserService();
	const user = service.get(params.id);
	if (user !== undefined) {
		return { user };
	}

	error(404, 'Not found');
};
