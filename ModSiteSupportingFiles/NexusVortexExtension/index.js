const GAME_ID = 'cobaltcore';
const STEAMAPP_ID = '2179850';

const path = require('path');
const { fs, log, util } = require('vortex-api');

async function findGame() {
	const game = await util.GameStoreHelper.findByAppId([STEAMAPP_ID]);
	return game.gamePath;
}

const NICKEL_URL = "https://www.nexusmods.com/cobaltcore/mods/1";

async function prepareForModding(discovery, context) {
	await fs.ensureDirWritableAsync(path.join(discovery.path, 'Nickel', 'ModLibrary'));
	const nickelLauncherPath = path.join(discovery.path, "Nickel", "NickelLauncher.exe");
	try {
		await fs.statAsync(nickelLauncherPath)
	} catch (e) {
		log("warn", "Caught error while looking for Nickel: (" + e.code + "): " + e.message);
		if(e.code === "ENOENT") {
			context.api.sendNotification({
				id: "nickel-missing",
				type: "warning",
				title: "Nickel is not installed",
				message: "The Nickel ModLoader is required to mod Cobalt Core.",
				actions: [
					{ title: "Get Nickel", action: () => util.opn(NICKEL_URL) }
				]
			})
		} else {
			throw e;
		}
	}
}

async function testSupportedContent(files, gameId) {
	return {
		supported: gameId === GAME_ID && files.find(file => path.basename(file) === 'nickel.json') !== undefined,
		requiredFiles: []
	};
}

async function installContent(files) {
	const manifestFile = files.find(file => path.basename(file) === 'nickel.json');
	const idx = manifestFile.indexOf(path.basename(manifestFile));
	const rootPath = path.dirname(manifestFile);

	return {
		instructions: files
			.filter(file => file.indexOf(rootPath) !== -1)
			.map(file => {
				return {
					type: 'copy',
					source: file,
					destination: path.join(file.substr(idx))
				}
			})
	};
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
			'CobaltCore.exe',
		],
		setup: (discovery) => prepareForModding(discovery, context),
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
