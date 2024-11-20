// TODO: consider utilizing state
import type { User } from './types';
export class UserService {
	users: User[] = [
		{
			userName: 'cabang',
			firstName: 'Casper',
			profileImg: 'https://images.gr-assets.com/users/1639240435p8/129022892.jpg',
			fullName: 'Casper Bang',
			location: {
				label: 'Aalborg, Nordjylland',
				distanceKm: 3
			},
			has: [
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
				}
			],
			needs: [
				{
					name: 'Timian',
					imgUrl:
						'https://www.gardenia.net/wp-content/uploads/2023/05/thymus-serpyllum-creeping-thyme-780x520.webp'
				}
			]
		},
		{
			userName: 'alice',
			firstName: 'Alice',
			fullName: 'Alice Bobish',
			profileImg: 'https://s.gr-assets.com/assets/nophoto/user/m_225x300-d890464beadb13e578061584eaaaa1dd.png',
			location: {
				label: 'Viby J, Midtjylland',
				distanceKm: 125
			},
			has: [
				{
					name: 'Dværg Tidsel',
					imgUrl: 'https://www.gardenia.net/wp-content/uploads/2023/05/cirsium-acaule-780x520.webp'
				},
				{
					name: 'Rosemarin',
					imgUrl:
						'https://www.gardenia.net/wp-content/uploads/2023/05/rosmarinus-officinalis-arp-780x520.webp'
				}
			],
			needs: [
				{
					name: 'Lavendel',
					imgUrl:
						'https://www.gardenia.net/wp-content/uploads/2015/02/Lavender-angustifolia-Hidcote.jpg'
				}
			]
		}
	];

	get(username: string) {
		return this.users.find((user) => user.userName == username);
	}

	getUsers() {
		return this.users;
	}
}
