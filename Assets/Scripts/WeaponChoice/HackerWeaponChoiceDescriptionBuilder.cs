using System.Globalization;
using NeoSurvive.Weapon;

public static class HackerWeaponChoiceDescriptionBuilder
{
    private const int MASTER_LEVEL = 5;

    public static bool TryBuild(WeaponBase weapon, int targetLevel, out string description)
    {
        description = string.Empty;

        if (weapon == null)
            return false;

        string statKey = GetHackerStatKey(weapon);
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

    private static string GetHackerStatKey(WeaponBase weapon)
    {
        switch (weapon.name)
        {
            case "01_LinkPistol":
                return "linkpistol";
            case "02_PlasmaRifle":
                return "plasmarifle";
            case "03_DataScrambler":
                return "datascrambler";
            case "04_AIDrone":
                return "aidrone";
            case "05_TacticalTurret":
                return "autoturret";
            case "06_EMPPulseGenerator":
                return "emppulsegenerator";
            case "07_DigitalShield":
                return "digitalshield";
            case "08_DataOptimization":
                return "dataoptimization";
            case "09_HologramDecoy":
                return "hologramdecoy";
            case "10_EMPGrenade":
                return "empgrenade";
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
            case "linkpistol":
                return levelText +
                    $"적에게 탄환을 발사합니다.\n" +
                    $"피해 {F(row.damage)}, 사거리 {F(row.range)}, 공격주기 {F(row.firerate)}초";

            case "plasmarifle":
                return levelText +
                    $"관통하는 레이저를 발사해 경로상의 적을 타격합니다.\n" +
                    $"피해 {F(row.damage)}, 사거리 {F(row.range)}, 관통 {row.penetration}회";

            case "datascrambler":
                return levelText +
                    $"부채꼴 범위에 교란 신호를 방출해 적에게 광란효과를 줍니다.\n" +
                    $"피해 {F(row.damage)}, 사거리 {F(row.range)}, 각도 {F(row.angle)}도, 혼란 {F(row.duration)}초";

            case "aidrone":
                return levelText +
                    $"플레이어를 따라다니는 드론이 소환되어 자동으로 적을 공격합니다.\n" +
                    $"피해 {F(row.damage)}, 공격주기 {F(row.attackspeed)}초, 드론 {row.subdronecount}기 추가";

            case "autoturret":
                return levelText +
                    $"일정 주기로 자동 포탑을 설치해 주변 적을 사격합니다.\n" +
                    $"설치 쿨타임 {F(row.installcooldown)}초, 유지 {F(row.lifetime)}초, 피해 {F(row.damage)}, 사거리 {F(row.range)}, 공격주기 {F(row.firerate)}초";

            case "emppulsegenerator":
                return levelText +
                    $"주변 위치에 EMP 필드를 생성해 범위 안의 적에게 피해를 줍니다.\n" +
                    $"피해 {F(row.damage)}, 범위 {F(row.areasize)}, 지속 {F(row.duration)}초, 쿨타임 {F(row.firerate)}초";

            case "digitalshield":
                return levelText +
                    $"원형 파동을 방출해 주변 적을 밀쳐내고 피해를 줍니다.\n" +
                    $"피해 {F(row.damage)}, 타격 반경 {F(row.hitradius)}, 넉백 {F(row.knockbackforce)}, 쿨타임 {F(row.firerate)}초";

            case "dataoptimization":
                return levelText +
                    $"데이터 필드를 전개해 접촉한 적이 받는 피해를 증가시킵니다.\n" +
                    $"필드 지속 {F(row.fieldduration)}초, 크기 {F(row.fieldwidth)}x{F(row.fieldheight)}, 피해 증가 {PercentMultiplier(row.damagebuffmultiplier)}, 버프 {F(row.buffduration)}초";

            case "hologramdecoy":
                return levelText +
                    $"홀로그램 분신을 생성해 적의 공격을 대신 받아냅니다.\n" +
                    $"분신 체력 {F(row.hp)}, 쿨타임 {F(row.cooldown)}초";

            case "empgrenade":
                return levelText +
                    $"적 위치로 EMP 수류탄을 투척해 폭발 피해를 줍니다.\n" +
                    $"피해 {F(row.damage)}, 폭발 반경 {F(row.explosionradius)}, 사거리 {F(row.range)}, 투척 {GetGrenadeCount(targetLevel)}개";

            default:
                return string.Empty;
        }
    }

    private static string BuildMasterDescription(string statKey, WeaponStatDB.Row row)
    {
        switch (statKey)
        {
            case "linkpistol":
                return "5레벨 달성 효과:\n적중한 적에게 5초동안 표식을 남깁니다.\n표식 대상은 무기 피해를 20% 더 받습니다.";
            case "plasmarifle":
                return "5레벨 달성 효과:\n주변 적 최대 2명에게 분열 레이저를 발사합니다.";
            case "datascrambler":
                return $"5레벨 달성 효과:\n혼란 대상이 사망하면 스스로 폭발합니다.\n광역피해 {F(row.damage)}";
            case "aidrone":
                return "5레벨 달성 효과:\n드론 수가 2배가 되고 드론 사이에 레이저 그물을 생성합니다.";
            case "autoturret":
                return "5레벨 달성 효과:\n포탑이 플레이어를 따라다니며 적을 공격합니다.";
            case "emppulsegenerator":
                return "5레벨 달성 효과:\nEMP 필드가 범위 안의 적 투사체를 제거합니다.";
            case "digitalshield":
                return $"5레벨 달성 효과:\n밀려난 적이 충돌 피해를 주는 충격체가 됩니다.\n충돌 피해 {F(row.damage * row.collisiondamagemultiplier)}";
            case "dataoptimization":
                return $"5레벨 달성 효과:\n필드 접촉 적의 이동속도를 {Percent(row.masterslowmul)}로 감소시킵니다.\n지속 {F(row.masterslowduration)}초";
            case "hologramdecoy":
                return $"5레벨 달성 효과:\n분신 종료 시 주변 적을 속박합니다.\n범위 {F(row.prisonrange)}, 속박 {F(row.prisonduration)}초";
            case "empgrenade":
                return $"5레벨 달성 효과:\n수류탄에 적이 맞으면 기절합니다.\n기절 {F(row.stunduration)}초";
            default:
                return string.Empty;
        }
    }

    private static int GetGrenadeCount(int targetLevel)
    {
        if (targetLevel >= MASTER_LEVEL)
            return 4;

        if (targetLevel >= 3)
            return 2;

        return 1;
    }

    private static string PercentMultiplier(float multiplier)
    {
        float value = (multiplier - 1f) * 100f;
        return $"{F(value)}%";
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
