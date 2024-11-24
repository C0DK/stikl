import { PlantService } from '$lib/services/plant';
import type { PageServerLoad } from './$types';

export const load: PageServerLoad = ({ url }) => {
	const query = url.searchParams.get('q') || '';
	const service = new PlantService();

	return { plants: service.search(query), query };
};
