// TODO: consider utilizing state
import type { Plant } from '../types';

export class PlantService {
	plants: Plant[] = [
		{
			name: 'Lavendel',
			imgUrl:
				'https://www.gardenia.net/wp-content/uploads/2015/02/Lavender-angustifolia-Hidcote.jpg'
		},
		{
			name: "Sommerfuglebusk, Buddleia dav. 'Black Knight'",
			imgUrl:
				'https://media.plantorama.dk/cdn/2SP93k/sommerfuglebusk-buddleia-dav-black-knight-5-liter-potte-sommerfuglebusk-buddleia-dav-black-knight-5-liter-potte.webp?d=14378'
		},
		{
			name: 'Peppermynte',
			imgUrl:
				'https://www.gardenia.net/wp-content/uploads/2023/05/mentha-piperita-peppermint-780x520.webp'
		},
		{
			name: 'Vinter squash',
			imgUrl:
				'https://www.gardenia.net/wp-content/uploads/2023/05/cucurbita-maxima-winter-squash-780x520.webp'
		},

		{
			name: 'Dværg Tidsel',
			imgUrl: 'https://www.gardenia.net/wp-content/uploads/2023/05/cirsium-acaule-780x520.webp'
		},
		{
			name: 'Rosemarin',
			imgUrl:
				'https://www.gardenia.net/wp-content/uploads/2023/05/rosmarinus-officinalis-arp-780x520.webp'
		},
		{
			name: 'Timian',
			imgUrl:
				'https://www.gardenia.net/wp-content/uploads/2023/05/thymus-serpyllum-creeping-thyme-780x520.webp'
		},
		{
			name: 'Dværg Tidsel',
			imgUrl: 'https://www.gardenia.net/wp-content/uploads/2023/05/cirsium-acaule-780x520.webp'
		},
		{
			name: 'Rosemarin',
			imgUrl:
				'https://www.gardenia.net/wp-content/uploads/2023/05/rosmarinus-officinalis-arp-780x520.webp'
		}
	];

	get(name: string) {
		return this.plants.find((plant) => plant.name == name);
	}

	search(query: string) {
		return this.plants.filter((plant) => plant.name.includes(query));
	}

	getAll() {
		return this.plants;
	}
}
