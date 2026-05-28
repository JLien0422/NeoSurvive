using UnityEngine;
using NeoSurvive.Core;
using NeoSurvive.Buff;

namespace NeoSurvive.Weapon
{
  /// <summary>
  /// 기본 에너지 권총
  /// </summary>
  public class LinkPistol : MonoBehaviour
  {
    public GameObject projectilePrefab;

    [Header("총알 설정")]
    public float damage = 5f;
    public float range = 10f;
    public float fireRate = 1f;
    public float bulletSpeed = 20f;

    [Header("마스터 표식 표시")]
    [SerializeField] private Sprite masterMarkIcon;

    private float fireTimer;

    // ★ CSV 식별용 ID
    private readonly string weaponId = "linkpistol";

    // ★ 추가: 현재 무기 레벨 저장
    private int currentLevel = 1;

    // ★ 추가: Lv5 이상이면 마스터 효과 활성화
    private bool IsMasterLevel => currentLevel >= 5;

    private void Start()
    {
      // ★ 수정: 시작 시 Lv1 적용
      currentLevel = 1;
      ApplyStatsFromCSV(currentLevel);
    }

    // ★ 수정: 레벨업 시 현재 레벨 저장
    public void OnLevelUp(int level)
    {
      currentLevel = level;

      ApplyStatsFromCSV(level);

      Debug.Log($"[LinkPistol] CSV 적용 완료 | Lv={level}, Damage={damage}, Master={IsMasterLevel}");
    }

    // ★ CSV 적용
    private void ApplyStatsFromCSV(int level)
    {
      if (WeaponStatLoader.DB == null)
      {
        Debug.LogWarning("[LinkPistol] WeaponStatLoader.DB 없음");
        return;
      }

      if (!WeaponStatLoader.DB.rows.TryGetValue(weaponId, out var levelDict))
      {
        Debug.LogWarning($"[LinkPistol] weaponId 없음: {weaponId}");
        return;
      }

      if (!levelDict.TryGetValue(level, out var row))
      {
        Debug.LogWarning($"[LinkPistol] level 데이터 없음: {level}");
        return;
      }

      damage = row.damage;
      range = row.range;
      fireRate = row.firerate;
      bulletSpeed = row.bulletspeed;

      Debug.Log($"[LinkPistol] CSV 적용 | Lv={level}, Damage={damage}, Range={range}, FireRate={fireRate}");
    }

    private void Update()
    {
      fireTimer += Time.deltaTime;

      if (fireTimer >= fireRate)
      {
        Attack();
        fireTimer = 0f;
      }
    }

    private void Attack()
    {
      if (projectilePrefab == null) return;

      GameObject target = FindClosestTarget();
      if (target == null) return;

      Vector3 dir = (target.transform.position - transform.position).normalized;

      ExecuteAttack(dir);
    }

    /// <summary>
    /// 실제 발사체 생성
    /// </summary>
    public void ExecuteAttack(Vector3 direction)
    {
      if (projectilePrefab == null) return;

      if (InGameSoundManager.Instance != null)
      {
        InGameSoundManager.Instance.PlayLinkPistolAttack();
      }

      GameObject obj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
      obj.SetActive(true);

      Projectile p = obj.GetComponent<Projectile>();

      if (p != null)
      {
        p.Initialize(direction, damage, bulletSpeed);

        var src = GetComponentInParent<WeaponSource>();

        if (src != null)
          p.SetSourceWeapon(src.weaponData);
      }

      // =========================
      // ★ 추가: LinkPistol Lv5 마스터 효과
      // - 탄환 적중 시 Enemy에게 표식 부여
      // - Projectile.cs는 공용이므로 수정하지 않음
      // =========================
      if (IsMasterLevel)
      {
        LinkPistolMarkOnHit markOnHit = obj.GetComponent<LinkPistolMarkOnHit>();

        if (markOnHit == null)
        {
          markOnHit = obj.AddComponent<LinkPistolMarkOnHit>();
        }

        markOnHit.Init(5f, masterMarkIcon);
      }
    }

    private GameObject FindClosestTarget()
    {
      GameObject closest = null;
      float closestDistance = range > 0 ? range : 10f;

      GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

      foreach (GameObject enemy in enemies)
      {
        float distance = Vector3.Distance(transform.position, enemy.transform.position);

        if (distance < closestDistance)
        {
          closestDistance = distance;
          closest = enemy;
        }
      }

      // =========================
      // 추가: IDamageable 자판기/맵오브젝트 탐색
      // =========================
      MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);

      foreach (MonoBehaviour behaviour in behaviours)
      {
        if (behaviour == null)
          continue;

        if (behaviour.CompareTag("Enemy"))
          continue;

        IDamageable damageable = behaviour as IDamageable;

        if (damageable == null)
          continue;

        float distance = Vector3.Distance(transform.position, behaviour.transform.position);

        if (distance < closestDistance)
        {
          closestDistance = distance;
          closest = behaviour.gameObject;
        }
      }

      return closest;
    }
  }
}