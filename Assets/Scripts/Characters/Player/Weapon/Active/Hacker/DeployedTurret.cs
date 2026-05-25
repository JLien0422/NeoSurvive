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

    // ★ 추가: AutoTurret Lv5 마스터 여부
    private bool isMasterTurret;

    // ★ 추가: 마스터 포탑이 따라갈 플레이어
    private Transform player;

    // ★ 추가: 플레이어 주변 호위 위치
    private Vector3 followOffset;

    [Header("Master Move")]
    [SerializeField] private float followSpeed = 5f;
    [SerializeField] private float followDistance = 1.5f;

    // ★ 수정: isMaster 인자 추가
    public void Initialize(
      float damage,
      float range,
      float fireRate,
      float lifeTime,
      GameObject projectilePrefab,
      WeaponBase weaponBase = null,
      bool isMaster = false)
    {
      this.damage = damage;
      this.range = range;
      this.fireRate = fireRate;
      this.projectilePrefab = projectilePrefab;
      this.sourceWeapon = weaponBase;

      // ★ 추가: 마스터 여부 저장
      this.isMasterTurret = isMaster;

      // ★ 추가: 마스터 포탑이면 플레이어 추적 준비
      if (isMasterTurret)
      {
        GameObject playerObj = GameObject.FindWithTag("Player");

        if (playerObj != null)
        {
          player = playerObj.transform;
          followOffset = Random.insideUnitCircle.normalized * followDistance;
        }
      }

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
      // ★ 추가: Lv5 마스터 포탑이면 플레이어를 따라다님
      FollowPlayerIfMaster();

      fireTimer += Time.deltaTime;
      if (fireTimer >= fireRate)
      {
        Attack();
        fireTimer = 0f;
      }
    }

    // ★ 추가: 마스터 포탑 호위 이동
    private void FollowPlayerIfMaster()
    {
      if (!isMasterTurret)
        return;

      if (player == null)
      {
        GameObject playerObj = GameObject.FindWithTag("Player");

        if (playerObj != null)
          player = playerObj.transform;
      }

      if (player == null)
        return;

      Vector3 targetPos = player.position + followOffset;

      transform.position = Vector3.Lerp(
        transform.position,
        targetPos,
        Time.deltaTime * followSpeed
      );
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
        if (sourceWeapon != null)
          p.SetSourceWeapon(sourceWeapon);
      }
    }

    private Transform FindClosestEnemy()
    {
      // =========================
      // ★ 추가: LinkPistol 표식 대상 우선 공격
      // =========================
      Transform markedTarget = MarkedTarget.GetCurrentTarget();

      if (markedTarget != null)
      {
        float markedDistance = Vector3.Distance(transform.position, markedTarget.position);

        if (markedDistance <= range)
        {
          return markedTarget;
        }
      }

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