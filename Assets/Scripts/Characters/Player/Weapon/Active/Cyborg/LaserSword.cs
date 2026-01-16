using System.Collections.Generic;
using UnityEngine;

namespace NeoSurvive.Weapon
{
  /// <summary>
  /// 사이보그 무기: 레이저 검
  /// - 기본: 전방(좌/우) 부채꼴 느낌의 근접 베기(여기서는 OverlapBox로 구현)
  /// - Lv.5 마스터: 베고 지나간 궤적 위치에 1.5초 유지되는 "공간 균열(잔상딜)" 생성
  /// - 구조: Pistol/Fist처럼 Update 타이머로 자동 Attack()
  /// </summary>
  public class LaserSword : MonoBehaviour
  {
    [Header("레벨")]
    [Range(1, 5)]
    public int level = 1;

    [Header("공격 기본 설정")]
    public float damage = 12f;            // 1회 베기 데미지
    public float fireRate = 0.7f;         // 공격 주기(초)
    public float targetSearchRange = 2.5f; // 가장 가까운 적 탐색 범위

    [Header("베기 판정(히트박스)")]
    public Vector2 slashBoxSize = new Vector2(1.8f, 1.1f); // 베기 판정 박스 크기
    public float slashForwardOffset = 1.0f;                // 플레이어 앞쪽 오프셋
    public LayerMask enemyLayer;                           // Enemy 레이어 추천

    [Header("Lv.5 마스터 - 공간 균열(잔상딜)")]
    public bool enableRiftAtLevel5 = true;
    public float riftDuration = 1.5f;        // 균열 유지 시간
    public float riftDps = 10f;              // 초당 데미지
    public Vector2 riftBoxSize = new Vector2(2.2f, 0.8f); // 균열 판정 크기(잔상 궤적 느낌)
    public float riftForwardOffset = 1.1f;   // 균열 생성 위치 오프셋(베기 궤적)

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

    /// <summary>
    /// 외부(레벨업 시스템)에서 호출할 수 있는 레벨업 함수
    /// - 필요 없으면 사용 안 해도 됨
    /// </summary>
    public void LevelUp()
    {
      if (level >= 5) return;
      level++;

      // 레벨업 변화(기획서: 공격 범위, 대미지 증가)
      damage += 3f;
      slashBoxSize += new Vector2(0.15f, 0.08f);
      targetSearchRange += 0.2f;

      // 공격 주기(공격속도)도 살짝 빨라지게 하고 싶다면 아래처럼(선택)
      // fireRate = Mathf.Max(0.35f, fireRate - 0.05f);
    }

    private void Attack()
    {
      // 1) 타겟 탐색 (없어도 공격은 함: 기본 오른쪽)
      GameObject target = FindClosestEnemy();
      Vector3 dir = target != null
        ? (target.transform.position - transform.position).normalized
        : Vector3.right;

      float facing = dir.x >= 0 ? 1f : -1f;

      // 2) 베기 판정 위치 계산
      Vector2 slashCenter = (Vector2)transform.position + new Vector2(slashForwardOffset * facing, 0f);

      // 3) 베기 판정(한 번 공격에 한 번 데미지)
      Collider2D[] hits = Physics2D.OverlapBoxAll(slashCenter, slashBoxSize, 0f, enemyLayer);
      for (int i = 0; i < hits.Length; i++)
      {
        if (hits[i] == null) continue;

        // 프로젝트 데미지 처리 방식에 맞춰 아래 1개로 정리하면 됨
        // (A) IDamageable 인터페이스 방식
        IDamageable d = hits[i].GetComponent<IDamageable>();
        if (d != null)
        {
          d.TakeDamage(damage);
          continue;
        }

        // (B) TakeDamage(float) 같은 메서드가 있을 경우
        hits[i].SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
      }

      // 4) Lv.5 마스터: 공간 균열(잔상딜) 생성
      if (enableRiftAtLevel5 && level >= 5)
      {
        SpawnRift(facing);
      }
    }

