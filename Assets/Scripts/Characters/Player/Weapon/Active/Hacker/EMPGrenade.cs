using System.Collections.Generic;
using UnityEngine;

namespace NeoSurvive.Weapon
{
  /// <summary>
  /// 해커 무기 - EMP 수류탄
  /// 
  /// [기본 동작]
  /// - 일정 주기마다 가까운 적을 탐색
  /// - 수류탄은 적에게 닿아서 터지는 것이 아니라
  ///   목표 지점까지 포물선처럼 날아간 뒤 폭발
  /// - 폭발 시 범위 피해 + 기절 적용
  /// 
  /// [레벨별 투척 개수]
  /// - Lv1~2 : 1개
  /// - Lv3~4 : 2개
  /// - Lv5   : 4개
  /// 
  /// [다중 투척 방식]
  /// - 주변 적이 여러 명 있으면 각각 다른 적에게 던짐
  /// - 적 수가 부족하면 남는 수류탄은 첫 번째 타겟 주변으로 퍼지게 던짐
  /// </summary>
  public class EMPGrenade : MonoBehaviour
  {
    [Header("Projectile")]
    [SerializeField] private GameObject grenadeProjectilePrefab;   // 실제 날아가는 수류탄 프리팹
    [SerializeField] private Transform firePoint;                  // 수류탄이 생성되는 시작 위치

    [Header("Targeting")]
    [SerializeField] private float throwRange = 8f;                // 적 탐색 범위
    [SerializeField] private LayerMask enemyMask;                  // 적 탐색용 레이어
    [SerializeField] private string enemyTag = "Enemy";            // 적 판별용 태그

    [Header("Explosion Stats")]
    [SerializeField] private float damage = 20f;                   // 폭발 피해
    [SerializeField] private float explosionRadius = 2.5f;         // 폭발 반경
    [SerializeField] private float stunDuration = 1.5f;            // 기절 지속 시간

    [Header("Throw Arc")]
    [SerializeField] private float travelTime = 0.65f;             // 목표 지점까지 날아가는 시간
    [SerializeField] private float arcHeight = 1.75f;              // 포물선 높이

    [Header("Attack Speed")]
    [SerializeField] private float fireRate = 2.2f;                // 수류탄 투척 간격

    [Header("Fallback")]
    [SerializeField] private bool throwForwardIfNoTarget = false;  // 적이 없을 때 전방으로 던질지 여부
    [SerializeField] private float fallbackForwardDistance = 4f;   // 적이 없을 때 전방 목표 지점 거리

    [Header("Multi Throw")]
    [SerializeField] private float multiThrowSpread = 1.2f;        // 적이 부족할 때 남는 수류탄이 퍼지는 정도

    [Header("Multi Throw Timing")]
    [SerializeField] private float multiThrowDelay = 0.05f;   // 수류탄을 하나씩 던질 때 간격

    private float fireTimer = 0f;

    // 현재 무기 레벨
    private int currentLevel = 1;

    // CSV에서 이 무기를 찾을 때 사용할 ID
    private readonly string weaponId = "empgrenade";

    private void Start()
    {
      // 시작 시 1레벨 기준 스탯 적용
      ApplyStatsFromCSV(1);
    }

    private void Update()
    {
      // 시간 누적
      fireTimer += Time.deltaTime;

      // fireRate마다 한 번씩 수류탄 투척 시도
      if (fireTimer >= fireRate)
      {
        fireTimer = 0f;
        TryThrowGrenade();
      }
    }

    /// <summary>
    /// 실제로 수류탄을 던질지 판단하는 함수
    /// 
    /// [작동 순서]
    /// 1. 프리팹 연결 여부 확인
    /// 2. 현재 레벨 기준 투척 개수 계산
    /// 3. 가까운 적 여러 명 찾기
    /// 4. 적이 있으면 각 적에게 던짐
    /// 5. 적이 없으면 옵션에 따라 전방으로 던짐
    /// </summary>
    private void TryThrowGrenade()
    {
      if (grenadeProjectilePrefab == null)
      {
        Debug.LogWarning("[EMPGrenade] grenadeProjectilePrefab 이 연결되지 않았습니다.");
        return;
      }

      // 수류탄이 생성될 시작 위치
      Vector2 startPos = firePoint != null ? (Vector2)firePoint.position : (Vector2)transform.position;

      // 현재 레벨에 따른 투척 개수 계산
      int grenadeCount = GetGrenadeCountByLevel(currentLevel);

      // 가까운 적들을 grenadeCount 개수만큼 찾아옴
      List<Transform> targets = FindNearestEnemies(grenadeCount);

      // 주변에 적이 하나도 없을 때
      if (targets.Count == 0)
      {
        if (!throwForwardIfNoTarget)
          return;

        // 적이 없으면 플레이어 전방으로 던짐
        Vector2 fallbackTargetPos = startPos + (Vector2)(transform.right * fallbackForwardDistance);
        SpawnGrenade(startPos, fallbackTargetPos);
        return;
      }

      // 찾은 적들에게 수류탄 여러 개 던지기
      SpawnMultipleGrenades(startPos, targets, grenadeCount);
    }

