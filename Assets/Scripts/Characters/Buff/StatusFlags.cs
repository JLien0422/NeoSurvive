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

    [Header("강제 이동")]
    // 마그네틱 비컨 등 외부 힘에 의해 강제 견인 중인지 여부
    // true이면 EnemyController의 일반 이동 로직을 무시하고 pullVelocity를 적용
    public bool isPulled;
    public Vector2 pullVelocity;

    [Header("Multipliers (Default = 1)")]
    public float moveSpeedMul = 1f;        // 슬로우/이속 버프
    public float outgoingDamageMul = 1f;   // 데미지 증가(가하는 피해)
    public float incomingDamageMul = 1f;   // 받는 피해 배율(데미지 2배 디버프)
  }
}

