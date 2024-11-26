export interface PlantKind {
	id: string;
	name: string;
	imgUrl: string;
}

export interface Position {
	label: string;
	longitude: number;
	latitude: number;
}

export interface Plant {
	plant: PlantKind;
	kind: 'seed' | 'seedling' | 'sapling' | 'full-grown';
	comment?: string;
}

export interface Distance {
	amount: number;
	unit: 'km';
}

export interface User {
	userName: string;
	position: Position;
	profileImg: string | null;
	fullName: string;
	firstName: string;
	has: Plant[];
	needs: PlantKind[];
}
