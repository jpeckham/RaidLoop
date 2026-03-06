using System.Text.Json;
using Microsoft.JSInterop;
using RaidLoop.Core;

namespace RaidLoop.Client.Services;

public sealed class StashStorage
{
    private const string SaveKey = "raidloop.save.v2";
    private readonly IJSRuntime _js;

    public StashStorage(IJSRuntime js)
    {
        _js = js;
    }

    public async Task<GameSave> LoadAsync()
    {
        var raw = await _js.InvokeAsync<string?>("raidLoopStorage.load", SaveKey);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return CreateDefaultSave();
        }

        try
        {
            var save = JsonSerializer.Deserialize<GameSave>(raw);
            return save is null
                ? CreateDefaultSave()
                : NormalizeSave(save);
        }
        catch
        {
            // Migration path from v1 (stash-only payload)
            var legacyStash = JsonSerializer.Deserialize<List<Item>>(raw);
            if (legacyStash is not null)
            {
                return NormalizeSave(new GameSave(legacyStash, DateTimeOffset.MinValue, null));
            }

            return CreateDefaultSave();
        }
    }

    public async Task SaveAsync(GameSave save)
    {
        var payload = JsonSerializer.Serialize(save);
        await _js.InvokeVoidAsync("raidLoopStorage.save", SaveKey, payload);
    }

    private static GameSave CreateDefaultSave()
    {
        return NormalizeSave(new GameSave(
            MainStash:
            [
                new Item("Makarov", ItemType.Weapon, 1),
                new Item("Hunting Rifle", ItemType.Weapon, 1),
                new Item("Soft Vest", ItemType.Armor, 1),
                new Item("Plate Carrier", ItemType.Armor, 1),
                new Item("Small Backpack", ItemType.Backpack, 1),
                new Item("Tactical Backpack", ItemType.Backpack, 1),
                new Item("Medkit", ItemType.Consumable, 1),
                new Item("Bandage", ItemType.Consumable, 1),
                new Item("Ammo Box", ItemType.Material, 1)
            ],
            RandomCharacterAvailableAt: DateTimeOffset.MinValue,
            RandomCharacter: null));
    }

    private static GameSave NormalizeSave(GameSave save)
    {
        EnsureKnifeFallback(save.MainStash);
        return save;
    }

    private static void EnsureKnifeFallback(List<Item> stash)
    {
        if (stash.Count == 0)
        {
            stash.Add(new Item("Rusty Knife", ItemType.Weapon, 1));
        }
    }
}

public sealed record RandomCharacterState(string Name, List<Item> Inventory);

public sealed record GameSave(
    List<Item> MainStash,
    DateTimeOffset RandomCharacterAvailableAt,
    RandomCharacterState? RandomCharacter);
