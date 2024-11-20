export interface Plant {
	name: string;
	imgUrl: string;
}
export interface Location {
	label: string;
	distanceKm: number;
}
export interface User {
	userName: string;
	location: Location;
	profileImg: string;
	fullName: string;
	firstName: string;
	// TODO add has type, i.e "seed", "sapling", "plant"
	has: Plant[];
	needs: Plant[];
}
