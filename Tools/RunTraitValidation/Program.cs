using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NeoSurvive.Characters;
using NeoSurvive.Weapon;

public static class Program
{
    private static int checks;
    private static readonly Dictionary<string, RunTraitDefinition> traits = new();
    private static readonly Dictionary<string, WeaponBase> weapons = new();
    private static RunTraitCatalog catalog;
    private static void Assert(bool condition, string name)
    {
        checks++;
        if (!condition) throw new Exception(name);
    }
    private static void Near(float actual, float expected, string name) =>
        Assert(Math.Abs(actual - expected) < 0.0001f, $"{name}: expected {expected}, got {actual}");
    private static string Field(string text, string name) =>
        Regex.Match(text, @"^  " + name + @": (.+)$", RegexOptions.Multiline).Groups[1].Value.Trim();
    private static void Set(object target, string name, object value) =>
        target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
    private static void LoadAssets()
    {
        var effects = new Dictionary<string, RunTraitEffect>();
        foreach (string path in Directory.GetFiles("Assets/Data/RunTraits/Effects", "*.asset"))
        {
            string text = File.ReadAllText(path);
            string guid = Regex.Match(File.ReadAllText(path + ".meta"), @"guid: (\w+)").Groups[1].Value;
            if (text.Contains("  kind:"))
            {
                string scriptGuid = Regex.Match(File.ReadAllText("Assets/Scripts/RunTrait/SpecialTraitEffect.cs.meta"), @"guid: (\w+)").Groups[1].Value;
                Assert(Field(text, "m_Script").Contains(scriptGuid), "special SO script GUID: " + path);
                var effect = new SpecialTraitEffect();
                effect.kind = (RunTraitSpecialKind)int.Parse(Field(text, "kind"));
                foreach (string field in new[] { "radius", "efficiency", "delay", "duration", "cooldown",
                    "shieldMaxHealthRatio", "shieldDecayPerSecond", "shieldDecayDelay" })
                    typeof(SpecialTraitEffect).GetField(field).SetValue(effect, float.Parse(Field(text, field), CultureInfo.InvariantCulture));
                effect.maxHops = int.Parse(Field(text, "maxHops"));
                effects.Add(guid, effect);
            }
            else if (text.Contains("bonusPerStack:") && !text.Contains("  status:"))
            {
                var effect = new DamageBonusEffect();
                Set(effect, "bonusPerStack", float.Parse(Field(text, "bonusPerStack"), CultureInfo.InvariantCulture));
                effects.Add(guid, effect);
            }
            else if (text.Contains("  status:"))
            {
                var effect = new OnHitStatusEffect();
                Set(effect, "status", (OnHitStatusEffect.StatusKind)int.Parse(Field(text, "status")));
                foreach (string field in new[] { "bonusPerStack", "slowDuration", "knockbackSpeed", "knockbackDuration" })
                    Set(effect, field, float.Parse(Field(text, field), CultureInfo.InvariantCulture));
                effects.Add(guid, effect);
            }
            else if (text.Contains("modifiers:"))
            {
                var effect = new WeaponStatModifierEffect();
                var modifiers = Regex.Matches(text, @"- stat: (\d+)\s+percentPerStack: ([-\d.]+)")
                    .Select(m => new WeaponStatModifierEffect.Modifier {
                        stat = (RunTraitStat)int.Parse(m.Groups[1].Value),
                        percentPerStack = float.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture)
                    }).ToArray();
                Assert(modifiers.Length > 0, "effect modifiers exist: " + path);
                Set(effect, "modifiers", modifiers);
                effects.Add(guid, effect);
            }
        }
        catalog = new RunTraitCatalog();
        foreach (string character in new[] { "Hacker", "Cyborg" })
        foreach (string path in Directory.GetFiles("Assets/Data/RunTraits/" + character, "*.asset"))
        {
            string text = File.ReadAllText(path);
            string effectPart = text.Substring(text.IndexOf("  effect:", StringComparison.Ordinal));
            var definition = new RunTraitDefinition {
                traitId = Field(text, "traitId"), targetCharacter = (CharacterType)int.Parse(Field(text, "targetCharacter")),
                tier = (RunTraitTier)int.Parse(Field(text, "tier")), maxStacks = int.Parse(Field(text, "maxStacks")),
                requiresWeaponTag = Field(text, "requiresWeaponTag") == "1",
                requiredTag = (SynergyTag)int.Parse(Field(text, "requiredTag")),
                effect = Regex.Matches(effectPart, @"guid: (\w+)").Select(m => effects[m.Groups[1].Value]).ToArray()
            };
            traits.Add(definition.traitId, definition);
            catalog.traits.Add(definition);
        }
        Assert(traits.Count == 30, "30 definitions");
        Assert(traits.Values.Count(t => t.effect.Length > 0) == 30, "all 30 traits linked");
        Assert(traits.Values.Where(t => t.tier == RunTraitTier.Platinum)
            .SelectMany(t => t.effect).OfType<SpecialTraitEffect>().Select(e => e.kind).Distinct().Count() == 10,
            "ten different platinum special effects");
        foreach (string character in new[] { "Hacker", "Cyborg" })
        foreach (string path in Directory.GetFiles("Assets/Data/Objects/Weapons/" + character, "*.asset"))
        {
            string hex = Field(File.ReadAllText(path), "synergyTags");
            var weapon = new WeaponBase();
            for (int i = 0; i < hex.Length; i += 8)
            {
                byte[] bytes = Enumerable.Range(0, 4).Select(b => Convert.ToByte(hex.Substring(i + b * 2, 2), 16)).ToArray();
                weapon.synergyTags.Add((SynergyTag)BitConverter.ToInt32(bytes, 0));
            }
            weapons.Add(Path.GetFileNameWithoutExtension(path), weapon);
        }
        Assert(weapons.Count == 20, "20 weapons");
    }
    private static RunTraitManager Run(CharacterType character)
    {
        var run = new RunTraitManager();
        Assert(run.EnsureInitialized(character, catalog), "initialized");
        return run;
    }
    private static void Acquire(RunTraitManager run, string id, int count)
    {
        for (int i = 0; i < count; i++) Assert(run.TryAcquire(traits[id]), "acquire " + id);
    }
    private static void Stat(RunTraitManager run, string weapon, RunTraitStat stat, float expected) =>
        Near(run.GetWeaponStatMultiplier(weapons[weapon], stat), expected, weapon + " " + stat);

    public static void Main()
    {
        LoadAssets();
        var hacker = Run(CharacterType.Hacker);
        Stat(hacker, "02_PlasmaRifle", RunTraitStat.AttackInterval, 1f);
        Acquire(hacker, "hacker_rapid_fire_circuit", 3);
        Stat(hacker, "02_PlasmaRifle", RunTraitStat.AttackInterval, .7f);
        Stat(hacker, "05_TacticalTurret", RunTraitStat.DeploymentCooldown, 1f);
        Acquire(hacker, "hacker_cycle_acceleration", 3);
        Stat(hacker, "02_PlasmaRifle", RunTraitStat.AttackInterval, .4f);
        Near(2f * hacker.GetWeaponStatMultiplier(weapons["02_PlasmaRifle"], RunTraitStat.AttackInterval), .8f, "2s becomes 0.8s");
        Acquire(hacker, "hacker_deployment_acceleration", 3);
        Stat(hacker, "05_TacticalTurret", RunTraitStat.DeploymentCooldown, .7f);
        Stat(hacker, "05_TacticalTurret", RunTraitStat.AttackInterval, .7f);
        Stat(hacker, "04_AIDrone", RunTraitStat.FollowSpeed, 1.3f);
        Stat(hacker, "04_AIDrone", RunTraitStat.AttackInterval, .7f);
        Stat(hacker, "01_LinkPistol", RunTraitStat.DeploymentCooldown, 1f);
        Acquire(hacker, "hacker_diffusion_node", 5);
        Stat(hacker, "02_PlasmaRifle", RunTraitStat.AttackRange, 1.5f);
        Stat(hacker, "02_PlasmaRifle", RunTraitStat.BeamWidth, 1.5f);
        Stat(hacker, "02_PlasmaRifle", RunTraitStat.ProjectileSpeed, 1f);
        Stat(hacker, "03_DataScrambler", RunTraitStat.AttackAngle, 1.5f);
        Stat(hacker, "07_DigitalShield", RunTraitStat.ShieldRadius, 1.5f);
        Stat(hacker, "10_EMPGrenade", RunTraitStat.ExplosionRadius, 1.5f);
        Near(hacker.GetWeaponDamageMultiplier(weapons["02_PlasmaRifle"]), 1f, "stat effects do not increase damage");
        Assert(!hacker.TryAcquire(traits["hacker_diffusion_node"]), "max stacks rejected");
        Assert(!hacker.TryAcquire(traits["cyborg_expanded_angle"]), "other character rejected");
        for (int i = 0; i < 10; i++) Stat(hacker, "02_PlasmaRifle", RunTraitStat.AttackInterval, .4f);
        var cyborg = Run(CharacterType.Cyborg);
        Acquire(cyborg, "cyborg_expanded_angle", 3);
        Stat(cyborg, "05_GravityHammer", RunTraitStat.AttackAngle, 1.3f);
        Stat(cyborg, "05_GravityHammer", RunTraitStat.AttackRange, 1f);
        Stat(cyborg, "02_SurgeBlade", RunTraitStat.ThrustRange, 1.3f);
        Stat(cyborg, "01_LaserSword", RunTraitStat.AttackRange, 1f);
        Acquire(cyborg, "cyborg_force_field_expansion", 5);
        Stat(cyborg, "05_GravityHammer", RunTraitStat.AttackRange, 1.5f);
        Stat(cyborg, "05_GravityHammer", RunTraitStat.AttackAngle, 1.3f);
        Stat(cyborg, "03_EnergyShield", RunTraitStat.OrbitRadius, 1.5f);
        Stat(cyborg, "07_TeslaCoilArmor", RunTraitStat.AuraRadius, 1.5f);
        Acquire(cyborg, "cyborg_drive_acceleration", 3);
        Stat(cyborg, "01_LaserSword", RunTraitStat.AttackInterval, .7f);
        Stat(cyborg, "09_ChainSaw", RunTraitStat.HitInterval, .7f);
        Acquire(cyborg, "cyborg_extended_range", 3);
        Stat(cyborg, "09_ChainSaw", RunTraitStat.MovementSpeed, 1.3f);
        Stat(cyborg, "08_BoltLauncher", RunTraitStat.ProjectileSpeed, 1.3f);
        Stat(cyborg, "08_BoltLauncher", RunTraitStat.AttackRange, 1.3f);
        Stat(cyborg, "10_BlastBreath", RunTraitStat.BreathWidth, 1.3f);
        Acquire(cyborg, "cyborg_force_field_acceleration", 3);
        Stat(cyborg, "03_EnergyShield", RunTraitStat.RotationSpeed, 1.3f);
        Stat(cyborg, "07_TeslaCoilArmor", RunTraitStat.HitInterval, .7f);
        Near(cyborg.GetWeaponStatMultiplier(null, RunTraitStat.AttackInterval), 1f, "missing source");
        hacker.InitializeRun(CharacterType.Hacker);
        Stat(hacker, "02_PlasmaRifle", RunTraitStat.AttackInterval, 1f);
        Stat(hacker, "02_PlasmaRifle", RunTraitStat.AttackRange, 1f);
        Stat(cyborg, "03_EnergyShield", RunTraitStat.RotationSpeed, 1.3f);

        // 한 특성에 여러 효과 SO와 빈 슬롯을 넣어도 각각의 계산이 보존됩니다.
        var largeReduction = new WeaponStatModifierEffect();
        Set(largeReduction, "modifiers", new[] { new WeaponStatModifierEffect.Modifier {
            stat = RunTraitStat.AttackInterval, percentPerStack = -.5f } });
        var composite = new RunTraitDefinition { traitId = "validation_composite", isCommonTrait = true,
            maxStacks = 5, effect = new RunTraitEffect[] { new DamageBonusEffect(), null, largeReduction } };
        catalog.traits.Add(composite);
        hacker.InitializeRun(CharacterType.Hacker);
        Assert(hacker.TryAcquire(composite), "composite acquire");
        Near(hacker.GetWeaponDamageMultiplier(weapons["01_LinkPistol"]), 1.1f, "damage effect preserved");
        Stat(hacker, "01_LinkPistol", RunTraitStat.AttackInterval, .5f);
        Assert(hacker.TryAcquire(composite), "composite second stack");
        Stat(hacker, "01_LinkPistol", RunTraitStat.AttackInterval, .1f);
        Near(hacker.GetWeaponDamageMultiplier(weapons["01_LinkPistol"]), 1.2f, "damage stacks additive");
        CheckStatusEffects();
        CheckPlatinumEffects();
        CheckBossEffects();
        CheckDebugAcquisition();
        CheckDebugRemoval();
        Console.WriteLine($"PASS: {checks} checks using actual effect assets and runtime calculation sources. Unity API shim; Play Mode not exercised.");
    }

    private static void Invoke(object target, string method) => target.GetType()
        .GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(target, null);

    private static void CheckDebugAcquisition()
    {
        var run = Run(CharacterType.Hacker);
        int version = run.RunVersion;
        int events = 0;
        run.OnTraitAcquired += (_, __) => events++;
        var silver = traits["hacker_aiming_link"];
        Assert(RunTraitDebugActions.Acquire(run, silver, false) == 1, "debug +1 uses normal acquisition");
        Near(run.GetWeaponDamageMultiplier(weapons["02_PlasmaRifle"]), 1.1f, "debug acquisition changes actual damage multiplier");
        Assert(RunTraitDebugActions.Acquire(run, silver, true) == 4, "debug maximum adds only four remaining silver stacks");
        Assert(run.GetTraitLevel(silver.traitId) == 5 && events == 5, "debug stack acquisition emits normal events");
        Near(run.GetWeaponDamageMultiplier(weapons["02_PlasmaRifle"]), 1.5f, "debug maximum applies all silver stacks");
        Assert(RunTraitDebugActions.Acquire(run, silver, false) == 0, "debug +1 cannot exceed cap");
        Assert(RunTraitDebugActions.Acquire(run, silver, true) == 0 && events == 5, "debug maximum is idempotent at cap");
        var gold = traits["hacker_rapid_fire_circuit"];
        Assert(RunTraitDebugActions.Acquire(run, gold, true) == 3, "debug gold maximum");
        Stat(run, "02_PlasmaRifle", RunTraitStat.AttackInterval, .7f);
        var platinum = traits["hacker_echo"];
        Assert(RunTraitDebugActions.Acquire(run, platinum, true) == 1, "debug platinum maximum is one");
        Assert(run.GetSpecialEffect(weapons["02_PlasmaRifle"], RunTraitSpecialKind.Echo) != null, "debug platinum enables actual special effect lookup");
        Assert(RunTraitDebugActions.Acquire(run, platinum, true) == 0, "debug cannot reacquire platinum");
        Assert(RunTraitDebugActions.Acquire(run, traits["cyborg_afterimage_attack"], true) == 0, "debug rejects other character");
        Assert(RunTraitDebugActions.Acquire(null, silver, true) == 0, "debug missing manager");
        Assert(RunTraitDebugActions.Acquire(new RunTraitManager(), silver, true) == 0, "debug uninitialized manager");
        Assert(RunTraitDebugActions.Acquire(run, null, true) == 0, "debug missing trait");
        Assert(RunTraitDebugActions.Acquire(run, new RunTraitDefinition(), true) == 0, "debug empty trait id");
        var outsider = new RunTraitDefinition { traitId = "debug_outside_catalog", maxStacks = 5 };
        Assert(RunTraitDebugActions.Acquire(run, outsider, true) == 0, "debug cannot add outside catalog");
        Assert(run.RunVersion == version && run.GetTraitLevel(silver.traitId) == 5, "debug acquisition preserves the current run and other traits");
        Assert(events == 9, "only successful debug stacks emit acquisition events");
        Near(silver.effect[0].GetDamageBonus(1), .1f, "debug acquisition does not modify shared effect SO");
        var offers = run.GenerateOffers(100);
        Assert(offers.All(t => t != silver && t != gold && t != platinum), "debug acquisition consumes the same vending pool copies");
    }

    private static void CheckDebugRemoval()
    {
        var run = Run(CharacterType.Hacker); Activate(run);
        int version = run.RunVersion;
        int acquisitions = 0, changes = 0;
        run.OnTraitAcquired += (_, __) => acquisitions++;
        run.OnTraitsChanged += () => changes++;
        var silver = traits["hacker_aiming_link"];
        var gold = traits["hacker_rapid_fire_circuit"];
        var platinum = traits["hacker_echo"];
        RunTraitDebugActions.Acquire(run, silver, true);
        RunTraitDebugActions.Acquire(run, gold, true);
        RunTraitDebugActions.Acquire(run, platinum, true);
        int before = acquisitions;
        Assert(RunTraitDebugActions.Remove(run, silver), "debug removes one silver stack");
        Assert(run.GetTraitLevel(silver.traitId) == 4, "remove decrements exactly one stack");
        Near(run.GetWeaponDamageMultiplier(weapons["02_PlasmaRifle"]), 1.4f, "damage changes immediately on removal");
        Assert(run.GetTraitLevel(gold.traitId) == 3 && run.RunVersion == version, "remove preserves other traits and current run");
        Assert(acquisitions == before && changes == before + 1, "remove emits change without an acquisition event");
        Assert(run.GenerateOffers(100).Contains(silver), "removed silver returns to vending pool");
        Assert(RunTraitDebugActions.Acquire(run, silver, true) == 1, "restored silver can be reacquired exactly once");
        Assert(RunTraitDebugActions.Remove(run, platinum), "debug removes platinum");
        Assert(!run.HasEffect(platinum.effect[0]) && run.GetSpecialEffect(weapons["02_PlasmaRifle"], RunTraitSpecialKind.Echo) == null,
            "removed platinum no longer grants effect");
        Assert(!RunTraitDebugActions.Remove(run, platinum), "cannot remove absent platinum");
        Assert(RunTraitDebugActions.Acquire(run, platinum, false) == 1, "removed platinum can be reacquired");
        var impostor = new RunTraitDefinition { traitId = silver.traitId, maxStacks = 5 };
        int oldChanges = changes;
        Assert(!RunTraitDebugActions.Remove(run, impostor), "removal rejects foreign SO with same id");
        Assert(!RunTraitDebugActions.Remove(run, traits["cyborg_afterimage_attack"]), "removal rejects other character");
        Assert(!RunTraitDebugActions.Remove(run, null) && !RunTraitDebugActions.Remove(null, silver), "removal handles null arguments");
        Assert(!RunTraitDebugActions.Remove(new RunTraitManager(), silver), "removal rejects uninitialized manager");
        Assert(changes == oldChanges, "failed removals do not notify listeners");
        Assert(RunTraitDebugActions.Reset(run) == 9, "reset reports all nine acquired stacks");
        Assert(run.GetTraitLevel(silver.traitId) == 0 && run.GetTraitLevel(gold.traitId) == 0
            && run.GetTraitLevel(platinum.traitId) == 0, "reset clears every tier");
        Near(run.GetWeaponDamageMultiplier(weapons["02_PlasmaRifle"]), 1f, "reset restores base damage");
        Stat(run, "02_PlasmaRifle", RunTraitStat.AttackInterval, 1f);
        Assert(run.RunVersion == version + 1 && run.IsInitialized, "reset invalidates old run callbacks while retaining initialized catalog");
        var offers = run.GenerateOffers(100);
        Assert(offers.Contains(silver) && offers.Contains(gold) && offers.Contains(platinum), "reset restores exhausted tier candidates");
        Assert(RunTraitDebugActions.Reset(run) == 0 && run.RunVersion == version + 1, "empty reset is idempotent");
        Assert(RunTraitDebugActions.Reset(null) == 0 && RunTraitDebugActions.Reset(new RunTraitManager()) == 0, "reset missing or uninitialized manager");
        for (int i = 0; i < 3; i++)
        {
            Assert(RunTraitDebugActions.Acquire(run, silver, true) == 5, "repeated cycle recovers full stack allowance");
            for (int j = 0; j < 5; j++) Assert(RunTraitDebugActions.Remove(run, silver), "repeated cycle removes each stack");
            Assert(!RunTraitDebugActions.Remove(run, silver), "level cannot underflow");
            Near(run.GetWeaponDamageMultiplier(weapons["02_PlasmaRifle"]), 1f, "no stale effect after final removal");
        }
        Near(silver.effect[0].GetDamageBonus(1), .1f, "removal does not mutate shared SO config");

        var cloneTrait = traits.Values.Single(t => t.effect.OfType<SpecialTraitEffect>().Any(e => e.kind == RunTraitSpecialKind.DeploymentClone));
        RunTraitDebugActions.Acquire(run, cloneTrait, false);
        var effect = (SpecialTraitEffect)cloneTrait.effect.Single();
        var clone = new UnityEngine.GameObject();
        RunTraitCloneLifetime.Track(clone, effect);
        RunTraitDebugActions.Acquire(run, silver, false);
        Assert(clone.activeSelf, "unrelated trait change retains clone");
        RunTraitDebugActions.Remove(run, cloneTrait);
        Assert(!clone.activeSelf, "clone stops immediately when owning trait removed");
        RunTraitDebugActions.Acquire(run, cloneTrait, false);
        var nextClone = new UnityEngine.GameObject(); RunTraitCloneLifetime.Track(nextClone, effect);
        RunTraitDebugActions.Reset(run);
        Assert(!nextClone.activeSelf, "reset stops trait clones");

        var virus = traits["hacker_virus_spread"];
        RunTraitDebugActions.Acquire(run, virus, false);
        var enemy = Healthy<Enemy>();
        int transfers = 0;
        var expiry = RunTraitControl.Expiry(enemy, weapons["10_EMPGrenade"], 3f, (_, __) => transfers++);
        RunTraitDebugActions.Remove(run, virus);
        expiry();
        Assert(transfers == 0, "removed virus cannot spread pending native CC on expiry");
        RunTraitDebugActions.Acquire(run, traits["hacker_cascade"], false);
        RunTraitAmplificationReceiver.Record(enemy, weapons["08_DataOptimization"], 1.4f, 5f, transferred: true);
        var receiver = enemy.GetComponent<RunTraitAmplificationReceiver>();
        Near(receiver.TransferredMultiplier(weapons["02_PlasmaRifle"]), 1.4f, "cascade active before removal");
        RunTraitDebugActions.Remove(run, traits["hacker_cascade"]);
        Near(receiver.TransferredMultiplier(weapons["02_PlasmaRifle"]), 1f, "removal clears transferred amplification");
        RunTraitDebugActions.Acquire(run, traits["hacker_cascade"], false);
        Near(receiver.TransferredMultiplier(weapons["02_PlasmaRifle"]), 1f, "reacquiring cascade does not revive old transfer");

        var cyborg = Run(CharacterType.Cyborg); Activate(cyborg);
        var absorptionTrait = traits.Values.Single(t => t.effect.OfType<SpecialTraitEffect>().Any(e => e.kind == RunTraitSpecialKind.ForceFieldAbsorption));
        RunTraitDebugActions.Acquire(cyborg, absorptionTrait, false);
        var shield = Healthy<Player>().gameObject.AddComponent<RunTraitShield>();
        shield.Charge(100f, (SpecialTraitEffect)absorptionTrait.effect.Single());
        Near(shield.Current, 10f, "shield charged before removal");
        RunTraitDebugActions.Remove(cyborg, absorptionTrait);
        Near(shield.Current, 0f, "owning trait removal clears charged shield immediately");
        RunTraitDebugActions.Acquire(cyborg, absorptionTrait, false);
        Near(shield.Current, 0f, "reacquire does not revive old shield");
        shield.Charge(100f, (SpecialTraitEffect)absorptionTrait.effect.Single());
        RunTraitDebugActions.Reset(cyborg);
        Near(shield.Current, 0f, "full reset clears shield immediately");
    }

    private static T Healthy<T>(float hp = 100f) where T : Character, new()
    {
        var character = new UnityEngine.GameObject().AddComponent<T>();
        Set(character, "maxHP", new Stat(hp));
        Set(character, "currentHP", new Stat(hp));
        return character;
    }

    private static void Activate(RunTraitManager run)
    {
        if (RunTraitManager.Instance != null) Invoke(RunTraitManager.Instance, "OnDestroy");
        Invoke(run, "Awake");
    }

    private static void CheckBossEffects()
    {
        var run = Run(CharacterType.Hacker); Activate(run);
        var boss = Healthy<Boss>();
        Assert(RunTraitTarget.Find(boss) == boss && RunTraitTarget.Find(boss.gameObject) == boss, "native Boss resolves as combat target");
        Assert(RunTraitTarget.Find(Healthy<Enemy>()) != null, "ordinary enemy remains supported");
        Assert(RunTraitTarget.Find(Healthy<Player>()) == null, "player excluded from combatant resolver");
        Assert(RunTraitTarget.Find(Healthy<TaggedObstacle>()) == null, "tagged obstacle excluded from combatant resolver");
        var child = new UnityEngine.Component { ParentComponent = boss };
        Assert(RunTraitTarget.Find(child) == boss, "boss child collider resolves to root");
        var rifle = weapons["02_PlasmaRifle"];
        boss.TakeDamage(10f, rifle); Near(boss.CurrentHP.CurrentValue, 90f, "boss base damage");
        Acquire(run, "hacker_aiming_link", 5);
        boss.TakeDamage(10f, rifle); Near(boss.CurrentHP.CurrentValue, 75f, "boss receives shooting bonus");
        Assert(RunTraitSpecialRuntime.LastDamaged == boss, "native Character dispatches boss to platinum hit runtime");
        Near(RunTraitSpecialRuntime.LastRawDamage, 10f, "boss hit preserves original damage for ricochet");
        Near(RunTraitSpecialRuntime.LastActualDamage, 15f, "boss hit reports actual modified damage");
        boss.TakeDamage(10f, weapons["03_DataScrambler"]);
        Near(boss.CurrentHP.CurrentValue, 65f, "non-shooting boss damage not changed by aiming link");
        using (new RunTraitDamageContext.Scope(secondary: true)) boss.TakeDamage(5f, rifle);
        Near(boss.CurrentHP.CurrentValue, 57.5f, "boss secondary half damage receives modifier exactly once");
        var obstacle = Healthy<TaggedObstacle>(); obstacle.TakeDamage(10f, rifle);
        Near(obstacle.CurrentHP.CurrentValue, 90f, "trait bonus not applied to non-combatant Character");

        Acquire(run, "hacker_virus_spread", 1); Acquire(run, "hacker_cascade", 1);
        var enemy = Healthy<Enemy>();
        var bossTwo = Healthy<Boss>();
        UnityEngine.Time.time = 100f;
        RunTraitSpecialRuntime.Candidate = enemy;
        RunTraitControl.Apply(boss.gameObject, new NeoSurvive.Buff.StunDebuff(4f), weapons["10_EMPGrenade"]);
        Assert(!boss.CanMove && !boss.CanAttack, "stun blocks native boss movement and attack");
        UnityEngine.Time.deltaTime = 4f; Invoke(boss.GetComponent<NeoSurvive.Buff.BuffHandler>(), "Update");
        Assert(boss.CanMove && boss.CanAttack, "boss resumes after actual CC expiry");
        Assert(enemy.GetComponent<NeoSurvive.Buff.StatusFlags>().attackBlocked, "boss CC spreads to ordinary enemy");
        UnityEngine.Time.deltaTime = 2f; Invoke(enemy.GetComponent<NeoSurvive.Buff.BuffHandler>(), "Update");
        Assert(!enemy.GetComponent<NeoSurvive.Buff.StatusFlags>().attackBlocked, "boss-origin CC half duration");

        RunTraitSpecialRuntime.Candidate = bossTwo;
        RunTraitControl.Apply(enemy.gameObject, new NeoSurvive.Buff.RootDebuff(2f), weapons["09_HologramDecoy"]);
        Invoke(enemy.GetComponent<NeoSurvive.Buff.BuffHandler>(), "Update");
        Assert(!bossTwo.CanMove && bossTwo.CanAttack, "enemy CC can transfer root to boss without blocking attack");
        UnityEngine.Time.deltaTime = 1f; Invoke(bossTwo.GetComponent<NeoSurvive.Buff.BuffHandler>(), "Update");
        Assert(bossTwo.CanMove, "boss transferred root expires after one second");
        RunTraitControl.Apply(boss.gameObject, new NeoSurvive.Buff.FrenzyDebuff(2f), weapons["03_DataScrambler"]);
        Assert(boss.TraitStatus.frenzy, "confusion applies to native boss flags");
        UnityEngine.Time.deltaTime = 2f; Invoke(boss.GetComponent<NeoSurvive.Buff.BuffHandler>(), "Update");
        Assert(bossTwo.TraitStatus.frenzy, "confusion transfers boss to boss");

        var slow = boss.gameObject.AddComponent<RunTraitSlowReceiver>();
        UnityEngine.Time.time = 110f; slow.Refresh(9, .4f, 2f);
        Near(boss.MovementMultiplier, .4f, "native boss reads slow strength");
        UnityEngine.Time.time = 112f; Invoke(slow, "Update");
        Near(boss.MovementMultiplier, 1f, "native boss speed restored after slow expires");
        var stagger = boss.gameObject.AddComponent<RunTraitStaggerReceiver>();
        stagger.TryStagger(.12f, .5f); Assert(!boss.CanMove && !boss.CanAttack, "stagger blocks native boss independently");
        UnityEngine.Time.time = 112.2f; Assert(boss.CanMove && boss.CanAttack, "native boss stagger expires");
        var knockback = boss.gameObject.AddComponent<NeoSurvive.Buff.KnockbackDebuff>();
        knockback.IsKnockbackActive = true; Assert(boss.IsKnockedBack && !boss.CanAttack, "boss does not run attacks through forced knockback");
        knockback.IsKnockbackActive = false; Assert(boss.CanAttack, "boss attack resumes after knockback");

        var amplifier = weapons["08_DataOptimization"];
        RunTraitAmplificationReceiver.Record(boss, amplifier, 1.7f, 5f);
        RunTraitSpecialRuntime.Candidate = enemy;
        boss.TakeDamage(10000f, rifle);
        Assert(boss.IsDead, "native Boss.Die invoked by lethal modified damage");
        Near(RunTraitSpecialRuntime.LastActualDamage, 57.5f, "boss overkill excluded from reported absorption damage");
        Near(enemy.GetComponent<RunTraitAmplificationReceiver>().TransferredMultiplier(rifle), 1.35f, "boss death cascades amplification to enemy");

        var originalEnemy = Healthy<Enemy>();
        RunTraitAmplificationReceiver.Record(originalEnemy, amplifier, 1.4f, 5f);
        RunTraitSpecialRuntime.Candidate = bossTwo;
        originalEnemy.TakeDamage(1000f, rifle);
        Near(bossTwo.GetComponent<RunTraitAmplificationReceiver>().TransferredMultiplier(rifle), 1.2f, "enemy death cascades amplification to boss");
        bossTwo.TakeDamage(10f, weapons["03_DataScrambler"]);
        Near(bossTwo.CurrentHP.CurrentValue, 88f, "native boss damage applies transferred amplification");
        UnityEngine.Time.time += 3f;
        bossTwo.TakeDamage(10f, weapons["03_DataScrambler"]);
        Near(bossTwo.CurrentHP.CurrentValue, 78f, "transferred boss amplification expires");
        run.InitializeRun(CharacterType.Hacker);
        var freshBoss = Healthy<Boss>(); freshBoss.TakeDamage(10f, rifle);
        Near(freshBoss.CurrentHP.CurrentValue, 90f, "boss trait bonuses reset between runs");
        var cyborg = Run(CharacterType.Cyborg); Activate(cyborg);
        Acquire(cyborg, "cyborg_hydraulic_slash", 5); Acquire(cyborg, "cyborg_ballistic_amplification", 5);
        Acquire(cyborg, "cyborg_conductive_slow", 5); Acquire(cyborg, "cyborg_afterglow_retention", 3);
        Acquire(cyborg, "cyborg_impact_weight", 5);
        var cyborgBoss = Healthy<Boss>();
        cyborgBoss.TakeDamage(10f, weapons["01_LaserSword"]);
        cyborgBoss.TakeDamage(10f, weapons["04_PlasmaPhoton"]);
        Near(cyborgBoss.CurrentHP.CurrentValue, 70f, "boss receives melee and ranged trait damage");
        var childObject = new UnityEngine.GameObject { parent = cyborgBoss.gameObject };
        UnityEngine.Time.time = 300f;
        RunTraitStatus.ApplySlow(childObject, weapons["04_PlasmaPhoton"]);
        Near(cyborgBoss.MovementMultiplier, .5f, "actual on-hit energy slow applied to boss root");
        Assert(childObject.GetComponent<RunTraitSlowReceiver>() == null, "child collider does not own separate boss slow state");
        UnityEngine.Time.time = 301f; Invoke(cyborgBoss.GetComponent<RunTraitSlowReceiver>(), "Update");
        Near(cyborgBoss.MovementMultiplier, .5f, "afterglow duration still active at one second");
        UnityEngine.Time.time = 301.31f; Invoke(cyborgBoss.GetComponent<RunTraitSlowReceiver>(), "Update");
        Near(cyborgBoss.MovementMultiplier, 1f, "on-hit boss slow ends after extended duration");
        RunTraitStatus.ApplyKnockback(childObject, weapons["01_LaserSword"], UnityEngine.Vector2.zero);
        var physical = cyborgBoss.GetComponent<NeoSurvive.Buff.KnockbackDebuff>();
        Near(physical.LastVelocity.x, 6f, "boss impact-weight knockback strength");
        Near(physical.LastDuration, .15f, "boss knockback duration");
        Assert(cyborgBoss.IsKnockedBack && RunTraitChainKnockback.LastOrigin == cyborgBoss, "boss root receives physical chain origin");
        var shieldOwner = Healthy<Player>();
        var shield = shieldOwner.gameObject.AddComponent<RunTraitShield>();
        var absorption = (SpecialTraitEffect)traits["cyborg_force_field_absorption"].effect.Single();
        shield.Charge(200f, absorption);
        shieldOwner.TakeDamage(30f);
        Near(shieldOwner.CurrentHP.CurrentValue, 90f, "native Character damage consumes generated shield before HP");
        Near(shield.Current, 0f, "native Character damage consumes entire shield on overflow");
        CheckBossPatternControl();
    }

    private sealed class TestBossPattern : BossPatternBase
    {
        public override System.Collections.IEnumerator RunPatternCoroutine() => WaitForPatternSeconds(.5f);
        public System.Collections.IEnumerator MovementWait() => WaitForMovementReady();
        public void SetVelocity(UnityEngine.Vector2 velocity) => SetPatternVelocity(velocity);
        public bool CanHit(UnityEngine.Collider2D collider) => TryGetDamageTarget(collider, out _);
    }

    private static void CheckBossPatternControl()
    {
        var boss = Healthy<Boss>();
        var rb = boss.gameObject.AddComponent<UnityEngine.Rigidbody2D>();
        var pattern = new TestBossPattern();
        pattern.Initialize(boss, null, rb);
        UnityEngine.Time.time = 200f;
        UnityEngine.Time.deltaTime = .25f;
        var waiting = pattern.RunPatternCoroutine();
        Assert(waiting.MoveNext(), "native pattern wait starts");
        boss.TraitStatus.attackBlocked = true;
        for (int i = 0; i < 8; i++) Assert(waiting.MoveNext(), "CC does not consume native pattern wait time");
        boss.TraitStatus.attackBlocked = false;
        Assert(waiting.MoveNext() && !waiting.MoveNext(), "native pattern wait resumes from remaining duration");
        boss.TraitStatus.moveBlocked = true;
        var movement = pattern.MovementWait();
        Assert(movement.MoveNext(), "boss dash waits for root to clear");
        boss.TraitStatus.moveBlocked = false;
        Assert(!movement.MoveNext(), "boss dash resumes after root");
        boss.TraitStatus.moveSpeedMul = .4f;
        pattern.SetVelocity(new UnityEngine.Vector2(10f, 0f)); Near(rb.velocity.x, 4f, "native pattern velocity scaled by slow");
        pattern.SetVelocity(new UnityEngine.Vector2(10f, 0f)); Near(rb.velocity.x, 4f, "pattern slow not multiplied repeatedly");
        boss.TraitStatus.moveBlocked = true;
        pattern.SetVelocity(new UnityEngine.Vector2(10f, 0f)); Near(rb.velocity.x, 0f, "pattern cannot move through root");
        var knockback = boss.gameObject.AddComponent<NeoSurvive.Buff.KnockbackDebuff>();
        knockback.IsKnockbackActive = true; rb.velocity = new UnityEngine.Vector2(8f, 0f);
        pattern.SetVelocity(UnityEngine.Vector2.zero); Near(rb.velocity.x, 8f, "pattern stop does not cancel externally owned knockback");
        knockback.IsKnockbackActive = false; boss.TraitStatus.moveBlocked = false;
        var player = Healthy<Player>(); var enemy = Healthy<Enemy>();
        var playerCollider = player.gameObject.AddComponent<UnityEngine.Collider2D>();
        var enemyCollider = enemy.gameObject.AddComponent<UnityEngine.Collider2D>();
        Assert(pattern.CanHit(playerCollider) && !pattern.CanHit(enemyCollider), "native boss pattern targets player normally");
        boss.TraitStatus.frenzy = true;
        Assert(!pattern.CanHit(playerCollider) && pattern.CanHit(enemyCollider), "confused native boss pattern targets enemies");
        boss.TraitStatus.attackBlocked = true;
        Assert(!pattern.CanHit(enemyCollider), "stunned confused boss cannot damage enemies either");
        Assert(RunTraitTarget.CanBossDamage(boss, player, false), "normal thrown trash can damage player");
        Assert(!RunTraitTarget.CanBossDamage(boss, player, true), "confused thrown trash cannot damage player");
        Assert(RunTraitTarget.CanBossDamage(boss, enemy, true), "confused thrown trash can damage enemy");
        Assert(!RunTraitTarget.CanBossDamage(boss, boss, true), "boss cannot damage itself with confused throw");
    }

    private static void CheckPlatinumEffects()
    {
        var hacker = Run(CharacterType.Hacker);
        var cyborg = Run(CharacterType.Cyborg);
        string[] hackerIds = { "hacker_ricochet_protocol", "hacker_echo", "hacker_virus_spread", "hacker_cascade", "hacker_bandwidth_expansion" };
        string[] cyborgIds = { "cyborg_afterimage_attack", "cyborg_projectile_erasure", "cyborg_chain_knockback", "cyborg_stagger_conduction", "cyborg_force_field_absorption" };
        foreach (var id in hackerIds) { Acquire(hacker, id, 1); Assert(!hacker.TryAcquire(traits[id]), "platinum one acquisition " + id); }
        foreach (var id in cyborgIds) { Acquire(cyborg, id, 1); Assert(!cyborg.TryAcquire(traits[id]), "platinum one acquisition " + id); }
        foreach (var trait in traits.Values.Where(t => t.tier == RunTraitTier.Platinum))
        {
            var run = trait.targetCharacter == CharacterType.Hacker ? hacker : cyborg;
            var special = (SpecialTraitEffect)trait.effect.Single();
            foreach (var weapon in weapons.Values)
                Assert((run.GetSpecialEffect(weapon, special.kind) != null) == weapon.synergyTags.Contains(trait.requiredTag),
                    "special tag filter " + trait.traitId);
            Assert(run.GetSpecialEffect(null, special.kind) == null, "null special source");
        }
        var ricochet = hacker.GetSpecialEffect(weapons["01_LinkPistol"], RunTraitSpecialKind.Ricochet);
        Near(ricochet.radius, 4f, "ricochet radius"); Near(ricochet.Scale(15f), 15f, "ricochet 100 percent");
        var echo = hacker.GetSpecialEffect(weapons["02_PlasmaRifle"], RunTraitSpecialKind.Echo);
        Near(echo.delay, .2f, "echo delay after end"); Near(echo.Scale(20f), 10f, "echo 50 percent");
        var virus = hacker.GetSpecialEffect(weapons["09_HologramDecoy"], RunTraitSpecialKind.VirusSpread);
        Near(virus.radius, 3f, "decoy CC spreads within three units"); Near(virus.Scale(4.5f), 2.25f, "transfer half final CC duration");
        var cascade = hacker.GetSpecialEffect(weapons["08_DataOptimization"], RunTraitSpecialKind.Cascade);
        Near(cascade.radius, 3.5f, "cascade radius"); Near(cascade.duration, 3f, "cascade fixed three seconds");
        Near(cascade.ScaleAmplification(1.2f), 1.1f, "20 percent becomes 10 percent");
        Near(cascade.ScaleAmplification(1.7f), 1.35f, "overlay included once in transfer");
        Near(cascade.ScaleAmplification(.8f), 1f, "transfer cannot turn amplification into damage reduction");
        var clone = hacker.GetSpecialEffect(weapons["05_TacticalTurret"], RunTraitSpecialKind.DeploymentClone);
        Near(clone.Scale(50f), 25f, "clone half performance");
        Near(clone.ScaleAmplification(1.7f), 1.35f, "clone retains base multiplier one");
        var chain = cyborg.GetSpecialEffect(weapons["09_ChainSaw"], RunTraitSpecialKind.ChainKnockback);
        Assert(chain.maxHops == 3, "chain propagation bounded");

        Assert(!RunTraitDamageContext.Secondary && !RunTraitDamageContext.ForceFieldContact, "initial damage scope");
        using (new RunTraitDamageContext.Scope(forceFieldContact: true))
        {
            Assert(RunTraitDamageContext.ForceFieldContact, "primary absorption allowed");
            using (new RunTraitDamageContext.Scope(secondary: true))
            {
                Assert(RunTraitDamageContext.Secondary && !RunTraitDamageContext.ForceFieldContact, "secondary cannot charge shield");
                using (new RunTraitDamageContext.Scope(forceFieldContact: true))
                    Assert(RunTraitDamageContext.Secondary && !RunTraitDamageContext.ForceFieldContact, "nested scopes cannot re-enable secondary procs");
            }
            Assert(!RunTraitDamageContext.Secondary && RunTraitDamageContext.ForceFieldContact, "scope restores parent origin");
        }
        try { using (new RunTraitDamageContext.Scope(deferRicochet: true)) { throw new InvalidOperationException(); } }
        catch (InvalidOperationException) { }
        Assert(!RunTraitDamageContext.Secondary && !RunTraitDamageContext.ForceFieldContact && !RunTraitDamageContext.DeferRicochet,
            "scope restores after exception");

        var absorption = cyborg.GetSpecialEffect(weapons["03_EnergyShield"], RunTraitSpecialKind.ForceFieldAbsorption);
        Near(absorption.ShieldGain(50f, 0f, 100f), 5f, "shield actual damage ratio");
        Near(absorption.ShieldGain(1000f, 15f, 100f), 5f, "shield gain capped");
        Near(absorption.ShieldGain(-5f, 0f, 100f), 0f, "negative damage cannot charge");
        // 실제 보호막/경직/버프 컴포넌트 소스를 실행하되 Unity 객체/시간만 대체합니다.
        var currentManager = RunTraitManager.Instance;
        if (currentManager != null) Invoke(currentManager, "OnDestroy");
        Invoke(cyborg, "Awake");
        UnityEngine.Time.time = 10f;
        var player = new UnityEngine.GameObject().AddComponent<Player>();
        var shield = player.gameObject.AddComponent<RunTraitShield>();
        shield.Charge(50f, absorption); Near(shield.Current, 5f, "runtime shield charge");
        shield.Charge(1000f, absorption); Near(shield.Current, 20f, "runtime shield cap");
        Near(shield.Absorb(7f), 0f, "shield absorbs first"); Near(shield.Current, 13f, "shield remainder");
        Near(shield.Absorb(20f), 7f, "only overflow damages HP"); Near(shield.Current, 0f, "shield cannot be negative");
        shield.Charge(100f, absorption);
        UnityEngine.Time.deltaTime = 1f;
        UnityEngine.Time.time = 12f; Invoke(shield, "Update"); Near(shield.Current, 10f, "shield decay delay");
        UnityEngine.Time.time = 13f; Invoke(shield, "Update"); Near(shield.Current, 5f, "shield decay rate");
        shield.Charge(1000f, absorption); player.MaxHP.BaseValue = 50f; Invoke(shield, "Update");
        Near(shield.Current, 10f, "HP reduction clamps shield cap");
        cyborg.InitializeRun(CharacterType.Cyborg); Near(shield.Absorb(6f), 6f, "new run clears shield immediately");
        var staggerObject = new UnityEngine.GameObject();
        var flags = staggerObject.AddComponent<NeoSurvive.Buff.StatusFlags>();
        var staggerReceiver = staggerObject.AddComponent<RunTraitStaggerReceiver>();
        var stagger = (SpecialTraitEffect)traits["cyborg_stagger_conduction"].effect.Single();
        UnityEngine.Time.time = 20f; staggerReceiver.TryStagger(stagger.duration, stagger.cooldown);
        Assert(flags.IsRunTraitStaggered, "stagger blocks independently");
        float end = flags.runTraitStaggerUntil;
        UnityEngine.Time.time = 20.1f; staggerReceiver.TryStagger(stagger.duration, stagger.cooldown);
        Near(flags.runTraitStaggerUntil, end, "rapid ticks do not extend stagger");
        flags.moveBlocked = true; UnityEngine.Time.time = 20.2f;
        Assert(!flags.IsRunTraitStaggered && flags.moveBlocked, "stagger expiry retains native root");
        UnityEngine.Time.time = 20.5f; staggerReceiver.TryStagger(stagger.duration, stagger.cooldown);
        Assert(flags.IsRunTraitStaggered, "stagger re-arms after cooldown");

        var target = new UnityEngine.GameObject();
        var buffs = target.AddComponent<NeoSurvive.Buff.BuffHandler>();
        int expired = 0;
        buffs.AddBuffWithId(1, new NeoSurvive.Buff.StunDebuff(1f), onExpired: () => expired++);
        buffs.AddBuffWithId(1, new NeoSurvive.Buff.StunDebuff(2f), onExpired: () => expired++);
        Assert(expired == 0, "CC refresh does not spread");
        buffs.AddBuffWithId(2, new NeoSurvive.Buff.RootDebuff(3f));
        UnityEngine.Time.deltaTime = 2f; Invoke(buffs, "Update");
        var buffFlags = target.GetComponent<NeoSurvive.Buff.StatusFlags>();
        Assert(expired == 1 && buffFlags.moveBlocked && !buffFlags.attackBlocked, "one true expiry and overlapping root retained");
        buffs.RemoveBuffById(2); Assert(expired == 1 && !buffFlags.moveBlocked, "explicit removal does not spread");
        var slow = target.AddComponent<RunTraitSlowReceiver>();
        UnityEngine.Time.time = 30f; slow.Refresh(1, .5f, 1f, () => expired++);
        slow.Refresh(1, .5f, 2f, () => expired++);
        UnityEngine.Time.time = 31f; Invoke(slow, "Update"); Assert(expired == 1, "slow refresh cancels old expiry");
        UnityEngine.Time.time = 32f; Invoke(slow, "Update"); Assert(expired == 2, "slow actual expiry once");
        slow.Refresh(1, .5f, 1f, () => expired++); Invoke(slow, "OnDisable");
        UnityEngine.Time.time = 34f; Invoke(slow, "Update"); Assert(expired == 2, "disable does not spread CC");
        hacker.InitializeRun(CharacterType.Hacker);
        Assert(hacker.GetSpecialEffect(weapons["02_PlasmaRifle"], RunTraitSpecialKind.Echo) == null, "new run clears special effect acquisition");
        Near(echo.efficiency, .5f, "shared SO retained after runtime and run reset");
        CheckPropagationRuntime();
    }

    private static void CheckPropagationRuntime()
    {
        var state = new RunTraitChainState(10, 3);
        Assert(!state.TryVisit(10) && state.Remaining == 3, "chain cannot revisit origin");
        Assert(state.TryVisit(11) && state.Remaining == 2, "first chain target");
        Assert(!state.TryVisit(11) && state.Remaining == 2, "duplicate colliders do not consume hop");
        Assert(state.TryVisit(12) && state.TryVisit(13) && !state.TryVisit(14), "chain total hop bound across branches");
        Assert(!new RunTraitChainState(1, -1).TryVisit(2), "invalid chain limit cannot propagate");

        var run = Run(CharacterType.Hacker);
        Invoke(RunTraitManager.Instance, "OnDestroy"); Invoke(run, "Awake");
        Acquire(run, "hacker_virus_spread", 1); Acquire(run, "hacker_cascade", 1);
        var source = weapons["10_EMPGrenade"];
        var first = new UnityEngine.GameObject().AddComponent<Enemy>();
        var second = new UnityEngine.GameObject().AddComponent<Enemy>();
        var third = new UnityEngine.GameObject().AddComponent<Enemy>();
        RunTraitSpecialRuntime.Candidate = second; RunTraitSpecialRuntime.Transfers = 0;
        RunTraitControl.Apply(first.gameObject, new NeoSurvive.Buff.StunDebuff(4.5f), source);
        UnityEngine.Time.deltaTime = 4.5f; Invoke(first.GetComponent<NeoSurvive.Buff.BuffHandler>(), "Update");
        Assert(second.GetComponent<NeoSurvive.Buff.StatusFlags>().attackBlocked, "actual CC expiration transfers stun");
        Assert(RunTraitSpecialRuntime.Transfers == 1, "one CC transfer");
        RunTraitSpecialRuntime.Candidate = third;
        var transferredBuffs = second.GetComponent<NeoSurvive.Buff.BuffHandler>();
        UnityEngine.Time.deltaTime = 2f; Invoke(transferredBuffs, "Update");
        Assert(second.GetComponent<NeoSurvive.Buff.StatusFlags>().attackBlocked, "half-duration transferred stun still active before 2.25s");
        UnityEngine.Time.deltaTime = .25f; Invoke(transferredBuffs, "Update");
        Assert(!second.GetComponent<NeoSurvive.Buff.StatusFlags>().attackBlocked && RunTraitSpecialRuntime.Transfers == 1,
            "transferred CC expires after half duration and does not spread");
        RunTraitControl.Apply(first.gameObject, new NeoSurvive.Buff.RootDebuff(2f), source);
        first.SetDead(true);
        UnityEngine.Time.deltaTime = 2f; Invoke(first.GetComponent<NeoSurvive.Buff.BuffHandler>(), "Update");
        Assert(RunTraitSpecialRuntime.Transfers == 1, "dead origin does not spread CC");
        first.SetDead(false);
        using (new RunTraitDamageContext.Scope(secondary: true))
            RunTraitControl.Apply(first.gameObject, new NeoSurvive.Buff.RootDebuff(2f), source);
        Invoke(first.GetComponent<NeoSurvive.Buff.BuffHandler>(), "Update");
        Assert(RunTraitSpecialRuntime.Transfers == 1, "echo CC does not trigger another platinum transfer");

        UnityEngine.Time.time = 50f;
        var amplifier = weapons["08_DataOptimization"];
        RunTraitAmplificationReceiver.Record(first, amplifier, 1.7f, 5f);
        var original = first.GetComponent<RunTraitAmplificationReceiver>();
        Near(original.TransferredMultiplier(weapons["02_PlasmaRifle"]), 1f, "original native amplification is metadata only");
        Assert(!RunTraitAmplificationReceiver.ShouldReplaceField(first, 1.35f), "weak clone does not replace stronger field");
        Assert(RunTraitAmplificationReceiver.ShouldReplaceField(first, 1.7f), "equal field refresh allowed");
        RunTraitSpecialRuntime.Candidate = second;
        original.OnKilled();
        var transferred = second.GetComponent<RunTraitAmplificationReceiver>();
        Near(transferred.TransferredMultiplier(weapons["02_PlasmaRifle"]), 1.35f, "actual cascade halves extra amplification");
        int transfers = RunTraitSpecialRuntime.Transfers;
        original.OnKilled(); Assert(RunTraitSpecialRuntime.Transfers == transfers, "one kill cannot cascade twice");
        RunTraitSpecialRuntime.Candidate = third; transferred.OnKilled();
        Assert(RunTraitSpecialRuntime.Transfers == transfers, "transferred amplification cannot cascade again");
        UnityEngine.Time.time = 53f;
        Near(transferred.TransferredMultiplier(weapons["02_PlasmaRifle"]), 1f, "cascade expires after exactly three seconds");
        RunTraitAmplificationReceiver.Record(third, weapons["01_LinkPistol"], 1.2f, 3f, hackerOnly: true, transferred: true);
        WeaponManager.AllowMarkedDamage = false;
        Near(third.GetComponent<RunTraitAmplificationReceiver>().TransferredMultiplier(weapons["01_LinkPistol"]), 1f, "transferred mark retains native source eligibility");
        WeaponManager.AllowMarkedDamage = true;
        Near(third.GetComponent<RunTraitAmplificationReceiver>().TransferredMultiplier(weapons["02_PlasmaRifle"]), 1.2f, "eligible incoming weapon gains transferred mark bonus");
        RunTraitControl.Apply(first.gameObject, new NeoSurvive.Buff.RootDebuff(2f), source);
        run.InitializeRun(CharacterType.Hacker);
        UnityEngine.Time.deltaTime = 2f; Invoke(first.GetComponent<NeoSurvive.Buff.BuffHandler>(), "Update");
        Assert(RunTraitSpecialRuntime.Transfers == transfers, "CC from previous run cannot transfer");
        Near(third.GetComponent<RunTraitAmplificationReceiver>().TransferredMultiplier(weapons["02_PlasmaRifle"]), 1f, "previous run amplification cleared");
    }

    private static void CheckStatusEffects()
    {
        var hacker = Run(CharacterType.Hacker);
        Acquire(hacker, "hacker_persistent_disruption", 5);
        Acquire(hacker, "hacker_deep_disruption", 3);
        Stat(hacker, "03_DataScrambler", RunTraitStat.ControlDuration, 1.5f);
        Near(hacker.GetWeaponStatBonus(weapons["03_DataScrambler"], RunTraitStat.SlowStrength), .3f, "30 slow percentage points");
        Near(hacker.GetHitSettings(weapons["10_EMPGrenade"]).slowDuration, 1f, "new slow duration from SO");
        Stat(hacker, "07_DigitalShield", RunTraitStat.KnockbackDistance, 1.3f);
        Stat(hacker, "01_LinkPistol", RunTraitStat.ControlDuration, 1f);
        Acquire(hacker, "hacker_overlay", 5);
        Acquire(hacker, "hacker_buffer_retention", 3);
        Acquire(hacker, "hacker_resident_process", 5);
        Near(hacker.GetWeaponStatBonus(weapons["01_LinkPistol"], RunTraitStat.AmplificationStrength), .5f, "overlay is percentage points");
        Stat(hacker, "08_DataOptimization", RunTraitStat.Lifetime, 1.8f);
        Stat(hacker, "08_DataOptimization", RunTraitStat.AmplificationDuration, 1.3f);
        Stat(hacker, "05_TacticalTurret", RunTraitStat.Lifetime, 1.5f);
        Stat(hacker, "06_EMPPulseGenerator", RunTraitStat.Lifetime, 1.5f);
        Stat(hacker, "04_AIDrone", RunTraitStat.DetectionRange, 1.5f);
        Stat(hacker, "09_HologramDecoy", RunTraitStat.DecoyHealth, 1.5f);
        Near(hacker.GetWeaponDamageMultiplier(weapons["08_DataOptimization"]), 1f, "overlay is not direct attack damage");
        Near(hacker.GetWeaponDamageMultiplier(weapons["05_TacticalTurret"]), 1f, "resident process is not direct attack damage");
        typeof(RunTraitManager).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(hacker, null);
        Near(RunTraitWeaponStats.Amplification(weapons["01_LinkPistol"], 1.2f), 1.7f, "20 percent plus 50 percentage points");
        Near(RunTraitWeaponStats.Amplification(weapons["02_PlasmaRifle"], 1.2f), 1.2f, "amplification source tag filter");
        Near(RunTraitWeaponStats.SlowMultiplier(.7f, .5f), .2f, "native slow plus energy slow");
        Near(RunTraitWeaponStats.SlowMultiplier(.7f, 2f), .1f, "slow capped at 90 percent");
        float velocity = RunTraitWeaponStats.KnockbackVelocity(12f, 1.3f, 1.5f);
        Near(velocity * (.25f * 1.5f), 12f * .25f * 1.3f, "CC time does not also multiply knockback distance");

        var cyborg = Run(CharacterType.Cyborg);
        Acquire(cyborg, "cyborg_conductive_slow", 5);
        Acquire(cyborg, "cyborg_afterglow_retention", 3);
        Acquire(cyborg, "cyborg_impact_weight", 5);
        Near(cyborg.GetWeaponStatBonus(weapons["04_PlasmaPhoton"], RunTraitStat.SlowStrength), .5f, "energy slow points");
        Near(cyborg.GetHitSettings(weapons["08_BoltLauncher"]).slowDuration, 1f, "new energy slow duration");
        Near(2.5f * cyborg.GetWeaponStatMultiplier(weapons["04_PlasmaPhoton"], RunTraitStat.ControlDuration), 3.25f, "afterglow native duration");
        var knockback = cyborg.GetHitSettings(weapons["01_LaserSword"]);
        Near(knockback.knockbackSpeed, 4f, "base knockback speed configurable");
        Near(knockback.knockbackDuration, .15f, "base knockback duration configurable");
        Stat(cyborg, "09_ChainSaw", RunTraitStat.KnockbackDistance, 1.5f);
        Near(cyborg.GetWeaponDamageMultiplier(weapons["04_PlasmaPhoton"]), 1f, "energy slow is not damage");

        var receiver = new RunTraitSlowReceiver();
        UnityEngine.Time.time = 0f;
        receiver.Refresh(1, .7f, 1f);
        var flags = receiver.GetComponent<NeoSurvive.Buff.StatusFlags>();
        Near(flags.moveSpeedMul, .7f, "slow receiver applied");
        UnityEngine.Time.time = .5f;
        receiver.Refresh(1, .6f, 1f);
        Near(flags.moveSpeedMul, .6f, "repeated same source refreshes without multiplication");
        receiver.Refresh(2, .2f, 3f);
        receiver.Refresh(1, .9f, 1f);
        Near(flags.moveSpeedMul, .2f, "weaker source does not overwrite stronger");
        receiver.Refresh(3, .01f, .25f);
        Near(flags.moveSpeedMul, .1f, "receiver slow cap");
        UnityEngine.Time.time = 1f;
        typeof(RunTraitSlowReceiver).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(receiver, null);
        Near(flags.moveSpeedMul, .2f, "expired strongest returns to next source");
        UnityEngine.Time.time = 4f;
        typeof(RunTraitSlowReceiver).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(receiver, null);
        Near(flags.moveSpeedMul, 1f, "all slows expire");
        Assert(!receiver.IsActive, "expired receiver inactive");
        flags.moveSpeedMul = .5f;
        receiver.Refresh(1, .4f, 2f);
        Near(flags.moveSpeedMul, .2f, "independent baseline modifier retained");
        typeof(RunTraitSlowReceiver).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(receiver, null);
        Near(flags.moveSpeedMul, .5f, "disable restores independent baseline");
        hacker.InitializeRun(CharacterType.Hacker);
        Near(RunTraitWeaponStats.Amplification(weapons["01_LinkPistol"], 1.2f), 1.2f, "new run resets amplification");
        Near(cyborg.GetHitSettings(weapons["01_LaserSword"]).knockbackSpeed, 4f, "shared SO unaffected by other run reset");
    }
}
