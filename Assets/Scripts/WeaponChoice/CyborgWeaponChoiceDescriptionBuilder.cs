using System.Collections.Generic;
using System.Globalization;
using NeoSurvive.Weapon;
using UnityEngine;

public static class CyborgWeaponChoiceDescriptionBuilder
{
    private const int MASTER_LEVEL = 5;

    public static bool TryBuild(WeaponBase weapon, int targetLevel, out string description)
    {
        description = string.Empty;

        if (weapon == null)
            return false;

        string statKey = GetCyborgStatKey(weapon);
        if (string.IsNullOrEmpty(statKey))
            return false;

        if (!TryGetStatRow(statKey, targetLevel, out WeaponStatDB.Row row))
            return false;

        description = BuildDescription(statKey, targetLevel, row);
        return !string.IsNullOrEmpty(description);
    }

    private static bool TryGetStatRow(string statKey, int targetLevel, out WeaponStatDB.Row row)
    {
        row = null;

        if (WeaponStatLoader.DB == null)
            return false;

        if (!WeaponStatLoader.DB.rows.TryGetValue(statKey, out var levels))
            return false;

        int clampedLevel = targetLevel < 1 ? 1 : targetLevel;
        clampedLevel = clampedLevel > MASTER_LEVEL ? MASTER_LEVEL : clampedLevel;

        return levels.TryGetValue(clampedLevel, out row);
    }

    private static string GetCyborgStatKey(WeaponBase weapon)
    {
        switch (weapon.name)
        {
            case "01_LaserSword":
                return "lasersword";
            case "02_SurgeBlade":
                return "surgeblade";
            case "03_EnergyShield":
                return "energyshield";
            case "04_PlasmaPhoton":
                return "plasmaphotongun";
            case "05_GravityHammer":
                return "gravityhammer";
            case "06_BoosterKnuckle":
                return "boosterknuckle";
            case "07_TeslaCoilArmor":
                return "teslacoilarmor";
            case "08_BoltLauncher":
                return "boltlauncher";
            case "09_ChainSaw":
                return "chainsaw";
            case "10_BlastBreath":
                return "blastbreath";
            default:
                return string.Empty;
        }
    }

    private static string BuildDescription(string statKey, int targetLevel, WeaponStatDB.Row row)
    {
        if (targetLevel >= MASTER_LEVEL)
            return BuildMasterDescription(statKey, row);

        string levelText = $"선택 후 Lv.{targetLevel}:\n";

        switch (statKey)
        {
            case "lasersword":
                return levelText +
                    $"전방 부채꼴 범위로 적을 베어 피해를 줍니다.\n" +
                    $"피해 {F(row.damage)}, 사거리 {F(row.range)}, 각도 {F(row.angle)}도, 공격주기 {F(row.firerate)}초";

            case "surgeblade":
                return levelText +
                    $"강력한 한 방으로 전방 적을 타격합니다.\n" +
                    $"피해 {F(row.damage)}, 사거리 {F(row.range)}, 각도 {F(row.angle)}도, 동시 타격 {GetSurgeBladeMaxTargets(targetLevel, row)}명, 공격주기 {F(row.firerate)}초";

            case "energyshield":
                return levelText +
                    $"플레이어 주위를 도는 방패가 적과 투사체를 막으며 피해를 줍니다.\n" +
                    $"피해 {F(row.damage)}, 회전반경 {F(row.radius)}, 회전속도 {F(row.rotationspeed)}도, 방패 {GetEnergyShieldOrbCount(targetLevel, row)}개";

            case "plasmaphotongun":
                return levelText +
                    $"직선 에너지 빔을 발사해 경로상의 적을 타격합니다.\n" +
                    $"피해 {F(row.basedamage)}, 사거리 {F(row.range)}, 발사간격 {F(row.fireinterval)}초{BuildPlasmaPhotonEffectSuffix(row)}";

            case "gravityhammer":
                return levelText +
                    $"지면을 강타해 부채꼴 충격파로 적을 타격합니다.\n" +
                    $"피해 {F(row.damage)}, 사거리 {F(row.range)}, 각도 {F(row.angle)}도, 넉백 {F(row.knockbackforce)}, 공격주기 {F(row.firerate)}초";

            case "boosterknuckle":
                return levelText +
                    $"로켓 추진 주먹을 전방으로 발사해 적을 타격합니다.\n" +
                    $"피해 {F(row.damage)}, 사거리 {F(row.range)}, 속도 {F(row.speed)}, 공격주기 {F(row.firerate)}초, 발사 {GetBoosterKnuckleCount(targetLevel, row)}개";

            case "teslacoilarmor":
                return levelText +
                    $"몸 주변에 전류 오라를 방출해 주변 적에게 지속 피해를 줍니다.\n" +
                    $"오라 피해 {F(row.auradamage)}, 반경 {F(row.auraradius)}, 틱 {F(row.auratick)}초";

            case "boltlauncher":
                GetBoltLauncherBurst(targetLevel, row, out float burstInterval, out int burstCount);
                return levelText +
                    $"유도 에너지 화살을 연발로 발사합니다.\n" +
                    $"피해 {F(row.damage)}, 사거리 {F(row.range)}, 연발 {burstCount}발, 연발간격 {F(burstInterval)}초";

            case "chainsaw":
                return levelText +
                    $"회전하는 체인톱이 적에게 연속 피해를 줍니다.\n" +
                    $"피해 {F(row.damage)}, 체력비례 {Percent(row.hpratiodamage)}, 명중쿨 {F(row.hitcooldown)}초, 속도 {F(row.speed)}, 타격반경 {F(row.hitradius * row.radiusscale)}";

            case "blastbreath":
                return levelText +
                    $"전방에 고열 화염을 방사해 범위 안의 적에게 지속 피해를 줍니다.\n" +
                    $"틱 피해 {F(row.damagepertick)}, 사거리 {F(row.range)}, 폭 {F(row.width)}, 지속 {F(row.areaduration)}초, 공격주기 {F(row.firerate)}초";

            default:
                return string.Empty;
        }
    }

