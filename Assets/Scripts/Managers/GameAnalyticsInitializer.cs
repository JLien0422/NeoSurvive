using System;
using System.Collections.Generic;
using GameAnalyticsSDK;
using NeoSurvive.Characters;
using NeoSurvive.Weapon;
using UnityEngine;

public sealed class GameAnalyticsInitializer : MonoBehaviour
{
    private const string InitializerObjectName = "[GameAnalytics Initializer]";

    private static bool bootstrapCreated;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (bootstrapCreated)
            return;

        bootstrapCreated = true;

        GameObject initializerObject = new GameObject(InitializerObjectName);
        DontDestroyOnLoad(initializerObject);
        initializerObject.AddComponent<GameAnalyticsInitializer>();
    }

    private void Start()
    {
        EnsureGameAnalyticsObjectExists();
        InitializeGameAnalytics();
    }

    private void EnsureGameAnalyticsObjectExists()
    {
        if (FindObjectOfType<GameAnalytics>() != null)
            return;

        gameObject.AddComponent<GameAnalytics>();
    }

    private static void InitializeGameAnalytics()
    {
        if (GameAnalytics.Initialized)
            return;

        GameAnalytics.Initialize();
    }
}

public static class GameAnalyticsTracker
{
    private static string currentRunId;
    private static Boss currentBoss;
    private static bool bossReached;
    private static string lastBossPatternHit = "None";
    private static string lastBossHitType = "None";
    private static float lastBossPatternHitTime = -1f;

    public static void TrackPlayButtonPressed(CharacterType selectedCharacter)
    {
        SendDesignEvent(
            "ui:play:press",
            1f,
            new Dictionary<string, object>
            {
                { "selected_character", selectedCharacter.ToString() }
            });
    }

    public static void TrackRunStarted(CharacterType selectedCharacter)
    {
        currentRunId = Guid.NewGuid().ToString("N").Substring(0, 12);

        SendDesignEvent(
            "run:start",
            1f,
            new Dictionary<string, object>
            {
                { "run_id", currentRunId },
                { "selected_character", selectedCharacter.ToString() }
            });
    }

    public static void TrackPhaseReached(int phaseIndex, float elapsedSeconds)
    {
        SendDesignEvent(
            "run:phase:reached",
            phaseIndex,
            new Dictionary<string, object>
            {
                { "run_id", CurrentRunId },
                { "phase", phaseIndex },
                { "elapsed_seconds", Mathf.RoundToInt(elapsedSeconds) }
            });
    }

    public static void TrackPlayerDeath(Player player)
    {
        GameManager gameManager = GameManager.Instance;
        float elapsedSeconds = gameManager != null ? gameManager.GetGameTime() : 0f;
        int phase = gameManager != null ? gameManager.CurrentPhase : 1;
        int killCount = gameManager != null ? gameManager.KillCount : 0;
        int runGold = gameManager != null ? gameManager.CurrentRunGold : 0;
        int playerLevel = player != null ? player.Level : 0;
        string characterType = player != null ? player.CharacterType.ToString() : "Unknown";

        Dictionary<string, object> fields = new Dictionary<string, object>
        {
            { "run_id", CurrentRunId },
            { "phase", phase },
            { "elapsed_seconds", Mathf.RoundToInt(elapsedSeconds) },
            { "player_level", playerLevel },
            { "kill_count", killCount },
            { "run_gold", runGold },
            { "character_type", characterType }
        };
        AddBossStateFields(fields);
        AddLastBossPatternFields(fields, elapsedSeconds);

        SendDesignEvent("run:end:death", elapsedSeconds, fields);
        SendDesignEvent($"run:death:phase:p{phase}", 1f, fields);
        SendDesignEvent("run:death:level", playerLevel, fields);
        SendDesignEvent("run:death:kills", killCount, fields);
        TrackWeaponSummaryOnRunEnd("death", elapsedSeconds, fields);
    }

