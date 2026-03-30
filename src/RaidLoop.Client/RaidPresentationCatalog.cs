namespace RaidLoop.Client;

public static class RaidPresentationCatalog
{
    public static string GetEncounterDescription(string? encounterDescriptionKey, EncounterType encounterType, string? serverDescription, bool extractHoldActive = false)
    {
        if (!string.IsNullOrWhiteSpace(encounterDescriptionKey))
        {
            return encounterDescriptionKey switch
            {
                "combat_contact" => "Enemy contact on your position.",
                "combat_hunter_contact" => "Hunter contact.",
                "combat_extract_ambush" => "You are ambushed while moving between positions.",
                "combat_mutual_contact" => "You and a patrol spot each other at the same moment.",
                "loot_container" => "A searchable container appears.",
                "extract_ready" => "You are near the extraction route.",
                "extract_hold" => "Holding at extract.",
                "neutral_travel" => "You move through the area carefully.",
                _ => GetEncounterDescription(encounterType, serverDescription, extractHoldActive)
            };
        }

        return GetEncounterDescription(encounterType, serverDescription, extractHoldActive);
    }

    public static string GetEncounterDescription(EncounterType encounterType, string? serverDescription, bool extractHoldActive = false)
    {
        if (extractHoldActive)
        {
            return "Holding at extract.";
        }

        if (!string.IsNullOrWhiteSpace(serverDescription))
        {
            return serverDescription!;
        }

        return encounterType switch
        {
            EncounterType.Combat => "Enemy contact on your position.",
            EncounterType.Loot => "A searchable container appears.",
            EncounterType.Extraction => "You are near the extraction route.",
            EncounterType.Neutral => "You move through the area carefully.",
            _ => string.Empty
        };
    }

    public static string GetEnemyLabel(string? serverEnemyName)
    {
        if (string.IsNullOrWhiteSpace(serverEnemyName))
        {
            return string.Empty;
        }

        return serverEnemyName!.Replace("Scav", "Scavenger", StringComparison.OrdinalIgnoreCase);
    }

    public static string GetEnemyLabel(string? enemyKey, string? serverEnemyName)
    {
        if (!string.IsNullOrWhiteSpace(enemyKey))
        {
            return enemyKey switch
            {
                "scavenger" => "Scavenger",
                "extract_hunter" => "Extract Hunter",
                "guard" => "Guard",
                _ => GetEnemyLabel(serverEnemyName)
            };
        }

        return GetEnemyLabel(serverEnemyName);
    }

    public static string LocalizeLogEntry(string entry)
    {
        if (string.IsNullOrWhiteSpace(entry))
        {
            return string.Empty;
        }

        return entry.Replace("Scav", "Scavenger", StringComparison.OrdinalIgnoreCase);
    }
}