    private static string BuildMasterDescription(string statKey, WeaponStatDB.Row row)
    {
        switch (statKey)
        {
            case "lasersword":
                return $"5레벨 달성 효과:\n베어간 궤적에 공간 균열이 남아 잔상 피해를 줍니다.\n지속 {F(row.riftduration)}초, 반경 {F(row.riftradius)}, 틱 {F(row.rifttick)}초, 피해계수 {F(row.riftdamagefactor)}";

            case "surgeblade":
                return $"5레벨 달성 효과:\n2회 공격마다 사거리와 범위, 피해가 {F(row.mastermultiplier)}배로 강화됩니다.\n최대 동시 타격 {row.maxtargetsatlv5}명";

            case "energyshield":
                return $"5레벨 달성 효과:\n방패 접촉 피해가 {F(row.masterdamagefactor)}배로 강화됩니다.\n방패 {row.maxorbcount}개까지 확장";

            case "plasmaphotongun":
                return $"5레벨 달성 효과:\n빔에 맞은 적이 추가 피해를 더 크게 받습니다.\n{BuildPlasmaPhotonMasterEffectText(row)}";

            case "gravityhammer":
                return $"5레벨 달성 효과:\n타격 지점에 소형 블랙홀이 생겨 적을 끌어당깁니다.\n지속 {F(row.blackholeduration)}초, 흡입반경 {F(row.blackholepullradius)}, 흡입력 {F(row.blackholepullforce)}";

            case "boosterknuckle":
                return "5레벨 달성 효과:\n주먹이 벽에 부딪히면 튕겨 나와 가장 가까운 적을 추격합니다.";

            case "teslacoilarmor":
                return $"5레벨 달성 효과:\n이동 경로에 전기 장판을 남기고 무작위 적에게 낙뢰를 떨어뜨립니다.\n장판 지속 {F(row.puddleduration)}초, 낙뢰 간격 {F(row.lightninginterval)}초, 낙뢰 사거리 {F(row.lightningrange)}";

            case "boltlauncher":
                return $"5레벨 달성 효과:\n적중 시 과부하 스택을 쌓고 {row.overloadstackstoexplode}스택에서 광역 폭발합니다.\n폭발 반경 {F(row.overloadexplosionradius)}, 피해계수 {F(row.overloadexplosiondamagefactor)}";

            case "chainsaw":
                return $"5레벨 달성 효과:\n톱날이 커지며 체력 비례 피해가 강화됩니다.\n체력비례 {Percent(row.hpratiodamage)}, 크기배율 {F(row.radiusscale)}";

            case "blastbreath":
                return $"5레벨 달성 효과:\n화염이 냉기로 바뀌어 적을 빙결시킵니다.\n빙결 {F(row.freezeduration)}초{BuildBlastBreathColdSuffix(row)}";

            default:
                return string.Empty;
        }
    }