    public static void TrackWeaponSelected(WeaponBase weapon, string source, bool isLevelUp, int slotIndex)
    {
        if (weapon == null)
            return;

        Dictionary<string, object> fields = new Dictionary<string, object>
        {
            { "run_id", CurrentRunId },
            { "weapon_id", weapon.weaponId },
            { "weapon_name", weapon.weaponName },
            { "weapon_level", weapon.level },
            { "source", source },
            { "action", isLevelUp ? "level_up" : "add" },
            { "slot_index", slotIndex }
        };

        SendDesignEvent("weapon:selected", weapon.level, fields);
    }

    public static void TrackHackableSpawned(string prefabName, Vector3 position)
    {
        GameManager gameManager = GameManager.Instance;
        int phase = gameManager != null ? gameManager.CurrentPhase : 1;
        float elapsedSeconds = gameManager != null ? gameManager.GetGameTime() : 0f;

        SendDesignEvent(
            "hacking:object:spawn",
            1f,
            new Dictionary<string, object>
            {
                { "run_id", CurrentRunId },
                { "phase", phase },
                { "elapsed_seconds", Mathf.RoundToInt(elapsedSeconds) },
                { "prefab_name", prefabName },
                { "x", position.x },
                { "y", position.y }
            });
    }

    public static void TrackHackableInteraction(HackableObjectType objectType, Vector3 position)
    {
        GameManager gameManager = GameManager.Instance;
        int phase = gameManager != null ? gameManager.CurrentPhase : 1;
        float elapsedSeconds = gameManager != null ? gameManager.GetGameTime() : 0f;

        SendDesignEvent(
            "hacking:object:interact",
            1f,
            new Dictionary<string, object>
            {
                { "run_id", CurrentRunId },
                { "phase", phase },
                { "elapsed_seconds", Mathf.RoundToInt(elapsedSeconds) },
                { "object_type", objectType.ToString() },
                { "minigame_type", GetMinigameType(objectType) },
                { "x", position.x },
                { "y", position.y }
            });
    }

    public static void TrackHackingResult(HackableObjectType objectType, bool success, float psychoIncreaseOnFail)
    {
        GameManager gameManager = GameManager.Instance;
        int phase = gameManager != null ? gameManager.CurrentPhase : 1;
        float elapsedSeconds = gameManager != null ? gameManager.GetGameTime() : 0f;

        SendDesignEvent(
            success ? "hacking:result:success" : "hacking:result:fail",
            1f,
            new Dictionary<string, object>
            {
                { "run_id", CurrentRunId },
                { "phase", phase },
                { "elapsed_seconds", Mathf.RoundToInt(elapsedSeconds) },
                { "object_type", objectType.ToString() },
                { "minigame_type", GetMinigameType(objectType) },
                { "result", success ? "success" : "fail" },
                { "psycho_increase_on_fail", success ? 0f : psychoIncreaseOnFail }
            });
    }

    public static void TrackNeuralLinkUsed(CharacterType characterType, float gaugeBeforeUse, float durationSeconds)
    {
        GameManager gameManager = GameManager.Instance;
        int phase = gameManager != null ? gameManager.CurrentPhase : 1;
        float elapsedSeconds = gameManager != null ? gameManager.GetGameTime() : 0f;

        SendDesignEvent(
            "neural:link:used",
            1f,
            new Dictionary<string, object>
            {
                { "run_id", CurrentRunId },
                { "phase", phase },
                { "elapsed_seconds", Mathf.RoundToInt(elapsedSeconds) },
                { "character_type", characterType.ToString() },
                { "gauge_before_use", gaugeBeforeUse },
                { "duration_seconds", durationSeconds }
            });
    }

    public static void TrackPsychoThresholdReached(CharacterType characterType, float threshold, float currentValue, float addedAmount)
    {
        GameManager gameManager = GameManager.Instance;
        int phase = gameManager != null ? gameManager.CurrentPhase : 1;
        float elapsedSeconds = gameManager != null ? gameManager.GetGameTime() : 0f;

        SendDesignEvent(
            $"psycho:threshold:{Mathf.RoundToInt(threshold)}",
            1f,
            new Dictionary<string, object>
            {
                { "run_id", CurrentRunId },
                { "phase", phase },
                { "elapsed_seconds", Mathf.RoundToInt(elapsedSeconds) },
                { "character_type", characterType.ToString() },
                { "threshold", threshold },
                { "current_value", currentValue },
                { "added_amount", addedAmount }
            });
    }

