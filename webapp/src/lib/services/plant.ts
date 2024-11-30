import type { PlantKind } from '../types';

export const lavender: PlantKind = {
	id: 'lavender',
	name: 'Lavendel',
	imgUrl: 'https://www.gardenia.net/wp-content/uploads/2015/02/Lavender-angustifolia-Hidcote.jpg'
};
export const sommerfugleBusk: PlantKind = {
	id: 'sommerfugle-busk',
	name: "Sommerfuglebusk, Buddleia dav. 'Black Knight'",
	imgUrl:
		'https://media.plantorama.dk/cdn/2SP93k/sommerfuglebusk-buddleia-dav-black-knight-5-liter-potte-sommerfuglebusk-buddleia-dav-black-knight-5-liter-potte.webp?d=14378'
};
export const winterSquash: PlantKind = {
	id: 'vinter-squash',
	name: 'Vinter squash',
	imgUrl:
		'https://www.gardenia.net/wp-content/uploads/2023/05/cucurbita-maxima-winter-squash-780x520.webp'
};
export const pepperMint: PlantKind = {
	id: 'peppermynte',
	name: 'Peppermynte',
	imgUrl:
		'https://www.gardenia.net/wp-content/uploads/2023/05/mentha-piperita-peppermint-780x520.webp'
};
export const rosemary: PlantKind = {
	id: 'rosemary',
	name: 'Rosemarin',
	imgUrl:
		'https://www.gardenia.net/wp-content/uploads/2023/05/rosmarinus-officinalis-arp-780x520.webp'
};
export const thyme: PlantKind = {
	id: 'thyme',
	name: 'Timian',
	imgUrl:
		'https://www.gardenia.net/wp-content/uploads/2023/05/thymus-serpyllum-creeping-thyme-780x520.webp'
};
export const dvaergTidsel: PlantKind = {
	id: 'dværg-tidsel',
	name: 'Dværg Tidsel',
	imgUrl: 'https://www.gardenia.net/wp-content/uploads/2023/05/cirsium-acaule-780x520.webp'
};

export class PlantService {
	plants: PlantKind[] = [
		lavender,
		sommerfugleBusk,
		pepperMint,
		winterSquash,
		dvaergTidsel,
		rosemary,
		thyme
	];

	async get(id: string) {
		return this.plants.find((plant) => plant.id == id) || null;
	}

	async search(query: string) {
		return this.plants.filter((plant) => plant.name.toLowerCase().includes(query.toLowerCase()));
	}
}
