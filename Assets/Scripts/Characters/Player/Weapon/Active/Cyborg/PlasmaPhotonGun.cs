using UnityEngine;
using NeoSurvive.Buff;
using NeoSurvive.Core;

namespace NeoSurvive.Weapon
{
  public class PlasmaPhotonGun : MonoBehaviour
  {
    [Header("References")]
    public PlasmaPhotonBeam beamPrefab;

    [Header("VFX")]
    public GameObject beamVfxPrefab;
    public GameObject muzzleVfxPrefab;

    [Header("VFX Settings")]
    public float beamVfxDuration = 0.45f;
    public float beamVfxScaleX = 0.4f;
    public float beamVfxScaleY = 0.6f;
    public float muzzleVfxDuration = 0.2f;

    [Header("Fire Offset")]
    [SerializeField] private float fireOffset = 0.35f;

    [Header("Beam Start Offset")]
    [SerializeField] private float beamStartOffset = 0.35f;

    [Header("Fire")]
    public float fireInterval = 0.35f;
    public float range = 10f;

    private float timer;
    private StatusFlags ownerFlags;

    private const string weaponId = "plasmaphotongun";
    private int currentLevel = 1;

    private void Awake()
    {
      ownerFlags = GetComponentInParent<StatusFlags>();

      if (ownerFlags == null)
        ownerFlags = GetComponent<StatusFlags>();
    }

    private void Start()
    {
      ApplyLevel(1);
    }

    public void OnLevelUp(int level)
    {
      ApplyLevel(level);
      Debug.Log($"[PlasmaPhotonGun] Lv={currentLevel}, FireInterval={fireInterval}, Range={range}");
    }

    private void Update()
    {
      timer += Time.deltaTime;

      if (timer < fireInterval)
        return;

      timer = 0f;

      Transform target = FindClosestTarget();

      if (target == null)
        return;

      if (InGameSoundManager.Instance != null)
        InGameSoundManager.Instance.PlayPlasmaPhotonGunFire();

      Vector3 baseOrigin = transform.position;
      Vector2 dir = ((Vector2)target.position - (Vector2)baseOrigin).normalized;

      if (dir == Vector2.zero)
        return;

      Vector3 origin = baseOrigin + (Vector3)(dir * fireOffset);
      Vector3 beamStartPos = origin + (Vector3)(dir * beamStartOffset);

      float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
      Quaternion rot = Quaternion.Euler(0f, 0f, angle);

      if (muzzleVfxPrefab != null)
      {
        GameObject muzzle = Instantiate(muzzleVfxPrefab, origin, rot);
        Destroy(muzzle, muzzleVfxDuration);
      }

      if (beamVfxPrefab != null)
      {
        float beamLength = range * beamVfxScaleX;
        Vector3 vfxPos = beamStartPos + (Vector3)(dir * (beamLength * 0.5f));

        GameObject vfx = Instantiate(beamVfxPrefab, vfxPos, rot);

        vfx.transform.localScale = new Vector3(
          beamLength,
          beamVfxScaleY,
          1f
        );

        Destroy(vfx, beamVfxDuration);
      }

      PlasmaPhotonBeam beam = Instantiate(beamPrefab, beamStartPos, Quaternion.identity);

      float outMul = ownerFlags != null ? ownerFlags.outgoingDamageMul : 1f;
      var src = GetComponentInParent<WeaponSource>();

      beam.ApplyStatsFromCSV(currentLevel);

      beam.Fire(
        dir,
        range,
        outMul,
        GetComponentInParent<Player>(),
        src != null ? src.weaponData : null
      );
    }

    private void ApplyLevel(int level)
    {
      currentLevel = Mathf.Clamp(level, 1, 5);
      ApplyStatsFromCSV(currentLevel);
    }

    private void ApplyStatsFromCSV(int level)
    {
      if (WeaponStatLoader.DB == null)
      {
        Debug.LogWarning("[PlasmaPhotonGun] WeaponStatLoader.DB 없음");
        return;
      }

      if (!WeaponStatLoader.DB.rows.TryGetValue(weaponId, out var levelDict))
      {
        Debug.LogWarning($"[PlasmaPhotonGun] weaponId 없음: {weaponId}");
        return;
      }

      if (!levelDict.TryGetValue(level, out var row))
      {
        Debug.LogWarning($"[PlasmaPhotonGun] level 데이터 없음: {level}");
        return;
      }

      fireInterval = row.fireinterval;
      range = row.range;

      Debug.Log($"[PlasmaPhotonGun] CSV 적용 | Lv={level}, FireInterval={fireInterval}, Range={range}");
    }

    private Transform FindClosestTarget()
    {
      Vector3 origin = transform.position;

      Collider2D[] hits = Physics2D.OverlapCircleAll(origin, range);

      Transform closest = null;
      float closestDist = float.MaxValue;

      foreach (Collider2D hit in hits)
      {
        if (hit == null)
          continue;

        bool isEnemy =
          hit.CompareTag("Enemy") ||
          (hit.transform.parent != null && hit.transform.parent.CompareTag("Enemy"));

        IDamageable damageable = hit.GetComponent<IDamageable>();

        if (damageable == null)
          damageable = hit.GetComponentInParent<IDamageable>();

        if (!isEnemy && damageable == null)
          continue;

        Transform targetTransform = hit.transform;

        Character character = hit.GetComponentInParent<Character>();

        if (character != null)
          targetTransform = character.transform;
        else if (damageable is Component damageableComponent)
          targetTransform = damageableComponent.transform;

        float d = Vector2.Distance(origin, targetTransform.position);

        if (d < closestDist && d <= range)
        {
          closestDist = d;
          closest = targetTransform;
        }
      }

      return closest;
    }
  }
}