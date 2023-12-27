using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using CobaltCoreModding.Definitions.ExternalItems;

namespace Nickel;

internal sealed class LegacyDatabase
{
    private Func<ContentManager> ContentManagerProvider { get; init; }

    private Dictionary<string, ExternalSprite> GlobalNameToSprite { get; init; } = new();
    private Dictionary<string, ExternalDeck> GlobalNameToDeck { get; init; } = new();
    private Dictionary<string, ExternalStatus> GlobalNameToStatus { get; init; } = new();
    private Dictionary<string, ExternalCard> GlobalNameToCard { get; init; } = new();
    private Dictionary<string, ExternalArtifact> GlobalNameToArtifact { get; init; } = new();

    public LegacyDatabase(Func<ContentManager> contentManagerProvider)
    {
        this.ContentManagerProvider = contentManagerProvider;
    }

    internal void InjectLocalizations(string locale, Dictionary<string, string> localizations)
    {
        foreach (var status in this.GlobalNameToStatus.Values)
        {
            string key = $"{status.Id!.Value}";
            status.GetLocalisation(locale, out string? name, out string? description);
            if (name is not null)
                localizations[$"status.{key}.name"] = name;
            if (description is not null)
                localizations[$"status.{key}.desc"] = description;
        }
        foreach (var card in this.GlobalNameToCard.Values)
        {
            string key = card.CardType.Name; // TODO: change this when Card.Key gets patched
            card.GetLocalisation(locale, out string? name, out string? description, out string? descriptionA, out string? descriptionB);
            if (name is not null)
                localizations[$"card.{key}.name"] = name;
            if (description is not null)
                localizations[$"card.{key}.desc"] = description;
            if (descriptionA is not null)
                localizations[$"card.{key}.descA"] = descriptionA;
            if (descriptionB is not null)
                localizations[$"card.{key}.descB"] = descriptionB;
        }
        foreach (var artifact in this.GlobalNameToArtifact.Values)
        {
            if (!artifact.GetLocalisation(locale, out string name, out string description))
                continue;
            string key = artifact.ArtifactType.Name; // TODO: change this when Artifact.Key gets patched
            localizations[$"artifact.{key}.name"] = name;
            localizations[$"artifact.{key}.desc"] = description;
        }
    }

    public void RegisterSprite(IModManifest mod, ExternalSprite value)
    {
        Func<Stream> GetStreamProvider()
        {
            if (value.virtual_location is { } provider)
                return provider;
            if (value.physical_location is { } path)
                return () => path.OpenRead().ToMemoryStream();
            throw new ArgumentException("Unsupported ExternalSprite");
        }

        var entry = this.ContentManagerProvider().Sprites.RegisterSprite(mod, value.GlobalName, GetStreamProvider());
        value.Id = (int)entry.Sprite;
        this.GlobalNameToSprite[value.GlobalName] = value;
    }

    public void RegisterDeck(IModManifest mod, ExternalDeck value)
    {
        DeckConfiguration configuration = new()
        {
            Definition = new() { color = new((uint)value.DeckColor.ToArgb()), titleColor = new((uint)value.TitleColor.ToArgb()) },
            DefaultCardArt = (Spr)value.CardArtDefault.Id!.Value,
            BorderSprite = (Spr)value.BorderSprite.Id!.Value,
            OverBordersSprite = value.BordersOverSprite is null ? null : (Spr)value.BordersOverSprite.Id!.Value
        };
        var entry = this.ContentManagerProvider().Decks.RegisterDeck(mod, value.GlobalName, configuration);
        value.Id = (int)entry.Deck;
        this.GlobalNameToDeck[value.GlobalName] = value;
    }

    public void RegisterStatus(IModManifest mod, ExternalStatus value)
    {
        StatusConfiguration configuration = new()
        {
            Definition = new()
            {
                icon = (Spr)value.Icon.Id!.Value,
                color = new((uint)value.MainColor.ToArgb()),
                border = value.BorderColor is { } borderColor ? new((uint)borderColor.ToArgb()) : null,
                affectedByTimestop = value.AffectedByTimestop,
                isGood = value.IsGood
            }
        };
        var entry = this.ContentManagerProvider().Statuses.RegisterStatus(mod, value.GlobalName, configuration);
        value.Id = (int)entry.Status;
        this.GlobalNameToStatus[value.GlobalName] = value;
    }

    public void RegisterCard(IModManifest mod, ExternalCard value)
    {
        var meta = value.CardType.GetCustomAttribute<CardMeta>() ?? new();
        if (value.ActualDeck is { } deck)
            meta.deck = (Deck)deck.Id!.Value;

        CardConfiguration configuration = new()
        {
            CardType = value.CardType,
            Meta = meta,
            Art = (Spr)value.CardArt.Id!.Value
        };

        this.ContentManagerProvider().Cards.RegisterCard(mod, value.GlobalName, configuration);
        this.GlobalNameToCard[value.GlobalName] = value;
    }

    public void RegisterArtifact(IModManifest mod, ExternalArtifact value)
    {
        var meta = value.ArtifactType.GetCustomAttribute<ArtifactMeta>() ?? new();
        if (value.OwnerDeck is { } deck)
            meta.owner = (Deck)deck.Id!.Value;

        ArtifactConfiguration configuration = new()
        {
            ArtifactType = value.ArtifactType,
            Meta = meta,
            Sprite = (Spr)value.Sprite.Id!.Value
        };

        this.ContentManagerProvider().Artifacts.RegisterArtifact(mod, value.GlobalName, configuration);
        this.GlobalNameToArtifact[value.GlobalName] = value;
    }

    public ExternalSprite GetSprite(string globalName)
        => this.GlobalNameToSprite.TryGetValue(globalName, out var value) ? value : throw new KeyNotFoundException();

    public ExternalDeck GetDeck(string globalName)
        => this.GlobalNameToDeck.TryGetValue(globalName, out var value) ? value : throw new KeyNotFoundException();

    public ExternalStatus GetStatus(string globalName)
        => this.GlobalNameToStatus.TryGetValue(globalName, out var value) ? value : throw new KeyNotFoundException();

    public ExternalCard GetCard(string globalName)
        => this.GlobalNameToCard.TryGetValue(globalName, out var value) ? value : throw new KeyNotFoundException();

    public ExternalArtifact GetArtifact(string globalName)
        => this.GlobalNameToArtifact.TryGetValue(globalName, out var value) ? value : throw new KeyNotFoundException();
}
