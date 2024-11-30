import { PlantService } from '$lib/services/plant';
import type { PageServerLoad } from './$types';

export const load: PageServerLoad = async ({ url }) => {
	const query = url.searchParams.get('q') || '';
	const service = new PlantService();

	return { plants: await service.search(query), query };
};