    public static void TrackBossReached(Boss boss, string prefabName, int mapId, int removedEnemyCount, Vector3 spawnPosition)
    {
        currentBoss = boss;
        bossReached = true;

        GameManager gameManager = GameManager.Instance;
        int phase = gameManager != null ? gameManager.CurrentPhase : 1;
        float elapsedSeconds = gameManager != null ? gameManager.GetGameTime() : 0f;

        Dictionary<string, object> fields = new Dictionary<string, object>
        {
            { "run_id", CurrentRunId },
            { "phase", phase },
            { "elapsed_seconds", Mathf.RoundToInt(elapsedSeconds) },
            { "boss_name", GetBossName(boss, prefabName) },
            { "boss_prefab", prefabName },
            { "map_id", mapId },
            { "removed_enemy_count", removedEnemyCount },
            { "x", spawnPosition.x },
            { "y", spawnPosition.y }
        };
        AddBossStateFields(fields);

        SendDesignEvent("boss:reached", 1f, fields);
    }

    public static void TrackBossDefeated(Boss boss)
    {
        GameManager gameManager = GameManager.Instance;
        float elapsedSeconds = gameManager != null ? gameManager.GetGameTime() : 0f;
        int killCount = gameManager != null ? gameManager.KillCount : 0;
        int runGold = gameManager != null ? gameManager.CurrentRunGold : 0;

        Dictionary<string, object> fields = new Dictionary<string, object>
        {
            { "run_id", CurrentRunId },
            { "phase", gameManager != null ? gameManager.CurrentPhase : 1 },
            { "elapsed_seconds", Mathf.RoundToInt(elapsedSeconds) },
            { "kill_count", killCount },
            { "run_gold", runGold },
            { "boss_name", GetBossName(boss, "Unknown") }
        };
        AddBossStateFields(fields, boss);

        SendDesignEvent("boss:defeated", 1f, fields);
        SendDesignEvent("run:end:clear", elapsedSeconds, fields);
        SendDesignEvent("run:clear:time", elapsedSeconds, fields);
        TrackWeaponSummaryOnRunEnd("clear", elapsedSeconds, fields);
    }

    public static void TrackBossPatternHit(string patternName, string hitType, float damage, Player player)
    {
        GameManager gameManager = GameManager.Instance;
        int phase = gameManager != null ? gameManager.CurrentPhase : 1;
        float elapsedSeconds = gameManager != null ? gameManager.GetGameTime() : 0f;

        lastBossPatternHit = patternName;
        lastBossHitType = hitType;
        lastBossPatternHitTime = elapsedSeconds;

        Dictionary<string, object> fields = new Dictionary<string, object>
        {
            { "run_id", CurrentRunId },
            { "phase", phase },
            { "elapsed_seconds", Mathf.RoundToInt(elapsedSeconds) },
            { "pattern_name", patternName },
            { "hit_type", hitType },
            { "damage", damage },
            { "player_level", player != null ? player.Level : 0 },
            { "character_type", player != null ? player.CharacterType.ToString() : "Unknown" }
        };
        AddBossStateFields(fields);

        SendDesignEvent("boss:pattern:hit", 1f, fields);
    }

