using UnityEngine;

/// <summary>실제 잃은 적 HP로 충전되는 별도 보호막. 체력 회복과 구분합니다.</summary>
public class RunTraitShield : MonoBehaviour
{
    [SerializeField, Min(0f)] private float currentShield;
    public float Current { get => currentShield; private set => currentShield = value; }
    private SpecialTraitEffect settings;
    private float lastCharge;
    private int version;
    private RunTraitManager manager;

    public void Charge(float actualDamage, SpecialTraitEffect effect)
    {
        var player = GetComponent<Player>();
        if (player == null || player.IsDead || RunTraitManager.Instance == null) return;
        ValidateRun();
        settings = effect;
        Current += effect.ShieldGain(actualDamage, Current, player.MaxHP.GetValue());
        lastCharge = Time.time;
    }

    public float Absorb(float damage)
    {
        ValidateRun();
        float absorbed = Mathf.Min(Current, Mathf.Max(0f, damage));
        Current -= absorbed;
        return damage - absorbed;
    }

    private void ValidateRun()
    {
        if (manager == RunTraitManager.Instance && manager != null && version == manager.RunVersion) return;
        Current = 0f;
        if (manager != null) manager.OnTraitsChanged -= OnTraitsChanged;
        manager = RunTraitManager.Instance;
        version = manager != null ? manager.RunVersion : 0;
        if (manager != null) manager.OnTraitsChanged += OnTraitsChanged;
    }
    private void OnTraitsChanged()
    {
        if (manager == null || version != manager.RunVersion || !manager.HasEffect(settings))
            Current = 0f;
    }
    private void Update()
    {
        ValidateRun();
        var player = GetComponent<Player>();
        if (player == null || player.IsDead) { Current = 0f; return; }
        if (settings == null) return;
        Current = Mathf.Min(Current, player.MaxHP.GetValue() * settings.shieldMaxHealthRatio);
        if (Time.time >= lastCharge + settings.shieldDecayDelay)
            Current = Mathf.Max(0f, Current - settings.shieldDecayPerSecond * Time.deltaTime);
    }
    private void OnDisable()
    {
        Current = 0f;
        if (manager != null) manager.OnTraitsChanged -= OnTraitsChanged;
        manager = null;
    }
}
