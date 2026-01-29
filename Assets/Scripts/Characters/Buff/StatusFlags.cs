using UnityEngine;

namespace NeoSurvive.Buff
{
  /// <summary>
  /// 버프/디버프가 변경하는 "상태 저장소".
  /// Player/Enemy/Weapon 등 어떤 대상이든 붙을 수 있으며,
  /// 대상의 이동/공격/피해 계산 로직에서 이 값을 "참조"하여 상태를 반영한다.
  /// </summary>
  public class StatusFlags : MonoBehaviour
  {
    [Header("CC Flags")]
    public bool moveBlocked;     // 속박/기절 등
    public bool attackBlocked;   // 기절 등
    public bool blinded;         // 실명
    public bool frenzy;          // 광란(적 AI 타겟 변경 등)

    [Header("Multipliers (Default = 1)")]
    public float moveSpeedMul = 1f;        // 슬로우/이속 버프
    public float outgoingDamageMul = 1f;   // 데미지 증가(가하는 피해)
    public float incomingDamageMul = 1f;   // 받는 피해 배율(데미지 2배 디버프)
  }
}

