using System;
using System.Collections.Generic;

namespace NeoSurvive.Editor.BalancingSheets
{
    internal sealed class BalancingSheetDefinition
    {
        public string SheetName { get; }
        public string AssetPath { get; }
        public IReadOnlyList<string> KeyColumns { get; }

        public BalancingSheetDefinition(string sheetName, string assetPath, params string[] keyColumns)
        {
            SheetName = sheetName;
            AssetPath = assetPath;
            KeyColumns = keyColumns;
        }
    }

    internal static class BalancingSheetCatalog
    {
        public const string DefaultSpreadsheetId =
            "1nyczg8od5FGttGzFQnwWbGiyJ_GYOe1UYkC8OJXkNFE";

        public const string DefaultSpreadsheetUrl =
            "https://docs.google.com/spreadsheets/d/" + DefaultSpreadsheetId + "/edit";

        public static readonly IReadOnlyList<BalancingSheetDefinition> All =
            new[]
            {
                Define("01_Characters", "Assets/Data/Balancing/characters.csv", "id"),
                Define("02_EnemyStats", "Assets/Data/Balancing/enemy_stats.csv", "enemyid"),
                Define("03_EnemyDrops", "Assets/Data/Balancing/enemy_drops.csv", "enemyId"),
                Define("04_SpawnLegacy", "Assets/Data/Balancing/enemy_spawn_table.csv", "phase"),
                Define(
                    "05_SpawnPhase1",
                    "Assets/Data/Balancing/enemy_spawn_phase1_segments.csv",
                    "segment"
                ),
                Define(
                    "06_SpawnPhase2_5",
                    "Assets/Data/Balancing/enemy_spawn_phase2_5_table.csv",
                    "phase"
                ),
                Define("07_PlayerExp", "Assets/Data/Balancing/player_exp_table.csv", "level"),
                Define(
                    "08_MapObjects",
                    "Assets/Data/Balancing/Objects/Map Objects/Map 1/map1_map_objects.csv",
                    "mapId",
                    "id"
                ),
                Define(
                    "09_MapGimmicks",
                    "Assets/Data/Balancing/Objects/Map Objects/Map 1/map1_environment_gimmicks.csv",
                    "mapId",
                    "id"
                ),
                Define(
                    "C01_LaserSword",
                    "Assets/Data/Objects/Weapons/Cyborg/01lasersword_stats.csv",
                    "weaponid",
                    "level"
                ),
                Define(
                    "C02_SurgeBlade",
                    "Assets/Data/Objects/Weapons/Cyborg/02surgeblade_stats.csv",
                    "weaponid",
                    "level"
                ),
                Define(
                    "C03_EnergyShield",
                    "Assets/Data/Objects/Weapons/Cyborg/03energyshield_stats.csv",
                    "weaponid",
                    "level"
                ),
                Define(
                    "C04_PlasmaPhoton",
                    "Assets/Data/Objects/Weapons/Cyborg/04plasmaphotongun_stats.csv",
                    "weaponid",
                    "level"
                ),
                Define(
                    "C05_GravityHammer",
                    "Assets/Data/Objects/Weapons/Cyborg/05gravityhammer_stats.csv",
                    "weaponid",
                    "level"
                ),
                Define(
                    "C06_BoosterKnuckle",
                    "Assets/Data/Objects/Weapons/Cyborg/06boosterknuckle_stats.csv",
                    "weaponid",
                    "level"
                ),
                Define(
                    "C07_BoltLauncher",
                    "Assets/Data/Objects/Weapons/Cyborg/07boltlauncher_stats.csv",
                    "weaponid",
                    "level"
                ),
                Define(
                    "C08_TeslaCoilArmor",
                    "Assets/Data/Objects/Weapons/Cyborg/08teslacoilarmor_stats.csv",
                    "weaponid",
                    "level"
                ),
                Define(
                    "C09_ChainSaw",
                    "Assets/Data/Objects/Weapons/Cyborg/09chainsaw_stats.csv",
                    "weaponid",
                    "level"
                ),
                Define(
                    "C10_BlastBreath",
                    "Assets/Data/Objects/Weapons/Cyborg/10blastbreath.csv",
                    "weaponid",
                    "level"
                ),
                Define(
                    "H01_LinkPistol",
                    "Assets/Data/Objects/Weapons/Hacker/01linkpistol_stats.csv",
                    "weaponid",
                    "level"
                ),
                Define(
                    "H02_PlasmaRifle",
                    "Assets/Data/Objects/Weapons/Hacker/02plasmarifle_stats.csv",
                    "weaponid",
                    "level"
                ),
                Define(
                    "H03_DataScrambler",
                    "Assets/Data/Objects/Weapons/Hacker/03datascrambler_stats.csv",
                    "weaponid",
                    "level"
                ),
                Define(
                    "H04_AIDrone",
                    "Assets/Data/Objects/Weapons/Hacker/04aidrone_stats.csv",
                    "weaponid",
                    "level"
                ),
                Define(
                    "H05_AutoTurret",
                    "Assets/Data/Objects/Weapons/Hacker/05autoturret_stats.csv",
                    "weaponid",
                    "level"
                ),
                Define(
                    "H06_EMPPulse",
                    "Assets/Data/Objects/Weapons/Hacker/06emppulsegenerator_stats.csv",
                    "weaponid",
                    "level"
                ),
                Define(
                    "H07_DigitalShield",
                    "Assets/Data/Objects/Weapons/Hacker/07digitalshield_stats.csv",
                    "weaponid",
                    "level"
                ),
                Define(
                    "H08_DataOptimization",
                    "Assets/Data/Objects/Weapons/Hacker/08dataoptimization_stats.csv",
                    "weaponid",
                    "level"
                ),
                Define(
                    "H09_HologramDecoy",
                    "Assets/Data/Objects/Weapons/Hacker/09hologramdecoy_stats.csv",
                    "weaponid",
                    "level"
                ),
                Define(
                    "H10_EMPGrenade",
                    "Assets/Data/Objects/Weapons/Hacker/10empgrenade_stats.csv",
                    "weaponid",
                    "level"
                ),
            };

        public static bool TryExtractSpreadsheetId(string urlOrId, out string spreadsheetId)
        {
            spreadsheetId = string.Empty;
            if (string.IsNullOrWhiteSpace(urlOrId))
                return false;

            string input = urlOrId.Trim();
            const string marker = "/spreadsheets/d/";
            int markerIndex = input.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (markerIndex < 0)
            {
                spreadsheetId = input;
                return !input.Contains("/") && input.Length >= 20;
            }

            int idStart = markerIndex + marker.Length;
            int idEnd = input.IndexOf('/', idStart);
            spreadsheetId = idEnd < 0 ? input.Substring(idStart) : input.Substring(idStart, idEnd - idStart);
            return !string.IsNullOrWhiteSpace(spreadsheetId);
        }

        public static string BuildExportUrl(string spreadsheetId, string sheetName)
        {
            return
                $"https://docs.google.com/spreadsheets/d/{spreadsheetId}/gviz/tq"
                + $"?tqx=out:csv&sheet={Uri.EscapeDataString(sheetName)}";
        }

        private static BalancingSheetDefinition Define(
            string sheetName,
            string assetPath,
            params string[] keyColumns
        )
        {
            return new BalancingSheetDefinition(sheetName, assetPath, keyColumns);
        }
    }
}
