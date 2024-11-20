// TODO: consider utilizing state
export class UserService {
	users = [{
		username: 'cabang',
		name: 'Casper Bang',
		needs: [
			{
				name: 'Timian'
			}
		],
		has: [
			{
				name: 'Lavendel'
			}
		]
	}];

	get(username: string) {
		return this.users.find((user) => user.username == username);
	}

	getUsers() {
		return this.users;
	}
}
