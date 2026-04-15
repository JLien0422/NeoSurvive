using UnityEngine;

/// <summary>
/// 보스에 한 번만 텔레그래프 프리팹을 넣어두는 용도. 각 <see cref="BossPatternBase"/>가
/// 자기 슬롯을 비웠을 때만 이 값을 읽습니다.
/// </summary>
public class BossAttackWarningDisplay : MonoBehaviour
{
  [Header("텔레그래프 프리팹 (패턴에 비어 있을 때만 사용)")]
  [SerializeField] private GameObject telegraphCirclePrefab;
  [SerializeField] private GameObject telegraphRectPrefab;

  public GameObject TelegraphCirclePrefab => telegraphCirclePrefab;
  public GameObject TelegraphRectPrefab => telegraphRectPrefab;
}
