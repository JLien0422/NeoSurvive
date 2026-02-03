using UnityEngine;
using NeoSurvive.Core;

namespace NeoSurvive.Weapon
{
  /// <summary>
  /// 자동 전투 드론
  /// </summary>
  public class AIDrone : MonoBehaviour
  {

    public Animator anim;
    public AnimationClip attackClip;

    /// <summary>
    /// 발사체 프리팹
    /// </summary>
    public GameObject projectilePrefab;

    [Header("AI 드론 설정")]
    /// <summary>
    /// 플레이어 따라가는 거리
    /// </summary>
    public float followDistance = 2f;

    /// <summary>
    /// 플레이어 따라가는 속도
    /// </summary>
    public float followSpeed = 1f;

    /// <summary>
    /// 플레이어 게임오브젝트
    /// </summary>
    public GameObject player;

    /// <summary>
    /// 플레이어와의 거리 오프셋
    /// </summary>
    public Vector3 offset;

    /// <summary>
    /// 캐릭터 기준 감지 범위
    /// </summary>
    public float detectionRange = 10f;

    /// <summary>
    /// 공격 속도 (초당 발사 횟수)
    /// </summary>
    public float attackSpeed = 5f;

    public float fireElapsed = 0f;

    public Transform target;

    public Vector2 fireOffset;

    public float damage = 5f;
    private float baseDamage;
    private System.Collections.Generic.List<AIDrone> subDrones = new System.Collections.Generic.List<AIDrone>();

    private void Start()
    {
      baseDamage = damage;

      offset = Random.insideUnitCircle.normalized * followDistance;

      player = GameObject.FindWithTag("Player");

      anim = GetComponent<Animator>();

      UpdateAnimationSpeed();
    }

    public void OnLevelUp(int level)
    {
      if (baseDamage == 0 && damage > 0) baseDamage = damage;

      // 데미지 20% 증가
      damage = baseDamage * (1f + (level - 1) * 0.2f);

      // 기존 서브 드론들 스탯 업데이트
      for (int i = subDrones.Count - 1; i >= 0; i--)
      {
        if (subDrones[i] == null) subDrones.RemoveAt(i);
        else subDrones[i].damage = damage;
      }

      // 드론 개수 증가 (레벨당 1마리 추가 생성)
      // Lv 1: 1 (Main)
      // Lv 2: 2 (Main + 1 Sub)
      int desiredSubCount = level - 1;
      int currentSubCount = subDrones.Count;

      for (int i = 0; i < desiredSubCount - currentSubCount; i++)
      {
        // 자신을 복제
        GameObject clone = Instantiate(gameObject, transform.position, Quaternion.identity);
        if (clone.TryGetComponent<AIDrone>(out var cloneScript))
        {
          cloneScript.damage = damage;
          // 복제된 드론의 Start()가 호출되면서 offset이 랜덤하게 재설정되어 겹치지 않음
          subDrones.Add(cloneScript);
        }
      }

      Debug.Log($"[AIDrone] Lv.{level}, Total Drones: {1 + subDrones.Count}, Damage: {damage}");
    }

    protected void Update()
    {
      // 플레이어 주변 따라다니기
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
      // [Coop] 서버에 공격 보고 (로컬 플레이어일 때만)
      var runtimeInfo = GetComponent<WeaponRuntimeInfo>();
      if (runtimeInfo != null && target != null)
      {
        Vector3 dir = (target.position - (transform.position + (Vector3)fireOffset)).normalized;
        runtimeInfo.ReportProjectile(transform.position + (Vector3)fireOffset, dir, 20f, detectionRange, 0, 0);
      }

      GameObject obj = Instantiate(projectilePrefab, transform.position + (Vector3)fireOffset, Quaternion.identity);
      if (obj.TryGetComponent<Projectile>(out var proj))
      {
        proj.Initialize(transform.right, damage, 20f);
        proj.SetTarget(target);
      }
      anim.SetTrigger("doAttack");
    }

    private void UpdateAnimationSpeed()
    {
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

      if (closestEnemy == null) return null;
      return closestEnemy.transform;
    }


    /// <summary>
    /// 감지 범위 시각화
    /// </summary>
    private void OnDrawGizmos()
    {
      Gizmos.color = Color.red;
      Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
  }
}
