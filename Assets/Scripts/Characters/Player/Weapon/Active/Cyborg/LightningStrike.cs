using UnityEngine;

namespace NeoSurvive.Weapon
{
  /// <summary>
  /// 낙뢰 이펙트용(선택)
  /// - 단순히 잠깐 보였다가 사라지는 용도
  /// </summary>
  public class LightningStrike : MonoBehaviour
  {
    public float lifeTime = 0.25f;

    private void Start()
    {
      Destroy(gameObject, lifeTime);
    }
  }
}

