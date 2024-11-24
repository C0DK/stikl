import type { Distance, Position } from '$lib/types';

export function getDistanceInKm(from: Position, to: Position): Distance {
	const R = 6371; // Radius of the earth in km
	const deltaLat = deg2rad(to.latitude - from.latitude);
	const deltaLon = deg2rad(to.longitude - from.longitude);
	const a =
		Math.sin(deltaLat / 2) * Math.sin(deltaLat / 2) +
		Math.cos(deg2rad(from.latitude)) *
			Math.cos(deg2rad(to.latitude)) *
			Math.sin(deltaLon / 2) *
			Math.sin(deltaLon / 2);
	const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));

	return { amount: R * c, unit: 'km' }; // Distance in km
}

function deg2rad(deg: number): number {
	return deg * (Math.PI / 180);
}