    private void SpawnRift(float facing)
    {
      Vector2 riftCenter = (Vector2)transform.position + new Vector2(riftForwardOffset * facing, 0f);

      // 별도 프리팹 없이 런타임 생성
      GameObject riftObj = new GameObject("SpaceRift");
      riftObj.transform.position = riftCenter;

      // 판정용 콜라이더(Trigger)
      BoxCollider2D box = riftObj.AddComponent<BoxCollider2D>();
      box.isTrigger = true;
      box.size = riftBoxSize;

      // 데미지 처리 컴포넌트
      SpaceRift rift = riftObj.AddComponent<SpaceRift>();
      rift.enemyLayer = enemyLayer;
      rift.duration = riftDuration;
      rift.dps = riftDps;

      // 보기용(선택): 스프라이트 없으면 안 보여도 기능은 정상
      // SpriteRenderer sr = riftObj.AddComponent<SpriteRenderer>();
      // sr.sprite = ...; // 나중에 VFX 리소스 연결
    }

    private GameObject FindClosestEnemy()
    {
      GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
      GameObject closest = null;
      float closestDistance = targetSearchRange > 0 ? targetSearchRange : 10f;

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

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
      // 베기 박스(오른쪽 기준) 시각화
      Gizmos.color = Color.cyan;
      Vector2 slashCenter = (Vector2)transform.position + new Vector2(slashForwardOffset, 0f);
      Gizmos.DrawWireCube(slashCenter, slashBoxSize);

      // 균열 박스(오른쪽 기준) 시각화
      Gizmos.color = Color.magenta;
      Vector2 riftCenter = (Vector2)transform.position + new Vector2(riftForwardOffset, 0f);
      Gizmos.DrawWireCube(riftCenter, riftBoxSize);
    }
#endif
  }

  /// <summary>
  /// Lv.5 마스터 효과: 공간 균열(잔상딜)
  /// - duration 동안 유지되며, 범위 안 적에게 초당 dps 데미지
  /// - 같은 적에게 너무 자주 들어가지 않도록 tick 간격으로 처리
  /// </summary>
  public class SpaceRift : MonoBehaviour
  {
    public LayerMask enemyLayer;
    public float duration = 1.5f;
    public float dps = 10f;

    private float lifeTimer;
    private float tickTimer;
    private const float tickInterval = 0.2f; // 0.2초마다 도트 적용

    // 적별로 "최근 데미지 적용 시간" 기록 (중복 과다 적용 방지)
    private readonly Dictionary<int, float> lastHitTime = new Dictionary<int, float>();

    private void Update()
    {
      lifeTimer += Time.deltaTime;
      if (lifeTimer >= duration)
      {
        Destroy(gameObject);
        return;
      }

      tickTimer += Time.deltaTime;
      if (tickTimer >= tickInterval)
      {
        tickTimer = 0f;
        ApplyDotTick();
      }
    }

    private void ApplyDotTick()
    {
      // 현재 균열(Trigger Collider) 범위 안에 있는 적들을 찾아 도트 적용
      // (Trigger 이벤트 대신, 안정적으로 동작하게 Overlap로 한 번 더 검사)
      BoxCollider2D box = GetComponent<BoxCollider2D>();
      if (box == null) return;

      Vector2 center = (Vector2)transform.position + box.offset;
      Collider2D[] hits = Physics2D.OverlapBoxAll(center, box.size, 0f, enemyLayer);

      float tickDamage = dps * tickInterval;

      for (int i = 0; i < hits.Length; i++)
      {
        if (hits[i] == null) continue;

        int id = hits[i].GetInstanceID();
        // 같은 tick에 중복 적용 방지(혹시 콜라이더가 여러 개일 때)
        if (lastHitTime.TryGetValue(id, out float t) && Mathf.Abs(t - Time.time) < 0.01f)
          continue;

        lastHitTime[id] = Time.time;

        IDamageable d = hits[i].GetComponent<IDamageable>();
        if (d != null)
        {
          d.TakeDamage(tickDamage);
          continue;
        }

        hits[i].SendMessage("TakeDamage", tickDamage, SendMessageOptions.DontRequireReceiver);
      }
    }
  }

  /// <summary>
  /// 프로젝트에 이미 데미지 인터페이스가 있다면 이건 제거하고 맞춰줘.
  /// </summary>
  public interface IDamageable
  {
    void TakeDamage(float amount);
  }
}
