using System.Collections.Generic;
using UnityEngine;

public class WeaponStatLoader : MonoBehaviour
{
    public static WeaponStatDB DB { get; private set; }

    [Header("CSV - Hacker")]
    [SerializeField] private List<TextAsset> hackerWeaponStatCsvFiles = new();

    [Header("CSV - Cyborg")]
    [SerializeField] private List<TextAsset> cyborgWeaponStatCsvFiles = new();

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        LoadWeaponStats();
    }

    private void LoadWeaponStats()
    {
        DB = new WeaponStatDB();

        bool hasHacker = hackerWeaponStatCsvFiles != null && hackerWeaponStatCsvFiles.Count > 0;
        bool hasCyborg = cyborgWeaponStatCsvFiles != null && cyborgWeaponStatCsvFiles.Count > 0;

        if (!hasHacker && !hasCyborg)
        {
            Debug.LogWarning("[WeaponStatLoader] Hacker/Cyborg 무기 CSV가 모두 비어 있습니다.");
            return;
        }

        LoadCsvGroup("Hacker", hackerWeaponStatCsvFiles);
        LoadCsvGroup("Cyborg", cyborgWeaponStatCsvFiles);

        Debug.Log($"[WeaponStatLoader] 전체 로드 완료: {DB.rows.Count} weapons");
    }

    private void LoadCsvGroup(string groupName, List<TextAsset> csvFiles)
    {
        if (csvFiles == null || csvFiles.Count == 0)
        {
            Debug.LogWarning($"[WeaponStatLoader] {groupName} 무기 CSV가 비어 있습니다.");
            return;
        }

        foreach (TextAsset csvFile in csvFiles)
        {
            if (csvFile == null)
            {
                Debug.LogWarning($"[WeaponStatLoader] {groupName} CSV 목록에 비어 있는 항목이 있습니다.");
                continue;
            }

            LoadSingleCSV(csvFile);
        }
    }

    private void LoadSingleCSV(TextAsset csvFile)
    {
        string csv = csvFile.text;

        var parsed = SimpleCsv.Parse(csv);

        foreach (var r in parsed)
        {
            if (!SimpleCsv.TryGetString(r, "weaponid", out string weaponId))
                continue;

            if (!SimpleCsv.TryGetFloat(r, "level", out float levelFloat))
                continue;

            int level = Mathf.RoundToInt(levelFloat);
            weaponId = weaponId.Trim().ToLower();

            var row = new WeaponStatDB.Row();
            row.level = level;

            SimpleCsv.TryGetFloat(r, "damage", out row.damage);
            SimpleCsv.TryGetFloat(r, "range", out row.range);
            SimpleCsv.TryGetFloat(r, "angle", out row.angle);
            SimpleCsv.TryGetFloat(r, "firerate", out row.firerate);

            // LaserSword 전용 스탯

            SimpleCsv.TryGetFloat(r, "riftduration", out row.riftduration);
            SimpleCsv.TryGetFloat(r, "riftradius", out row.riftradius);
            SimpleCsv.TryGetFloat(r, "rifttick", out row.rifttick);
            SimpleCsv.TryGetFloat(r, "riftdamagefactor", out row.riftdamagefactor);

            SimpleCsv.TryGetFloat(r, "damageperlevel", out row.damageperlevel);
            SimpleCsv.TryGetFloat(r, "rangeperlevel", out row.rangeperlevel);
            SimpleCsv.TryGetFloat(r, "fireratemulperlevel", out row.fireratemulperlevel);

            // SurgeBlade 전용 스탯
            if (SimpleCsv.TryGetFloat(r, "basemaxtargets", out float baseTargets))
                row.basemaxtargets = Mathf.RoundToInt(baseTargets);

            if (SimpleCsv.TryGetFloat(r, "maxtargetsatlv5", out float maxTargetsLv5))
                row.maxtargetsatlv5 = Mathf.RoundToInt(maxTargetsLv5);

            SimpleCsv.TryGetFloat(r, "mastermultiplier", out row.mastermultiplier);

            // EnergyShield 전용 스탯
            SimpleCsv.TryGetFloat(r, "rotationspeed", out row.rotationspeed);
            SimpleCsv.TryGetFloat(r, "radiusperlevel", out row.radiusperlevel);

            if (SimpleCsv.TryGetFloat(r, "baseorbcount", out float baseOrbCount))
                row.baseorbcount = Mathf.RoundToInt(baseOrbCount);

            if (SimpleCsv.TryGetFloat(r, "maxorbcount", out float maxOrbCount))
                row.maxorbcount = Mathf.RoundToInt(maxOrbCount);

            SimpleCsv.TryGetFloat(r, "masterdamagefactor", out row.masterdamagefactor);

            // PlasmaPhoton 전용 스탯
            SimpleCsv.TryGetFloat(r, "fireinterval", out row.fireinterval);
            SimpleCsv.TryGetFloat(r, "speed", out row.speed);
            SimpleCsv.TryGetFloat(r, "lifetime", out row.lifetime);
            SimpleCsv.TryGetFloat(r, "basedamage", out row.basedamage);

            SimpleCsv.TryGetFloat(r, "applyslow", out row.applyslow);
            SimpleCsv.TryGetFloat(r, "slowmul", out row.slowmul);
            SimpleCsv.TryGetFloat(r, "slowduration", out row.slowduration);

            SimpleCsv.TryGetFloat(r, "applystun", out row.applystun);
            SimpleCsv.TryGetFloat(r, "stunduration", out row.stunduration);

            SimpleCsv.TryGetFloat(r, "applyroot", out row.applyroot);
            SimpleCsv.TryGetFloat(r, "rootduration", out row.rootduration);

            SimpleCsv.TryGetFloat(r, "applyblind", out row.applyblind);
            SimpleCsv.TryGetFloat(r, "blindduration", out row.blindduration);

            SimpleCsv.TryGetFloat(r, "applyvulnerable", out row.applyvulnerable);
            SimpleCsv.TryGetFloat(r, "vulnerablemul", out row.vulnerablemul);
            SimpleCsv.TryGetFloat(r, "vulnerableduration", out row.vulnerableduration);

            SimpleCsv.TryGetFloat(r, "destroyonhit", out row.destroyonhit);

            // GravityHammer 전용 스탯
            SimpleCsv.TryGetFloat(r, "knockbackforce", out row.knockbackforce);
            SimpleCsv.TryGetFloat(r, "knockbackperlevel", out row.knockbackperlevel);

            SimpleCsv.TryGetFloat(r, "blackholeduration", out row.blackholeduration);
            SimpleCsv.TryGetFloat(r, "blackholepullradius", out row.blackholepullradius);
            SimpleCsv.TryGetFloat(r, "blackholepullforce", out row.blackholepullforce);

            // BoosterKnuckle 전용 스탯
            SimpleCsv.TryGetFloat(r, "speedperlevel", out row.speedperlevel);

            if (SimpleCsv.TryGetFloat(r, "basecount", out float baseCount))
                row.basecount = Mathf.RoundToInt(baseCount);

            if (SimpleCsv.TryGetFloat(r, "maxcount", out float maxCount))
                row.maxcount = Mathf.RoundToInt(maxCount);

            SimpleCsv.TryGetFloat(r, "homingturnspeed", out row.homingturnspeed);
            SimpleCsv.TryGetFloat(r, "homingsearchrange", out row.homingsearchrange);

            // BoltLauncher 전용 스탯
            SimpleCsv.TryGetFloat(r, "baseburstinterval", out row.baseburstinterval);

            if (SimpleCsv.TryGetFloat(r, "baseburstcount", out float baseBurstCount))
                row.baseburstcount = Mathf.RoundToInt(baseBurstCount);

            SimpleCsv.TryGetFloat(r, "intervaldecreaseperlevel", out row.intervaldecreaseperlevel);

            if (SimpleCsv.TryGetFloat(r, "countincreaseperlevel", out float countIncrease))
                row.countincreaseperlevel = Mathf.RoundToInt(countIncrease);

            SimpleCsv.TryGetFloat(r, "minburstinterval", out row.minburstinterval);

            if (SimpleCsv.TryGetFloat(r, "maxburstcount", out float maxBurstCount))
                row.maxburstcount = Mathf.RoundToInt(maxBurstCount);

            SimpleCsv.TryGetFloat(r, "spreadangle", out row.spreadangle);

            if (SimpleCsv.TryGetFloat(r, "overloadstackstoexplode", out float overloadStacks))
                row.overloadstackstoexplode = Mathf.RoundToInt(overloadStacks);

            SimpleCsv.TryGetFloat(r, "overloadexplosionradius", out row.overloadexplosionradius);
            SimpleCsv.TryGetFloat(r, "overloadexplosiondamagefactor", out row.overloadexplosiondamagefactor);

            // TeslaCoilArmor 전용 스탯
            SimpleCsv.TryGetFloat(r, "auradamage", out row.auradamage);
            SimpleCsv.TryGetFloat(r, "auraradius", out row.auraradius);
            SimpleCsv.TryGetFloat(r, "auratick", out row.auratick);

            SimpleCsv.TryGetFloat(r, "puddleinterval", out row.puddleinterval);
            SimpleCsv.TryGetFloat(r, "puddleduration", out row.puddleduration);
            SimpleCsv.TryGetFloat(r, "puddleradius", out row.puddleradius);
            SimpleCsv.TryGetFloat(r, "puddletick", out row.puddletick);
            SimpleCsv.TryGetFloat(r, "puddledamagefactor", out row.puddledamagefactor);

            SimpleCsv.TryGetFloat(r, "lightninginterval", out row.lightninginterval);
            SimpleCsv.TryGetFloat(r, "lightningrange", out row.lightningrange);
            SimpleCsv.TryGetFloat(r, "lightningdamagefactor", out row.lightningdamagefactor);

            // ChainSaw 전용 스탯
            SimpleCsv.TryGetFloat(r, "hpratiodamage", out row.hpratiodamage);
            SimpleCsv.TryGetFloat(r, "hitcooldown", out row.hitcooldown);
            SimpleCsv.TryGetFloat(r, "radiusscale", out row.radiusscale);
            SimpleCsv.TryGetFloat(r, "hitradius", out row.hitradius);

            SimpleCsv.TryGetFloat(r, "sizeperlevel", out row.sizeperlevel);
            SimpleCsv.TryGetFloat(r, "cooldownmulperlevel", out row.cooldownmulperlevel);
            SimpleCsv.TryGetFloat(r, "lifetimeperlevel", out row.lifetimeperlevel);
            SimpleCsv.TryGetFloat(r, "mastersizefactor", out row.mastersizefactor);

            SimpleCsv.TryGetFloat(r, "halfwidth", out row.halfwidth);
            SimpleCsv.TryGetFloat(r, "halfheight", out row.halfheight);

            SimpleCsv.TryGetFloat(r, "spawninterval", out row.spawninterval);

            if (SimpleCsv.TryGetFloat(r, "spawncount", out float spawnCount))
                row.spawncount = Mathf.RoundToInt(spawnCount);

            SimpleCsv.TryGetFloat(r, "spawnradius", out row.spawnradius);

            // BlastBreath 전용 스탯
            SimpleCsv.TryGetFloat(r, "damagepertick", out row.damagepertick);
            SimpleCsv.TryGetFloat(r, "width", out row.width);
            SimpleCsv.TryGetFloat(r, "tickinterval", out row.tickinterval);
            SimpleCsv.TryGetFloat(r, "areaduration", out row.areaduration);
            SimpleCsv.TryGetFloat(r, "aimrange", out row.aimrange);

            SimpleCsv.TryGetFloat(r, "widthperlevel", out row.widthperlevel);

            SimpleCsv.TryGetFloat(r, "freezeduration", out row.freezeduration);
            SimpleCsv.TryGetFloat(r, "freezeasstun", out row.freezeasstun);
            SimpleCsv.TryGetFloat(r, "applyslowincold", out row.applyslowincold);
            SimpleCsv.TryGetFloat(r, "slowmul", out row.slowmul);
            SimpleCsv.TryGetFloat(r, "slowduration", out row.slowduration);

            // LinkPistol 전용 스탯
            SimpleCsv.TryGetFloat(r, "bulletspeed", out row.bulletspeed);

            // AIDrone 전용 스탯
            SimpleCsv.TryGetFloat(r, "followdistance", out row.followdistance);
            SimpleCsv.TryGetFloat(r, "followspeed", out row.followspeed);
            SimpleCsv.TryGetFloat(r, "detectionrange", out row.detectionrange);
            SimpleCsv.TryGetFloat(r, "attackspeed", out row.attackspeed);
            SimpleCsv.TryGetFloat(r, "projectilespeed", out row.projectilespeed);

            if (SimpleCsv.TryGetFloat(r, "subdronecount", out float subDroneCount))
                row.subdronecount = Mathf.RoundToInt(subDroneCount);

            // AutoTurret 전용 스탯
            SimpleCsv.TryGetFloat(r, "installcooldown", out row.installcooldown);
            SimpleCsv.TryGetFloat(r, "lifetimeperlevel", out row.lifetimeperlevel);

            // DataScrambler 전용 스탯
            SimpleCsv.TryGetFloat(r, "durationperlevel", out row.durationperlevel);
            SimpleCsv.TryGetFloat(r, "frenzyduration", out row.frenzyduration);
            SimpleCsv.TryGetFloat(r, "explosionradius", out row.explosionradius);
            SimpleCsv.TryGetFloat(r, "duration", out row.duration);

            // EMPPulseGenerator 전용 스탯
            SimpleCsv.TryGetFloat(r, "areasize", out row.areasize);
            SimpleCsv.TryGetFloat(r, "spawnradius", out row.spawnradius);
            SimpleCsv.TryGetFloat(r, "areasizeperlevel", out row.areasizeperlevel);

            // PlasmaRifle 전용 스탯
            SimpleCsv.TryGetFloat(r, "bulletspeed", out row.bulletspeed);
            SimpleCsv.TryGetFloat(r, "duration", out row.duration);

            if (SimpleCsv.TryGetFloat(r, "penetration", out float penetration))
                row.penetration = Mathf.RoundToInt(penetration);

            // HologramDecoy 전용 스탯
            SimpleCsv.TryGetFloat(r, "hp", out row.hp);
            SimpleCsv.TryGetFloat(r, "cooldown", out row.cooldown);
            SimpleCsv.TryGetFloat(r, "prisonduration", out row.prisonduration);
            SimpleCsv.TryGetFloat(r, "hpperlevel", out row.hpperlevel);
            SimpleCsv.TryGetFloat(r, "cooldownreductionperlevel", out row.cooldownreductionperlevel);
            SimpleCsv.TryGetFloat(r, "prisonrange", out row.prisonrange);

            // NanoWire 전용 스탯
            SimpleCsv.TryGetFloat(r, "spawndistance", out row.spawndistance);
            SimpleCsv.TryGetFloat(r, "activationdelay", out row.activationdelay);
            SimpleCsv.TryGetFloat(r, "tickrate", out row.tickrate);
            SimpleCsv.TryGetFloat(r, "hitradius", out row.hitradius);

            // EMPGrenade 전용 스탯
            SimpleCsv.TryGetFloat(r, "arcHeight", out row.archeight);
            SimpleCsv.TryGetFloat(r, "travelTime", out row.traveltime);

            // DataOptimization 전용 스탯
            SimpleCsv.TryGetFloat(r, "fieldduration", out row.fieldduration);
            SimpleCsv.TryGetFloat(r, "fieldwidth", out row.fieldwidth);
            SimpleCsv.TryGetFloat(r, "fieldheight", out row.fieldheight);
            SimpleCsv.TryGetFloat(r, "damagebuffmultiplier", out row.damagebuffmultiplier);
            SimpleCsv.TryGetFloat(r, "buffduration", out row.buffduration);
            SimpleCsv.TryGetFloat(r, "masterslowmul", out row.masterslowmul);
            SimpleCsv.TryGetFloat(r, "masterslowduration", out row.masterslowduration);

            // DigitalShield 전용 스탯
            SimpleCsv.TryGetFloat(r, "knockbackduration", out row.knockbackduration);
            SimpleCsv.TryGetFloat(r, "collisiondamagemultiplier", out row.collisiondamagemultiplier);
            SimpleCsv.TryGetFloat(r, "collisiondetectradius", out row.collisiondetectradius);
            SimpleCsv.TryGetFloat(r, "collisionimpactradius", out row.collisionimpactradius);
            SimpleCsv.TryGetFloat(r, "collisioncarrierduration", out row.collisioncarrierduration);

            if (!DB.rows.ContainsKey(weaponId))
                DB.rows[weaponId] = new System.Collections.Generic.Dictionary<int, WeaponStatDB.Row>();

            DB.rows[weaponId][level] = row;
        }

        Debug.Log($"[WeaponStatLoader] 로드됨: {csvFile.name}");
    }
}