    /// <summary>
    /// [변경]
    /// 예전에는 이 함수 안에서 바로 여러 개를 한 프레임에 생성했음.
    /// 지금은 코루틴을 시작해서 시간차를 두고 하나씩 던지게 바뀜.
    /// </summary>
    private void SpawnMultipleGrenades(Vector2 startPos, List<Transform> targets, int grenadeCount)
    {
      // [변경] 직접 SpawnGrenade를 반복 호출하지 않고 코루틴 시작
      StartCoroutine(SpawnGrenadesWithDelay(startPos, targets, grenadeCount));
    }

    /// <summary>
    /// [추가]
    /// 시간차를 두고 수류탄을 하나씩 던지는 코루틴
    /// 
    /// [작동 순서]
    /// 1. 찾은 적들에게 각각 하나씩 던짐
    /// 2. 던질 때마다 multiThrowDelay 만큼 대기
    /// 3. 남는 수류탄이 있으면 첫 번째 타겟 주변으로 퍼뜨려서 던짐
    /// 4. 퍼뜨릴 때도 매번 multiThrowDelay 만큼 대기
    /// </summary>
    private System.Collections.IEnumerator SpawnGrenadesWithDelay(
      Vector2 startPos,
      List<Transform> targets,
      int grenadeCount)
    {
      if (targets == null || targets.Count == 0)
        yield break;

      // 1차: 찾은 적들에게 각각 하나씩 던짐
      int throwCount = Mathf.Min(grenadeCount, targets.Count);

      for (int i = 0; i < throwCount; i++)
      {
        if (targets[i] != null)
        {
          SpawnGrenade(startPos, targets[i].position);
        }

        // [추가] 하나 던질 때마다 잠깐 쉬어서 연속 투척처럼 보이게 함
        yield return new WaitForSeconds(multiThrowDelay);
      }

      // 2차: 적 수보다 수류탄이 많으면 남는 수류탄 처리
      int remain = grenadeCount - throwCount;
      if (remain <= 0)
        yield break;

      Vector2 playerPos = transform.root != null ? (Vector2)transform.root.position : (Vector2)transform.position;
      Vector2 centerTargetPos = targets[0] != null ? (Vector2)targets[0].position : playerPos;

      Vector2 dir = (centerTargetPos - playerPos).normalized;
      if (dir.sqrMagnitude <= 0.0001f)
        dir = Vector2.right;

      // 첫 번째 타겟 방향에 수직인 방향으로 좌우 퍼짐 생성
      Vector2 perpendicular = new Vector2(-dir.y, dir.x);

      for (int i = 0; i < remain; i++)
      {
        float t = remain == 1 ? 0f : Mathf.Lerp(-1f, 1f, i / (float)(remain - 1));
        Vector2 offset = perpendicular * (t * multiThrowSpread);

        SpawnGrenade(startPos, centerTargetPos + offset);

        // [추가] 남는 수류탄도 시간차 투척
        yield return new WaitForSeconds(multiThrowDelay);
      }
    }

    /// <summary>
    /// 수류탄 프리팹을 실제로 생성하고 초기화하는 함수
    /// 
    /// [작동 방식]
    /// - grenadeProjectilePrefab 생성
    /// - EMPGrenadeProjectile 컴포넌트 찾기
    /// - 시작 위치, 목표 위치, 데미지, 반경, 기절 시간 등을 넘겨줌
    /// </summary>
    private void SpawnGrenade(Vector2 startPos, Vector2 targetPos)
    {
      GameObject grenadeObj = Instantiate(grenadeProjectilePrefab, startPos, Quaternion.identity);

      EMPGrenadeProjectile projectile = grenadeObj.GetComponent<EMPGrenadeProjectile>();
      if (projectile == null)
      {
        Debug.LogWarning("[EMPGrenade] grenadeProjectilePrefab 에 EMPGrenadeProjectile 컴포넌트가 없습니다.");
        Destroy(grenadeObj);
        return;
      }

      projectile.Initialize(
        gameObject,
        startPos,
        targetPos,
        damage,
        explosionRadius,
        stunDuration,
        travelTime,
        arcHeight,
        enemyMask,
        enemyTag
      );
    }

