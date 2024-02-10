const GAME_ID = 'cobaltcore';
const STEAMAPP_ID = '2179850';

const path = require('path');
const { fs, log, util } = require('vortex-api');

function findGame() {
	return util.GameStoreHelper.findByAppId([STEAMAPP_ID]).then(game => game.gamePath);
}

function prepareForModding(discovery) {
	return fs.ensureDirWritableAsync(path.join(discovery.path, 'Nickel', 'ModLibrary'));
}

function testSupportedContent(files, gameId) {
	return Promise.resolve({
		supported: gameId === GAME_ID && files.find(file => path.basename(file) === 'nickel.json') !== undefined,
		requiredFiles: []
	});
}

function installContent(files) {
	const manifestFile = files.find(file => path.basename(file) === 'nickel.json');
	const idx = manifestFile.indexOf(path.basename(manifestFile));
	const rootPath = path.dirname(manifestFile);

	return Promise.resolve({
		instructions: files
			.filter(file => file.indexOf(rootPath) !== -1)
			.map(file => {
				return {
					type: 'copy',
					source: file,
					destination: path.join(file.substr(idx))
				}
			})
	});
}

function main(context) {
	context.registerGame({
		id: GAME_ID,
		name: 'Cobalt Core',
		mergeMods: false,
		queryPath: findGame,
		supportedTools: [],
		queryModPath: () => 'Nickel/ModLibrary',
		logo: 'gameart.jpg',
		executable: () => 'Nickel/NickelLauncher.exe',
		requiredFiles: [
			'Nickel/NickelLauncher.exe',
			'CobaltCore.exe',
		],
		setup: prepareForModding,
		environment: {
			SteamAPPId: STEAMAPP_ID,
		},
		details: {
			steamAppId: STEAMAPP_ID
		},
	});

	context.registerInstaller('cobaltcore-nickel', 25, testSupportedContent, installContent);
	
	return true;
}

module.exports = {
	default: main,
};