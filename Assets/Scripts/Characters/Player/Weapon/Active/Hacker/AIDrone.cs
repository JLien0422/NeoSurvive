using UnityEngine;
using NeoSurvive.Core;
using System.Collections.Generic;

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

    // =========================
    // ★ Lv5 레이저 그물
    // =========================
    [Header("Master Laser Net")]
    [SerializeField] private GameObject laserNetPrefab;
    [SerializeField] private float laserDamagePerTick = 2f;
    [SerializeField] private float laserTickInterval = 0.25f;
    [SerializeField] private float laserWidth = 0.15f;

    private readonly string weaponId = "aidrone";
    private const int maxLevel = 5;

    private int currentLevel = 1;
    private bool isSubDrone = false;

    private bool IsMasterLevel => currentLevel >= maxLevel;

    private List<AIDrone> subDrones = new List<AIDrone>();
    private List<DroneLaserNet> laserNets = new List<DroneLaserNet>();

    private void Start()
    {
      player = GameObject.FindWithTag("Player");
      anim = GetComponent<Animator>();

      offset = Random.insideUnitCircle.normalized * followDistance;

      if (!isSubDrone)
      {
        currentLevel = 1;
        ApplyStatsFromCSV(currentLevel);
      }

      UpdateAnimationSpeed();
    }

    public void OnLevelUp(int level)
    {
      if (isSubDrone)
        return;

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

      // =========================
      // ★ Lv5 마스터 효과
      // =========================
      if (IsMasterLevel)
      {
        DoubleDroneCount();
        CreateLaserNet();
      }

      Debug.Log($"[AIDrone] CSV 적용 | Lv={currentLevel}, Total Drones={1 + subDrones.Count}, Damage={damage}, Master={IsMasterLevel}");
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

      float perLevel =
        row.damageperlevel > 0f
        ? row.damageperlevel
        : levelOneRow.damageperlevel;

      damage =
        levelOneRow.damage *
        (1f + (level - 1) * perLevel);

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

      // =========================
      // ★ Lv5 마스터면 드론 수 2배
      // =========================
      if (IsMasterLevel)
      {
        desiredSubCount *= 2;
      }

      for (int i = subDrones.Count - 1; i >= 0; i--)
      {
        if (subDrones[i] == null)
        {
          subDrones.RemoveAt(i);
        }
      }

      int currentSubCount = subDrones.Count;

      if (currentSubCount >= desiredSubCount)
        return;

      for (int i = 0; i < desiredSubCount - currentSubCount; i++)
      {
        GameObject clone =
          Instantiate(gameObject, transform.position, Quaternion.identity);

        if (clone.TryGetComponent<AIDrone>(out var cloneScript))
        {
          cloneScript.isSubDrone = true;

          cloneScript.ApplyStatsFromMainDrone(this);

          subDrones.Add(cloneScript);
        }
      }
    }

    private void DoubleDroneCount()
    {
      UpdateSubDroneCount(currentLevel);
    }

    // =========================
    // ★ Lv5 레이저 그물 생성
    // =========================
    private void CreateLaserNet()
    {
      if (!IsMasterLevel)
        return;

      if (laserNetPrefab == null)
      {
        Debug.LogWarning("[AIDrone] laserNetPrefab이 연결되지 않았습니다.");
        return;
      }

      ClearLaserNet();

      List<AIDrone> allDrones = new List<AIDrone>();
      allDrones.Add(this);

      for (int i = subDrones.Count - 1; i >= 0; i--)
      {
        if (subDrones[i] == null)
        {
          subDrones.RemoveAt(i);
          continue;
        }

        allDrones.Add(subDrones[i]);
      }

      if (allDrones.Count < 2)
        return;

      var src = GetComponentInParent<WeaponSource>();

      for (int i = 0; i < allDrones.Count; i++)
      {
        AIDrone current = allDrones[i];
        AIDrone next = allDrones[(i + 1) % allDrones.Count];

        GameObject laserObj = Instantiate(laserNetPrefab);

        DroneLaserNet laser =
          laserObj.GetComponent<DroneLaserNet>();

        if (laser == null)
        {
          Debug.LogWarning("[AIDrone] DroneLaserNet 컴포넌트 없음");
          Destroy(laserObj);
          continue;
        }

        laser.Initialize(
          current.transform,
          next.transform,
          laserDamagePerTick,
          laserTickInterval,
          laserWidth,
          src != null ? src.weaponData : null
        );

        laserNets.Add(laser);
      }
    }

    private void ClearLaserNet()
    {
      for (int i = laserNets.Count - 1; i >= 0; i--)
      {
        if (laserNets[i] != null)
        {
          Destroy(laserNets[i].gameObject);
        }
      }

      laserNets.Clear();
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

      laserNetPrefab = main.laserNetPrefab;
      laserDamagePerTick = main.laserDamagePerTick;
      laserTickInterval = main.laserTickInterval;
      laserWidth = main.laserWidth;

      currentLevel = main.currentLevel;

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

        transform.position = Vector3.Lerp(
          transform.position,
          targetPos,
          Time.deltaTime * followSpeed
        );
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

      GameObject obj = Instantiate(
        projectilePrefab,
        transform.position + (Vector3)fireOffset,
        Quaternion.identity
      );

      if (obj.TryGetComponent<Projectile>(out var proj))
      {
        proj.Initialize(transform.right, damage, projectileSpeed);

        proj.SetTarget(target);

        var src = GetComponentInParent<WeaponSource>();

        if (src != null)
        {
          proj.SetSourceWeapon(src.weaponData);
        }
      }

      if (anim != null)
      {
        anim.SetTrigger("doAttack");
      }
    }

    private void UpdateAnimationSpeed()
    {
      if (anim == null) return;
      if (attackClip == null) return;

      float multiplier =
        attackClip.length * attackSpeed;

      anim.SetFloat("AttackSpeedMult", multiplier);
    }

    private Transform FindClosestEnemy()
    {
      // =========================
      // ★ LinkPistol 표식 대상 우선 공격
      // =========================
      Transform markedTarget =
        MarkedTarget.GetCurrentTarget();

      if (markedTarget != null)
      {
        float markedDistance =
          Vector3.Distance(transform.position, markedTarget.position);

        if (markedDistance <= detectionRange)
        {
          return markedTarget;
        }
      }

      Collider2D[] hitEnemies =
        Physics2D.OverlapCircleAll(
          transform.position,
          detectionRange,
          LayerMask.GetMask("Enemy")
        );

      if (hitEnemies.Length == 0)
        return null;

      GameObject closestEnemy = null;
      float minDistance = detectionRange;

      for (int i = 0; i < hitEnemies.Length; i++)
      {
        float distance =
          Vector3.Distance(
            transform.position,
            hitEnemies[i].transform.position
          );

        if (distance < minDistance)
        {
          minDistance = distance;
          closestEnemy = hitEnemies[i].gameObject;
        }
      }

      return closestEnemy != null
        ? closestEnemy.transform
        : null;
    }

    private void OnDestroy()
    {
      if (!isSubDrone)
      {
        ClearLaserNet();
      }
    }

    private void OnDrawGizmos()
    {
      Gizmos.color = Color.red;
      Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
  }
}