    /// <summary>
    /// 가까운 적 여러 명을 찾는 함수
    /// 
    /// [왜 플레이어 중심인가]
    /// - 무기 오브젝트 위치가 플레이어보다 위쪽에 있으면
    ///   아래쪽 적이 탐색 범위에서 빠지는 문제가 생김
    /// - 그래서 반드시 transform.root, 즉 플레이어 중심으로 탐색
    /// 
    /// [작동 순서]
    /// 1. 플레이어 중심으로 범위 탐색
    /// 2. enemyMask / enemyTag 조건에 맞는 적만 후보로 저장
    /// 3. 거리순으로 정렬
    /// 4. 필요한 개수(count)만큼만 반환
    /// </summary>
    private List<Transform> FindNearestEnemies(int count)
    {
      Vector2 origin = transform.root != null ? (Vector2)transform.root.position : (Vector2)transform.position;
      Collider2D[] hits = Physics2D.OverlapCircleAll(origin, throwRange, enemyMask);

      List<Transform> candidates = new List<Transform>();

      foreach (Collider2D hit in hits)
      {
        if (hit == null)
          continue;

        if (!string.IsNullOrEmpty(enemyTag) && !hit.CompareTag(enemyTag))
          continue;

        candidates.Add(hit.transform);
      }

      // 가까운 순서대로 정렬
      candidates.Sort((a, b) =>
      {
        float distA = ((Vector2)a.position - origin).sqrMagnitude;
        float distB = ((Vector2)b.position - origin).sqrMagnitude;
        return distA.CompareTo(distB);
      });

      // 필요한 개수보다 많으면 뒤는 잘라냄
      if (candidates.Count > count)
        candidates.RemoveRange(count, candidates.Count - count);

      return candidates;
    }

    /// <summary>
    /// 현재 레벨에 따라 수류탄 개수를 정하는 함수
    /// </summary>
    private int GetGrenadeCountByLevel(int level)
    {
      if (level >= 5) return 4;
      if (level >= 3) return 2;
      return 1;
    }

    /// <summary>
    /// CSV에서 현재 레벨의 스탯을 읽어와 적용하는 함수
    /// 
    /// [CSV에서 가져오는 값]
    /// - damage
    /// - explosionradius
    /// - stunduration
    /// - range
    /// - archeight
    /// - traveltime
    /// - firerate
    /// 
    /// [주의]
    /// - 수류탄 개수는 CSV가 아니라 코드(GetGrenadeCountByLevel)에서 처리
    /// </summary>
    public void ApplyStatsFromCSV(int level)
    {
      currentLevel = level;

      if (WeaponStatLoader.DB == null)
      {
        Debug.LogWarning("[EMPGrenade] WeaponStatLoader.DB 가 없습니다.");
        return;
      }

      if (!WeaponStatLoader.DB.rows.TryGetValue(weaponId, out var levelDict))
      {
        Debug.LogWarning($"[EMPGrenade] weaponId 없음: {weaponId}");
        return;
      }

      if (!levelDict.TryGetValue(level, out var row))
      {
        Debug.LogWarning($"[EMPGrenade] level 데이터 없음: {level}");
        return;
      }

      // CSV 값 적용
      damage = row.damage;
      explosionRadius = row.explosionradius;
      stunDuration = row.stunduration;
      throwRange = row.range;
      arcHeight = row.archeight;
      travelTime = row.traveltime;
      fireRate = row.firerate;

      Debug.Log($"[EMPGrenade] CSV 적용 | Lv={level}, Count={GetGrenadeCountByLevel(level)}, Damage={damage}, Radius={explosionRadius}, Stun={stunDuration}");
    }

    /// <summary>
    /// 외부에서 무기 레벨업 시 호출하는 함수
    /// </summary>
    public void OnLevelUp(int level)
    {
      ApplyStatsFromCSV(level);
    }

    private void OnDrawGizmosSelected()
    {
      // 탐색 범위도 플레이어 중심으로 보이게 수정
      Vector2 origin = transform.root != null ? (Vector2)transform.root.position : (Vector2)transform.position;

      Gizmos.color = Color.cyan;
      Gizmos.DrawWireSphere(origin, throwRange);
    }
  }
}