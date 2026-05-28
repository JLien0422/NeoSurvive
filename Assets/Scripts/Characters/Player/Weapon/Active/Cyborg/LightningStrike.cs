using UnityEngine;

namespace NeoSurvive.Weapon
{
  /// <summary>
  /// 낙뢰 이펙트
  /// - 비주얼 전용
  /// - 자동 삭제
  /// </summary>
  public class LightningStrike : MonoBehaviour
  {
    [Header("Life")]
    [SerializeField] private float lifeTime = 0.4f;

    [Header("Scale")]
    [SerializeField] private float randomScaleMin = 0.8f;
    [SerializeField] private float randomScaleMax = 1.2f;

    private void Start()
    {
      // 랜덤 크기
      float scale =
        Random.Range(
          randomScaleMin,
          randomScaleMax);

      transform.localScale =
        new Vector3(scale, scale, 1f);

      // 자동 삭제
      Destroy(gameObject, lifeTime);
    }
  }
}