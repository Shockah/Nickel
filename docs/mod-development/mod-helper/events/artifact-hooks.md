# Artifact hooks

**Artifact hooks** are global callbacks that participate in the same event methods used by normal artifacts. For example, if your mod needs to run some code each combat, you may hook the `OnCombatStart` method.

Artifact hooks are accessed via your mod's [mod helper instance [TODO]](TODO):
```
helper.Events.RegisterBeforeArtifactsHook([...])
helper.Events.RegisterAfterArtifactsHook([...])
```

* Hooks registered with `RegisterBeforeArtifactsHook` run before any artifacts receive the event.
* Hooks registered with `RegisterAfterArtifactsHook` run after all artifacts receive the event.

To register a hook, you need to decide if you need your code to run before or after all other artifacts, tell Nickel which artifact method you want to participate in, give it the code to run, and optionally specify the priority between all other hooks. Priority only affects ordering between hooks registered in the same phase. Higher priority hooks run earlier. Hook priority defaults to `0`. The easiest way to specify the method is to just provide its name. Using `nameof` is recommended, as it allows the compiler to verify the hooked method name at build time. For example:
```cs
helper.Events.RegisterAfterArtifactsHook(nameof(Artifact.OnTurnStart), (State state, Combat combat) =>
{
	combat.QueueImmediate(new AAddCard { card = new BlockShot() });
}, priority: -1000);
```

The code to run is provided as a [lambda](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/lambda-expressions). Lambda parameters are matched to the hooked artifact method by parameter name, not position. Parameters you do not need may be omitted entirely.

```cs
helper.Events.RegisterAfterArtifactsHook(nameof(Artifact.OnTurnStart), (Combat combat) =>
{
	// valid even though `State state` was omitted
});
```

## Return values

Artifact hooks participate in return value aggregation exactly like normal artifacts.

This means different artifact methods use different aggregation rules:
* `void` methods simply execute all hooks.
* `int` return values are summed across all hooks and artifacts.
* Reference return values (such as `StuffBase?`) use the first non-`null` result. Once a non-`null` value is returned, later hooks and artifacts are ignored.

For methods with return values, hook ordering can affect the final result in the same way artifact ordering can.