    private static void TrackWeaponSummaryOnRunEnd(string result, float elapsedSeconds, IDictionary<string, object> runFields)
    {
        WeaponManager weaponManager = WeaponManager.Instance;
        if (weaponManager == null || weaponManager.activeWeapons == null)
            return;

        SendDesignEvent(
            "weapon:run:count",
            weaponManager.activeWeapons.Count,
            new Dictionary<string, object>(runFields)
            {
                { "result", result }
            });

        WeaponDamageStats damageStats = WeaponDamageStats.Instance;
        float safeElapsedSeconds = Mathf.Max(0.01f, elapsedSeconds);

        for (int i = 0; i < weaponManager.activeWeapons.Count; i++)
        {
            WeaponBase weapon = weaponManager.activeWeapons[i];
            if (weapon == null)
                continue;

            float totalDamage = damageStats != null ? damageStats.GetTotalDamage(weapon) : 0f;
            float activeSeconds = damageStats != null ? damageStats.GetActiveSeconds(weapon) : safeElapsedSeconds;
            float dps = damageStats != null ? damageStats.GetDPS(weapon) : totalDamage / safeElapsedSeconds;

            Dictionary<string, object> fields = new Dictionary<string, object>(runFields)
            {
                { "result", result },
                { "weapon_id", weapon.weaponId },
                { "weapon_name", weapon.weaponName },
                { "weapon_level", weapon.level },
                { "slot_index", i },
                { "total_damage", Mathf.RoundToInt(totalDamage) },
                { "weapon_active_seconds", activeSeconds },
                { "dps", dps }
            };

            SendDesignEvent("weapon:summary", totalDamage, fields);
        }
    }

    private static void AddBossStateFields(IDictionary<string, object> fields)
    {
        AddBossStateFields(fields, currentBoss);
    }

    private static void AddBossStateFields(IDictionary<string, object> fields, Boss boss)
    {
        fields["boss_reached"] = bossReached;

        if (boss == null)
        {
            fields["boss_alive"] = false;
            fields["boss_hp_percent_remaining"] = bossReached ? 0f : -1f;
            fields["boss_hp_percent_dealt"] = bossReached ? 100f : 0f;
            return;
        }

        float maxHp = boss.MaxHP != null ? Mathf.Max(1f, boss.MaxHP.GetValue()) : 1f;
        float currentHp = boss.CurrentHP != null ? Mathf.Clamp(boss.CurrentHP.CurrentValue, 0f, maxHp) : maxHp;
        float remainingPercent = currentHp / maxHp * 100f;

        fields["boss_alive"] = !boss.IsDead;
        fields["boss_name"] = GetBossName(boss, "Unknown");
        fields["boss_hp_current"] = Mathf.RoundToInt(currentHp);
        fields["boss_hp_max"] = Mathf.RoundToInt(maxHp);
        fields["boss_hp_percent_remaining"] = remainingPercent;
        fields["boss_hp_percent_dealt"] = 100f - remainingPercent;
    }

    private static void AddLastBossPatternFields(IDictionary<string, object> fields, float elapsedSeconds)
    {
        fields["last_boss_pattern_hit"] = lastBossPatternHit;
        fields["last_boss_hit_type"] = lastBossHitType;
        fields["seconds_since_last_boss_hit"] = lastBossPatternHitTime >= 0f ? elapsedSeconds - lastBossPatternHitTime : -1f;
    }

    private static string GetBossName(Boss boss, string fallback)
    {
        return boss != null ? boss.GetBossDisplayName() : fallback;
    }

    private static string CurrentRunId
    {
        get
        {
            if (string.IsNullOrEmpty(currentRunId))
                currentRunId = Guid.NewGuid().ToString("N").Substring(0, 12);

            return currentRunId;
        }
    }

    private static string GetMinigameType(HackableObjectType objectType)
    {
        switch (objectType)
        {
            case HackableObjectType.SecurityTurret:
                return "NumberSequence";
            case HackableObjectType.ElectricFence:
                return "CommandBypass";
            case HackableObjectType.SatelliteUplink:
                return "NetworkBridge";
            case HackableObjectType.SynapseServer:
                return "SynapseSync";
            case HackableObjectType.MagneticBeacon:
                return "FrequencyOverride";
            default:
                return "Unknown";
        }
    }

    private static void SendDesignEvent(string eventName, float value, IDictionary<string, object> fields)
    {
        try
        {
            if (!GameAnalytics.Initialized)
                GameAnalytics.Initialize();

            if (!GameAnalytics.Initialized)
                return;

            GameAnalytics.NewDesignEvent(eventName, value, fields);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[GameAnalyticsTracker] 이벤트 전송 실패: {eventName} / {exception.Message}");
        }
    }
}
