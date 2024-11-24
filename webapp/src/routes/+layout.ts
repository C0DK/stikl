import type { LayoutLoad } from './$types';
import { UserService } from '$lib/services/user';
import type { Position } from '$lib/types';
import { getDistanceInKm } from '$lib/utils/distance';

export const load: LayoutLoad = async () => {
	const service = new UserService();
	// TODO: dont hardcode own username
	const me = service.get('cabang');
	return {
		my:
			me != null
				? {
						distanceTo: (position: Position) => getDistanceInKm(me.position, position),
						...me
					}
				: null
		//
	};
};
