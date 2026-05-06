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

    private float fireTimer;

    // ★ 추가: CSV 식별용 ID
    private readonly string weaponId = "linkpistol";

    private void Start()
    {
      // ★ 수정: 시작 시 Lv1 CSV 적용
      ApplyStatsFromCSV(1);
    }

    // ★ 수정: 레벨업 시 CSV 재적용
    public void OnLevelUp(int level)
    {
      ApplyStatsFromCSV(level);

      Debug.Log($"[LinkPistol] CSV 적용 완료 | Lv={level}, Damage={damage}");
    }

    // ★ 핵심: CSV 적용 함수
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

      // =========================
      // ★ 핵심 수정 부분
      // =========================

      // Lv1 기준 데미지 가져오기
      float baseDamage = row.damage;

      if (levelDict.TryGetValue(1, out var levelOneRow))
      {
        baseDamage = levelOneRow.damage;
      }

      // damageperlevel 가져오기
      float perLevel = row.damageperlevel;

      // 현재 레벨 값이 없으면 Lv1 값 사용
      if (perLevel <= 0f && levelDict.TryGetValue(1, out var baseRow))
      {
        perLevel = baseRow.damageperlevel;
      }

      // 최종 데미지 계산
      damage = baseDamage * (1f + (level - 1) * perLevel);

      // 나머지 값은 현재 레벨 CSV 그대로 사용
      range = row.range;
      fireRate = row.firerate;
      bulletSpeed = row.bulletspeed;

      Debug.Log($"[LinkPistol] CSV 적용 | Lv={level}, Base={baseDamage}, PerLevel={perLevel}, Final={damage}");
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

      GameObject obj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
      obj.SetActive(true);

      Projectile p = obj.GetComponent<Projectile>();
      if (p != null)
      {
        p.Initialize(direction, damage, bulletSpeed);

        var src = GetComponentInParent<WeaponSource>();
        if (src != null) p.SetSourceWeapon(src.weaponData);
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
      // Enemy가 더 가까우면 Enemy 우선 유지
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