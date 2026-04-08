using UnityEngine;
using NeoSurvive.Core;

namespace NeoSurvive.Weapon
{
  public class DeployedTurret : MonoBehaviour
  {
    private float damage;
    private float range;
    private float fireRate;
    [SerializeField]
    private GameObject projectilePrefab;

    private float fireTimer;

    // DPM 기록용 소스 무기
    private WeaponBase sourceWeapon;

    public void Initialize(float damage, float range, float fireRate, float lifeTime, GameObject projectilePrefab, WeaponBase weaponBase = null)
    {
      this.damage = damage;
      this.range = range;
      this.fireRate = fireRate;
      this.projectilePrefab = projectilePrefab;
      this.sourceWeapon = weaponBase;

      Destroy(gameObject, lifeTime);

      // Setup Visuals if missing
      SpriteRenderer sr = GetComponent<SpriteRenderer>();
      if (sr == null)
      {
        sr = gameObject.AddComponent<SpriteRenderer>();
        sr.color = Color.gray;
        sr.sortingOrder = 4;
      }
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

      Transform target = FindClosestEnemy();
      if (target == null) return;

      Vector3 dir = (target.position - transform.position).normalized;
      GameObject obj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
      if (obj.TryGetComponent<Projectile>(out var p))
      {
        p.Initialize(dir, damage, 20f);
        // 부모 무기에서 전달받은 sourceWeapon으로 DPM 기록
        if (sourceWeapon != null) p.SetSourceWeapon(sourceWeapon);
      }
    }

    private Transform FindClosestEnemy()
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
      return closest != null ? closest.transform : null;
    }

    private void OnDrawGizmos()
    {
      Gizmos.color = Color.red;
      Gizmos.DrawWireSphere(transform.position, range);
    }
  }
}
