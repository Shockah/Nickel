using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.IO.Compression;

namespace Nickel;

internal sealed class SaveManager
{
	private readonly ILogger Logger;

	public SaveManager(ILogger logger)
	{
		this.Logger = logger;

		StatePatches.OnLoad.Subscribe(this, this.OnLoad);
	}

	private void OnLoad(object? _, StatePatches.LoadEventArgs e)
	{
		if (!e.Data.isCorrupted)
			return;

		try
		{
			var savePath = State.GetSavePath(e.Slot);
			using var fileStream = File.OpenRead(Storage.SavePath(savePath));
			using var reader = new StreamReader(savePath.EndsWith(".gz") ? new GZipStream(fileStream, CompressionMode.Decompress) : fileStream);
			using var jsonReader = new JsonTextReader(reader);
			var token = JToken.ReadFrom(jsonReader);

			if (token is not JObject @object)
				return;

			this.TryResetToMenu(e, @object);
			if (!e.Data.isCorrupted)
				return;

			this.TryForceReset(e, @object);
		}
		catch
		{
		}
	}

	private void TryResetToMenu(StatePatches.LoadEventArgs e, JObject @object)
	{
		this.Logger.LogDebug("Attempting to recover save slot {Slot} by returning to menu...", e.Slot);

		try
		{
			var clone = (JObject)@object.DeepClone();
			clone[nameof(State.ship)] = JValue.CreateNull();
			clone[nameof(State.characters)] = new JArray();
			clone[nameof(State.artifacts)] = new JArray();
			clone[nameof(State.map)] = JValue.CreateNull();
			clone[nameof(State.route)] = JValue.CreateNull();
			clone[nameof(State.routeOverride)] = JValue.CreateNull();
			clone[nameof(State.pendingRunSummary)] = JValue.CreateNull();
			clone[nameof(State.rewardsQueue)] = new JArray();

			if (@object.GetValue(nameof(State.runConfig)) is JObject runConfig)
				runConfig[nameof(RunConfig.selectedChars)] = new JArray();

			using var jsonReader = new JTokenReader(clone);
			if (JSONSettings.serializer.Deserialize<State>(jsonReader) is not { } recoveredState)
				return;

			recoveredState.ship = Mutil.DeepCopy(StarterShip.ships["artemis"].ship);
			recoveredState.map = new MapFirst();
			recoveredState.route = new NewRunOptions();
			recoveredState.runConfig.selectedShip = "artemis";

			recoveredState.map.Populate(recoveredState, recoveredState.rngZone);

			e.Data.state = recoveredState;
			e.Data.isCorrupted = false;
			this.Logger.LogWarning("Recovered save slot {Slot} by returning to menu.", e.Slot);
		}
		catch
		{
		}
	}

	private void TryForceReset(StatePatches.LoadEventArgs e, JObject @object)
	{
		this.Logger.LogDebug("Attempting to force reset save slot {Slot} while keeping progress...", e.Slot);

		try
		{
			if (@object.GetValue(nameof(State.storyVars)) is not JObject storyVars)
				return;

			using var jsonReader = new JTokenReader(storyVars);
			if (JSONSettings.serializer.Deserialize<StoryVars>(jsonReader) is not { } recoveredStoryVars)
				return;

			var recoveredState = State.NewGame(slot: null);
			recoveredState.storyVars = recoveredStoryVars;
			recoveredState.route = new NewRunOptions();
			recoveredState.slot = e.Slot;

			e.Data.state = recoveredState;
			e.Data.isCorrupted = false;
			this.Logger.LogWarning("Recovered save slot {Slot} by force resetting while keeping progress.", e.Slot);
		}
		catch
		{
		}
	}
}