using System.IO;

namespace RaidLoop.Core.Tests;

public sealed class StartRaidEntryAmbiguityHotfixTests
{
    private static readonly string MigrationPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "supabase", "migrations", "2026032602_fix_start_raid_entry_ambiguity_prod_hotfix.sql"));

    [Fact]
    public void HotfixMigrationPinsUnambiguousRaidStartAliases()
    {
        Assert.True(File.Exists(MigrationPath));

        var migration = File.ReadAllText(MigrationPath);

        Assert.Contains("create or replace function game.start_raid_action", migration);
        Assert.Contains("loop_entry jsonb;", migration);
        Assert.Contains("from jsonb_array_elements(on_person_items) on_person_entry", migration);
        Assert.Contains("coalesce((coalesce(on_person_entry->'item', on_person_entry->'Item')->>'type')::int, -1)", migration);
        Assert.Contains("for loop_entry in", migration);
        Assert.Contains("game.normalize_item(loop_entry)", migration);
        Assert.DoesNotContain("from jsonb_array_elements(on_person_items) entry", migration);
        Assert.DoesNotContain("where coalesce((coalesce(entry->'item', entry->'Item')->>'type')::int, -1)", migration);
    }
}
