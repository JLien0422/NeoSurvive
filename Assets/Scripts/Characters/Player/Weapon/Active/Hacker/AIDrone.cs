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

    private void Start()
    {
      offset = Random.insideUnitCircle.normalized * followDistance;

      player = GameObject.FindWithTag("Player");

      anim = GetComponent<Animator>();

      updateAnimationSpeed();
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
      GameObject obj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
      if (obj.TryGetComponent<Projectile>(out var proj))
      {
        proj.Initialize(transform.right, 5f); // 5f는 임시 데미지입니다.
        proj.SetTarget(target);
      }
      anim.SetTrigger("doAttack");
    }

    private void updateAnimationSpeed()
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
