using UnityEngine;
using NeoSurvive.Core;

namespace NeoSurvive.Weapon
{
  public class AIDrone : MonoBehaviour
  {
    public Animator anim;
    public AnimationClip attackClip;
    public GameObject projectilePrefab;

    [Header("AI 드론 설정")]
    public float followDistance = 2f;
    public float followSpeed = 1f;
    public GameObject player;
    public Vector3 offset;
    public float detectionRange = 10f;
    public float attackSpeed = 5f;
    public float projectileSpeed = 20f;

    public float fireElapsed = 0f;
    public Transform target;
    public Vector2 fireOffset;

    public float damage = 5f;

    private readonly string weaponId = "aidrone";
    private const int maxLevel = 5;

    private int currentLevel = 1;
    private bool isSubDrone = false;

    private System.Collections.Generic.List<AIDrone> subDrones =
      new System.Collections.Generic.List<AIDrone>();

    private void Start()
    {
      player = GameObject.FindWithTag("Player");
      anim = GetComponent<Animator>();

      offset = Random.insideUnitCircle.normalized * followDistance;

      // ★ 메인 드론만 CSV 적용
      if (!isSubDrone)
      {
        ApplyStatsFromCSV(1);
      }

      UpdateAnimationSpeed();
    }

    public void OnLevelUp(int level)
    {
      // ★ 서브 드론은 레벨업 처리 금지
      if (isSubDrone)
        return;

      // ★ Lv5 이후는 더 이상 증가 금지
      int clampedLevel = Mathf.Clamp(level, 1, maxLevel);

      if (currentLevel >= maxLevel && clampedLevel >= maxLevel)
      {
        Debug.Log("[AIDrone] 이미 마스터 레벨입니다. 추가 레벨업 무시");
        return;
      }

      currentLevel = clampedLevel;

      ApplyStatsFromCSV(currentLevel);
      SyncSubDroneStats();
      UpdateSubDroneCount(currentLevel);

      Debug.Log($"[AIDrone] CSV 적용 | Lv={currentLevel}, Total Drones={1 + subDrones.Count}, Damage={damage}");
    }

    private void ApplyStatsFromCSV(int level)
    {
      if (WeaponStatLoader.DB == null)
      {
        Debug.LogWarning("[AIDrone] WeaponStatLoader.DB 없음");
        return;
      }

      if (!WeaponStatLoader.DB.rows.TryGetValue(weaponId, out var levelDict))
      {
        Debug.LogWarning($"[AIDrone] weaponId 없음: {weaponId}");
        return;
      }

      if (!levelDict.TryGetValue(level, out var row))
      {
        Debug.LogWarning($"[AIDrone] level 데이터 없음: {level}");
        return;
      }

      WeaponStatDB.Row levelOneRow = row;
      if (levelDict.TryGetValue(1, out var baseRow))
      {
        levelOneRow = baseRow;
      }

      float perLevel = row.damageperlevel > 0f
        ? row.damageperlevel
        : levelOneRow.damageperlevel;

      damage = levelOneRow.damage * (1f + (level - 1) * perLevel);

      followDistance = row.followdistance;
      followSpeed = row.followspeed;
      detectionRange = row.detectionrange;
      attackSpeed = row.attackspeed;
      projectileSpeed = row.projectilespeed;

      UpdateAnimationSpeed();
    }

    private int GetSubDroneCountFromCSV(int level)
    {
      if (WeaponStatLoader.DB == null)
        return Mathf.Clamp(level - 1, 0, 4);

      if (!WeaponStatLoader.DB.rows.TryGetValue(weaponId, out var levelDict))
        return Mathf.Clamp(level - 1, 0, 4);

      if (!levelDict.TryGetValue(level, out var row))
        return Mathf.Clamp(level - 1, 0, 4);

      return Mathf.Clamp(row.subdronecount, 0, 4);
    }

    private void UpdateSubDroneCount(int level)
    {
      int desiredSubCount = GetSubDroneCountFromCSV(level);

      for (int i = subDrones.Count - 1; i >= 0; i--)
      {
        if (subDrones[i] == null)
          subDrones.RemoveAt(i);
      }

      int currentSubCount = subDrones.Count;

      if (currentSubCount >= desiredSubCount)
        return;

      for (int i = 0; i < desiredSubCount - currentSubCount; i++)
      {
        GameObject clone = Instantiate(gameObject, transform.position, Quaternion.identity);

        if (clone.TryGetComponent<AIDrone>(out var cloneScript))
        {
          cloneScript.isSubDrone = true;
          cloneScript.ApplyStatsFromMainDrone(this);

          subDrones.Add(cloneScript);
        }
      }
    }

    private void SyncSubDroneStats()
    {
      for (int i = subDrones.Count - 1; i >= 0; i--)
      {
        if (subDrones[i] == null)
        {
          subDrones.RemoveAt(i);
          continue;
        }

        subDrones[i].ApplyStatsFromMainDrone(this);
      }
    }

    private void ApplyStatsFromMainDrone(AIDrone main)
    {
      damage = main.damage;
      followDistance = main.followDistance;
      followSpeed = main.followSpeed;
      detectionRange = main.detectionRange;
      attackSpeed = main.attackSpeed;
      projectileSpeed = main.projectileSpeed;
      projectilePrefab = main.projectilePrefab;
      attackClip = main.attackClip;

      player = GameObject.FindWithTag("Player");
      anim = GetComponent<Animator>();
      offset = Random.insideUnitCircle.normalized * followDistance;

      UpdateAnimationSpeed();
    }

    protected void Update()
    {
      if (player != null)
      {
        Vector3 targetPos = player.transform.position + offset;
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * followSpeed);
      }
      else
      {
        player = GameObject.FindWithTag("Player");
      }

      if (target != null)
      {
        fireElapsed += Time.deltaTime;
        if (fireElapsed >= 1f / attackSpeed)
        {
          Attack();
          fireElapsed = 0f;
        }
      }
      else
      {
        target = FindClosestEnemy();
      }
    }

    private void Attack()
    {
      if (projectilePrefab == null) return;
      if (target == null) return;

      GameObject obj = Instantiate(projectilePrefab, transform.position + (Vector3)fireOffset, Quaternion.identity);

      if (obj.TryGetComponent<Projectile>(out var proj))
      {
        proj.Initialize(transform.right, damage, projectileSpeed);
        proj.SetTarget(target);

        var src = GetComponentInParent<WeaponSource>();
        if (src != null) proj.SetSourceWeapon(src.weaponData);
      }

      if (anim != null)
        anim.SetTrigger("doAttack");
    }

    private void UpdateAnimationSpeed()
    {
      if (anim == null) return;
      if (attackClip == null) return;

      float multiplier = attackClip.length * attackSpeed;
      anim.SetFloat("AttackSpeedMult", multiplier);
    }

    private Transform FindClosestEnemy()
    {
      Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, detectionRange, LayerMask.GetMask("Enemy"));
      if (hitEnemies.Length == 0) return null;

      GameObject closestEnemy = null;
      float minDistance = detectionRange;

      for (int i = 0; i < hitEnemies.Length; i++)
      {
        float distance = Vector3.Distance(transform.position, hitEnemies[i].transform.position);
        if (distance < minDistance)
        {
          minDistance = distance;
          closestEnemy = hitEnemies[i].gameObject;
        }
      }

      return closestEnemy != null ? closestEnemy.transform : null;
    }

    private void OnDrawGizmos()
    {
      Gizmos.color = Color.red;
      Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
  }
}