using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 보스 AI. 패턴이 없으면 플레이어만 추적.
/// 패턴이 있으면 기본적으로 <b>직전에 쓴 패턴을 제외한 뒤 무작위</b>로 하나 골라 실행합니다.
/// </summary>
[RequireComponent(typeof(Boss))]
[RequireComponent(typeof(Rigidbody2D))]
public class BossController : MonoBehaviour
{
  [Header("이동 설정")]
  [Tooltip("패턴이 끝난 뒤 플레이어 추적 속도")]
  [SerializeField] private float moveSpeed = 2f;

  [Header("패턴")]
  [Tooltip("사용 가능한 패턴 풀 (순서는 랜덤 모드에서는 무시됩니다)")]
  [SerializeField] private BossPatternBase[] patternsInOrder;

  [Tooltip("켜면 예전처럼 배열 순서대로만 반복합니다.")]
  [SerializeField] private bool useSequentialPatternOrder = false;

  [Tooltip("패턴과 패턴 사이 대기(초)")]
  [SerializeField] private float delayBetweenPatterns = 1f;

  [Header("기즈모 설정")]
  [SerializeField] private bool showGizmos = true;

  private Boss boss;
  private Rigidbody2D rb;
  private Transform playerTarget;

  private float searchTimer;
  private const float SEARCH_INTERVAL = 1f;

  private bool chaseEnabled = true;

  /// <summary>직전에 실행한 <see cref="patternsInOrder"/> 인덱스. 랜덤 시 같은 패턴 연속 방지용.</summary>
  private int lastPatternIndex = -1;

  private void Awake()
  {
    boss = GetComponent<Boss>();
    rb = GetComponent<Rigidbody2D>();
    rb.gravityScale = 0f;
    rb.freezeRotation = true;

    FindPlayer();
  }

  private void Start()
  {
    if (patternsInOrder == null || patternsInOrder.Length == 0)
      return;

    foreach (var p in patternsInOrder)
    {
      if (p != null)
        p.Initialize(boss, playerTarget, rb);
    }

    StartCoroutine(PatternLoopCoroutine());
  }

  private void OnDisable()
  {
    StopAllCoroutines();
  }

  private IEnumerator PatternLoopCoroutine()
  {
    var waitBetween = new WaitForSeconds(delayBetweenPatterns);

    while (boss != null && !boss.IsDead)
    {
      if (useSequentialPatternOrder)
      {
        foreach (var pattern in patternsInOrder)
        {
          if (boss == null || boss.IsDead)
            yield break;
          if (pattern == null)
            continue;

          yield return RunSinglePattern(pattern, waitBetween);
        }
      }
      else
      {
        int idx = PickRandomPatternIndexExcludingLast();
        if (idx < 0)
          yield break;

        var pattern = patternsInOrder[idx];
        if (pattern == null)
          continue;

        lastPatternIndex = idx;
        yield return RunSinglePattern(pattern, waitBetween);
      }
    }
  }

  private IEnumerator RunSinglePattern(BossPatternBase pattern, WaitForSeconds waitBetween)
  {
    pattern.RefreshPlayerTarget(playerTarget);

    chaseEnabled = false;
    if (rb != null)
      rb.velocity = Vector2.zero;

    yield return StartCoroutine(pattern.RunPatternCoroutine());

    chaseEnabled = true;

    if (delayBetweenPatterns > 0f)
      yield return waitBetween;
  }

  /// <summary>직전 인덱스를 제외하고 무작위. 패턴이 1개뿐이면 그대로 사용.</summary>
  private int PickRandomPatternIndexExcludingLast()
  {
    if (patternsInOrder == null || patternsInOrder.Length == 0)
      return -1;

    var candidates = new List<int>();
    for (int i = 0; i < patternsInOrder.Length; i++)
    {
      if (patternsInOrder[i] != null)
        candidates.Add(i);
    }

    if (candidates.Count == 0)
      return -1;

    if (candidates.Count == 1)
      return candidates[0];

    if (lastPatternIndex >= 0)
      candidates.Remove(lastPatternIndex);

    if (candidates.Count == 0)
    {
      // 전부 제거됐으면(이론상 동일 인덱스만 있었을 때) 풀에서 다시 고름
      candidates.Clear();
      for (int i = 0; i < patternsInOrder.Length; i++)
      {
        if (patternsInOrder[i] != null)
          candidates.Add(i);
      }
    }

    return candidates[Random.Range(0, candidates.Count)];
  }

  private void Update()
  {
    searchTimer += Time.deltaTime;
    if (searchTimer >= SEARCH_INTERVAL)
    {
      FindPlayer();
      searchTimer = 0f;
    }
  }

  private void FixedUpdate()
  {
    if (boss.IsDead)
    {
      rb.velocity = Vector2.zero;
      return;
    }

    if (chaseEnabled)
      MoveTowardPlayer();
  }

  /// <summary>패턴 스크립트에서 추적 on/off가 필요할 때 호출 가능합니다.</summary>
  public void SetChaseEnabled(bool value) => chaseEnabled = value;

  private void FindPlayer()
  {
    GameObject playerObj = GameObject.FindWithTag("Player");
    if (playerObj != null)
      playerTarget = playerObj.transform;
  }

  private void MoveTowardPlayer()
  {
    if (playerTarget == null) return;

    Vector2 direction = (playerTarget.position - transform.position).normalized;
    rb.velocity = direction * moveSpeed;
  }

#if UNITY_EDITOR
  private void OnDrawGizmos()
  {
    if (!showGizmos) return;

    Gizmos.color = Color.red;
    Gizmos.DrawWireSphere(transform.position, 0.5f);
  }
#endif
}
