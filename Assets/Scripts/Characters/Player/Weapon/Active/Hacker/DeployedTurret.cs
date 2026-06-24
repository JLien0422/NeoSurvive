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
    [SerializeField]
    private Vector3 fireOffset = Vector3.zero;

    private float fireTimer;

    private WeaponBase sourceWeapon;

    private bool isMasterTurret;

    private Transform player;

    private Vector3 followOffset;
    private SpriteRenderer[] spriteRenderers;
    private int facingSign = 1;

    [Header("Master Move")]
    [SerializeField] private float followSpeed = 5f;
    [SerializeField] private float followDistance = 1.5f;

    private void Awake()
    {
      RefreshVisualRenderers();
    }

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

      this.isMasterTurret = isMaster;

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

      SpriteRenderer sr = GetComponent<SpriteRenderer>();
      if (sr == null)
      {
        sr = gameObject.AddComponent<SpriteRenderer>();
        sr.color = Color.gray;
        sr.sortingOrder = 4;
      }

      RefreshVisualRenderers();
    }

    private void Update()
    {
      FollowPlayerIfMaster();

      fireTimer += Time.deltaTime;
      if (fireTimer >= fireRate)
      {
        Attack();
        fireTimer = 0f;
      }
    }

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

      if (InGameSoundManager.Instance != null)
      {
        InGameSoundManager.Instance.PlayTacticalTurretAttack();
      }

      FaceTarget(target);

      Vector3 spawnPos = transform.position;
      if (fireOffset != Vector3.zero)
      {
        // 타겟 방향/회전된 터렛에 따라 위치 반전 적용
        spawnPos = transform.TransformPoint(fireOffset);
      }

      Vector3 dir = (target.position - spawnPos).normalized;
      GameObject obj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
      if (obj.TryGetComponent<Projectile>(out var p))
      {
        p.Initialize(dir, damage, 20f);

        if (sourceWeapon != null)
          p.SetSourceWeapon(sourceWeapon);
      }
    }

    private void FaceTarget(Transform attackTarget)
    {
      if (attackTarget == null) return;

      float deltaX = attackTarget.position.x - transform.position.x;
      if (!Mathf.Approximately(deltaX, 0f))
      {
        facingSign = deltaX < 0f ? -1 : 1;
      }

      if (spriteRenderers == null || spriteRenderers.Length == 0)
      {
        RefreshVisualRenderers();
      }

      if (spriteRenderers == null) return;

      for (int i = 0; i < spriteRenderers.Length; i++)
      {
        if (spriteRenderers[i] != null)
        {
          spriteRenderers[i].flipX = facingSign < 0;
        }
      }
    }

    private void RefreshVisualRenderers()
    {
      spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
    }

    private Transform FindClosestEnemy()
    {
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