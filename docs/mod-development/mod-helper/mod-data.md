# Mod data

**Mod data** is a tool provided by Nickel, solving several common problems in modding:
1. Storing new arbitrary properties on game state objects.
2. Tying data to lifecycles of game state objects.
3. Persisting data in save files.
4. Handling copying data to clones of game state objects (`Mutil.DeepCopy<T>`).

Game state objects include most objects used by the game, such as `Card`, `CardAction`, `State`, `Combat`, `Artifact`, `StuffBase` and many more. As long as it's a `class`, it can store mod data. There are some exceptions where Nickel allows mod data on `struct`s too - notably `CardData` - but other `struct`s are generally unsupported.

Mod data automatically disappears when the object it is attached to is no longer referenced by the game. For example, mod data attached to a `Card` is discarded when that `Card` is removed permanently, unless you still hold a reference to that `Card` in your mod. This means mods generally do not need to manually clean up mod data. It also enables some automatic lifecycle-based behavior. For example, values attached to a `Combat` instance naturally reset every fight, since each fight creates a new `Combat` instance.

Mod data is kept separate between mods. Modders are recommended to expose methods for accessing their own mod data through their [mods' APIs [TODO]](TODO) when appropriate. Alternatively, [you can access another mod's helper [TODO]](TODO).

Mod data participates in `Mutil.DeepCopy<T>`. When a game state object is cloned, its mod data is cloned as well. Mutable objects stored as mod data are deep-copied. Because of this, mod data should generally contain properly serializable data structures.

> [!CAUTION]
> While technically Nickel lets you store data of any type as mod data, it is advised to only ever store data that is properly serializable (see the [Serialization](../serialization.md) page). If any non-serializable mod data is present, it may render the save file unusable, making the player lose data.

# Accessing mod data

The main way to access mod data is via your mod's [mod helper](../mod-helper.md) instance:
```
helper.ModData.[...]
```

All mod data methods operate on a `string` key. These keys are scoped to your mod automatically. Two different mods can both use the same key without conflicts. Keys should generally describe the meaning of the stored value rather than its type.

## Examples

Storing data on a `Card` instance:
```cs
helper.ModData.Set(card, "MyNumber", 123);
helper.ModData.Set(card, "MyList", new List<int>());
```

Getting previously stored data:
```cs
// safe retrieval; `number` is assigned only if this returns `true`
if (helper.ModData.TryGet<int>(card, "MyNumber", out var number))
{
    // do something with `number`
}
```

```cs
// safe retrieval, will return a default value if the data isn't there
var number = helper.ModData.GetOrDefault<int>(card, "MyNumber");

// can also specify the default value
number = helper.ModData.GetOrDefault<int>(card, "MyNumber", 42);
```

```cs
// only check if the data exists
if (helper.ModData.Contains(card, "MyNumber"))
{
    // do something
}

// unsafe retrieval, will throw if the data isn't there!
var number = helper.ModData.Get<int>(card, "MyNumber");
```

`Obtain` is useful for mutable data structures such as lists, dictionaries, or sets, where you want to initialize the value only once:
```cs
// safe retrieval, will set the data to a default value or a newly constructed value if the data isn't there
var list = helper.ModData.Obtain<List<int>>(card, "MyList");

// can also specify how the new value should be constructed
list = helper.ModData.Obtain<List<int>>(card, "MyList", key => [1, 2, 3]);
```

Removing no longer needed data:
```cs
helper.ModData.Remove(card, "MyNumber");
```

Working with optional values:
```cs
bool contains;
int? optionalNumber;

contains = helper.ModData.Contains(card, "MyOptionalNumber"); // false
optionalNumber = helper.ModData.GetOptional<int>(card, "MyOptionalNumber"); // null

optionalNumber = 123;
helper.ModData.SetOptional(card, "MyOptionalNumber", optionalNumber);

contains = helper.ModData.Contains(card, "MyOptionalNumber"); // true
optionalNumber = helper.ModData.GetOptional<int>(card, "MyOptionalNumber"); // 123

optionalNumber = null;
helper.ModData.SetOptional(card, "MyOptionalNumber", optionalNumber);

contains = helper.ModData.Contains(card, "MyOptionalNumber"); // false
optionalNumber = helper.ModData.GetOptional<int>(card, "MyOptionalNumber"); // null
```

## Type safety

Using different types for the same property key can lead to runtime exceptions. Nickel will attempt to convert stored values to the requested type when possible.

```cs
helper.ModData.Set(card, "Value", "example text");
var heldCard = helper.ModData.Get<Card>(card, "Value"); // this throws, cannot convert `string` to `Card`!
```

## Mod extensions NuGet package

An alternative way of accessing mod data is through the [`Nickel.ModExtensions` NuGet package [TODO]](TODO). It contains extension methods for most of the game state objects you may want to use mod data on. The below code assumes you have already [configured the package properly for your mod [TODO]](TODO).

```cs
card.ModData().Set("MyNumber", 123);
```