    private static int GetSurgeBladeMaxTargets(int level, WeaponStatDB.Row row)
    {
        if (level <= 1)
            return row.basemaxtargets > 0 ? row.basemaxtargets : 1;

        if (level <= 3)
            return row.maxtargetsatlv5 > 0 ? Mathf.Min(2, row.maxtargetsatlv5) : 2;

        return row.maxtargetsatlv5 > 0 ? row.maxtargetsatlv5 : 3;
    }

    private static int GetEnergyShieldOrbCount(int level, WeaponStatDB.Row row)
    {
        int baseCount = row.baseorbcount > 0 ? row.baseorbcount : 1;
        int maxCount = row.maxorbcount > 0 ? row.maxorbcount : 5;
        return Mathf.Clamp(baseCount + (level - 1), baseCount, maxCount);
    }

    private static int GetBoosterKnuckleCount(int level, WeaponStatDB.Row row)
    {
        int baseCount = row.basecount > 0 ? row.basecount : 1;
        int maxCount = row.maxcount > 0 ? row.maxcount : 4;

        if (level <= 1)
            return baseCount;

        if (level <= 3)
            return Mathf.Min(2, maxCount);

        return Mathf.Min(3, maxCount);
    }

    private static void GetBoltLauncherBurst(int level, WeaponStatDB.Row row, out float burstInterval, out int burstCount)
    {
        float baseInterval = row.baseburstinterval > 0f ? row.baseburstinterval : 5f;
        int baseCount = row.baseburstcount > 0 ? row.baseburstcount : 4;
        float intervalDecrease = row.intervaldecreaseperlevel > 0f ? row.intervaldecreaseperlevel : 1f;
        int countIncrease = row.countincreaseperlevel > 0 ? row.countincreaseperlevel : 1;
        float minInterval = row.minburstinterval > 0f ? row.minburstinterval : 1f;
        int maxCount = row.maxburstcount > 0 ? row.maxburstcount : 8;

        burstInterval = baseInterval - (level - 1) * intervalDecrease;
        burstCount = baseCount + (level - 1) * countIncrease;

        if (burstInterval < minInterval)
            burstInterval = minInterval;

        if (burstCount > maxCount)
            burstCount = maxCount;
    }

    private static string BuildPlasmaPhotonEffectSuffix(WeaponStatDB.Row row)
    {
        string effects = BuildPlasmaPhotonEffects(row);
        return string.IsNullOrEmpty(effects) ? string.Empty : $", {effects}";
    }

    private static string BuildPlasmaPhotonEffects(WeaponStatDB.Row row)
    {
        List<string> effects = new List<string>();

        if (row.applyslow > 0.5f)
            effects.Add($"슬로우 {Percent(row.slowmul)} {F(row.slowduration)}초");

        if (row.applystun > 0.5f)
            effects.Add($"기절 {F(row.stunduration)}초");

        if (row.applyroot > 0.5f)
            effects.Add($"속박 {F(row.rootduration)}초");

        if (row.applyblind > 0.5f)
            effects.Add($"실명 {F(row.blindduration)}초");

        if (row.applyvulnerable > 0.5f)
            effects.Add($"받는 피해 {Percent(row.vulnerablemul)} {F(row.vulnerableduration)}초");

        return string.Join(", ", effects);
    }

    private static string BuildPlasmaPhotonMasterEffectText(WeaponStatDB.Row row)
    {
        if (row.applyvulnerable > 0.5f)
            return $"받는 피해 {Percent(row.vulnerablemul)}, 지속 {F(row.vulnerableduration)}초";

        return BuildPlasmaPhotonEffects(row);
    }

    private static string BuildBlastBreathColdSuffix(WeaponStatDB.Row row)
    {
        if (row.applyslowincold > 0.5f)
            return $", 슬로우 {Percent(row.slowmul)} {F(row.slowduration)}초";

        return string.Empty;
    }

    private static string Percent(float multiplier)
    {
        return $"{F(multiplier * 100f)}%";
    }

    private static string F(float value)
    {
        return value.ToString("0.##", CultureInfo.InvariantCulture);
    }
}
