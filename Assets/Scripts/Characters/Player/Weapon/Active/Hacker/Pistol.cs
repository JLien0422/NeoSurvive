using UnityEngine;
using NeoSurvive.Core;
using NeoSurvive.Buff;

namespace NeoSurvive.Weapon
{
  /// <summary>
  /// 기본 에너지 권총
  /// </summary>
  public class Pistol : MonoBehaviour
  {
    public GameObject projectilePrefab;

    [Header("총알 설정")]
    public float damage = 5f;
    public float range = 10f;
    public float fireRate = 1f;

    private float fireTimer;

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

      GameObject target = FindClosestEnemy();
      Vector3 dir = target != null ? (target.transform.position - transform.position).normalized : Vector3.right;

      GameObject obj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
      obj.SetActive(true);
      Projectile p = obj.GetComponent<Projectile>();
      if (p != null) p.Initialize(dir, damage);
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
