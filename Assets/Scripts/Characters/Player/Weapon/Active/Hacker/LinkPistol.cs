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
    private float baseDamage;
    private Player owner;

    private void Start()
    {
      baseDamage = damage;
      owner = GetComponentInParent<Player>();
    }

    public void OnLevelUp(int level)
    {
      if (baseDamage == 0 && damage > 0) baseDamage = damage;

      // 레벨당 데미지 20% 증가
      damage = baseDamage * (1f + (level - 1) * 0.2f);

      Debug.Log($"[LinkPistol] Leveled Up! Lv.{level}, new Damage: {damage}");

      if (level >= 5)
      {
        // TODO: Lv.5 마스터 효과 (표식 및 집중 공격) 구현
      }
    }

    private void Update()
    {
      // 로컬 플레이어만 자동 공격 시도
      if (owner != null && !owner.IsLocal) return;

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

      GameObject target = FindClosestEnemy();
      if (target == null) return;
      Vector3 dir = (target.transform.position - transform.position).normalized;

      // [Coop] 서버에 공격 보고 (로컬 플레이어일 때만)
      var runtimeInfo = GetComponent<WeaponRuntimeInfo>();
      if (runtimeInfo != null)
      {
        runtimeInfo.ReportProjectile(transform.position, dir, bulletSpeed, range, 0, 0);
      }

      ExecuteAttack(dir);
    }

    /// <summary>
    /// 실제 발사체 생성 (로컬/원격 공용)
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
        var src = GetComponent<WeaponSource>();
        if (src != null) p.SetSourceWeapon(src.weaponData);
      }
    }

    private GameObject FindClosestEnemy()
    {
      GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
      GameObject closest = null;
      float closestDistance = range > 0 ? range : 10f;

      foreach (GameObject enemy in enemies)
      {
        float distance = Vector3.Distance(transform.position, enemy.transform.position);
        if (distance < closestDistance)
        {
          closestDistance = distance;
          closest = enemy;
        }
      }
      return closest;
    }
  }
}
