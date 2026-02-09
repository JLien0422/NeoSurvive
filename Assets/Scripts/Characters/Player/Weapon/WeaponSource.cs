using UnityEngine;

namespace NeoSurvive.Weapon
{
  /// <summary>
  /// WeaponManager가 무기 프리팹을 생성할 때 붙이며,
  /// 이 무기가 어떤 WeaponBase(데이터)에 해당하는지 참조를 보관합니다.
  /// 무기별 대미지 통계 기록 시 소스 식별에 사용됩니다.
  /// </summary>
  public class WeaponSource : MonoBehaviour
  {
    public WeaponBase weaponData;
  }
}
