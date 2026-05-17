# Serialization

Both the game and Nickel utilize the [Newtonsoft.JSON](https://www.newtonsoft.com/json) library for serializing data (for example, save state persistence).

There are some techniques and features used by the game and mods that rely on serialization and require objects to be properly serializable:
* The game utilizes serialization to power the `Mutil.DeepCopy<T>` method, used all around the game's code. Nickel replaces this with a more performant implementation, but it still follows Newtonsoft's serialization rules.
* For example a mod could implement undoing actions, or simulating results, by leveraging serialization in specific ways.

## Serialization rules

By default, Newtonsoft serializes all `public` fields and properties.
* Marking a non-`public` field or property with `[JsonProperty]` will also serialize it.
* Marking a `public` field or property with `[JsonIgnore]` will exclude it from being serialized.

Static fields and properties are not serialized and are completely ignored by the save system. They are unsuitable for storing game state, since they do not persist across saves.

Similarly, storing game state on mod instances (or any objects not part of the serialized game state graph) will not persist and should not be used for gameplay-relevant data.

Whenever you add a field or property to a game state object, you should ask whether it affects game state. If it does, it **must** be serialized.

At the same time, if a field or property only affects visual effects or is just a cache, it should not be serialized to avoid bloating save data or introducing unstable values.

> [!WARNING]
> Some types are not serializable at all. Common types that can't be serialized are [`Type`](https://learn.microsoft.com/en-us/dotnet/api/system.type), as well as all [`Delegate`](https://learn.microsoft.com/en-us/dotnet/api/system.delegate) objects (like [`Action`](https://learn.microsoft.com/en-us/dotnet/api/system.action) and [`Func<TResult>`](https://learn.microsoft.com/en-us/dotnet/api/system.func-1)). Attempting to serialize such values may cause errors and render the serialized data unusable.

### JSON serialization system

Nickel and the game use **Newtonsoft.Json** as the only supported JSON serialization system for save data.

Other serializers (such as `System.Text.Json`) are not used by the game and will not participate in save/load behavior. Using them for game state data will result in data not being saved or restored correctly.

Make sure you are using the correct namespace:

```diff
- using System.Text.Json; // this won't work
+ using Newtonsoft.Json; // correct!
```

## Storing additional data

Sometimes mods need to associate extra information with game state objects (for example, tracking per-card state, or storing custom effects across saves).

This must be done using the [Mod data](mod-helper/mod-data.md) system.

The important rule is:

* Only data stored inside the game state graph is persisted.
* Everything outside that graph (including mod instances, static fields, or other runtime objects) is not persisted.

Mod instances themselves are runtime objects created by Nickel. They are not part of the game's save data and should not be used to store gameplay-relevant state.

If data needs to survive saving and loading, it must be attached to serialized game state objects via the Mod data system.

## Identity and object references (`uuid`)

Some game objects use an `int uuid` field to represent identity (for example `Card`s and other gameplay entities).

Each `uuid` identifies exactly one active instance of that object in the game state.

### Why this exists

Serialization is not always performed on the full game state graph (for example during deep copy operations). In these cases, direct object references can become ambiguous or duplicated.

The `uuid` system avoids this by separating:
* Identity (the `uuid`)
* Storage (the object instance in the game state graph)

### Rules for modders

When working with `uuid`-based objects:
* Store `uuid` instead of object references in mod data.
* Treat `uuid` as the primary identifier.
* Do not store multiple persistent references to the same `uuid`-based object in different systems unless that system explicitly owns the reference.

For example:
* If a `Card` exists in `Combat.hand`, other systems should store its `uuid`, not a second reference to the card object

Only store direct object references when the object is guaranteed to exist in exactly one place in the game state.

## Serialization pitfalls

While Newtonsoft is generally very neat to work with, sometimes it does come with its own problems.

### Collections with non-empty default state

By default, deserialization merges with the existing collection instead of replacing it, meaning previously removed default elements may reappear after loading. This can be overridden using the [`ObjectCreationHandling`](https://www.newtonsoft.com/json/help/html/P_Newtonsoft_Json_JsonPropertyAttribute_ObjectCreationHandling.htm) property on [`JsonProperty`](https://www.newtonsoft.com/json/help/html/T_Newtonsoft_Json_JsonPropertyAttribute.htm).

### [Stack&lt;T&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.stack-1)

Serializing a `Stack<T>` reverses its element order when deserialized. This is due to Newtonsoft treating it like a `List<T>` without preserving stack semantics.

This makes `Stack<T>` unsuitable for serialized game state. Use a `List<T>` instead, and treat it as a stack in code if needed.

### Mod-owned reference graphs

Mods may define their own recursive or graph-like data structures (for example trees, linked lists, or dependency graphs). This is fine as long as the structure remains fully contained within mod-owned data.

Problems arise when mod data starts mixing with the game state graph.

Avoid:
* Storing direct references to game state objects inside mod-owned graphs
* Building recursive mod structures that include `uuid`-based objects in multiple branches
* Creating bidirectional links between mod data and game state objects

Instead:
* Store `uuid` values when referencing game state objects
* Reconstruct relationships at runtime when needed
* Keep recursive structures strictly within mod data, or strictly within game state data — not both

The key rule is:

> Do not build a single combined object graph across mod data and game state.

# Applying serializer settings

If you need to customize serializer settings, Nickel lets you do so via your mod's [mod helper instance [TODO]](TODO):
```
helper.Storage.ApplyJsonSerializerSettings([...]);
helper.Storage.ApplyGlobalJsonSerializerSettings([...]);
```

For more information, see the [Mod helper storage [TODO]](TODO) page.