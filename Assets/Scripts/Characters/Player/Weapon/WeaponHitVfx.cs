using UnityEngine;

namespace NeoSurvive.Weapon
{
  /// <summary>
  /// 무기 프리팹에서 직접 설정하는 명중 이펙트 연결점입니다.
  /// 데미지 판정은 기존 무기 코드가 담당하고, 이 컴포넌트는 시각 효과만 재생합니다.
  /// </summary>
  [DisallowMultipleComponent]
  public sealed class WeaponHitVfx : MonoBehaviour
  {
    [Header("Hit Effect")]
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField, Min(0.01f)] private float lifetime = 0.35f;
    [SerializeField] private Vector3 positionOffset;
    [SerializeField] private Vector3 rotationOffset;
    [SerializeField] private Vector3 scale = Vector3.one;
    [SerializeField] private Color tint = Color.white;
    [SerializeField, Min(0.1f)] private float animationSpeed = 1f;
    [SerializeField] private bool alignToAttackDirection;

    public void Play(Vector3 hitPosition, Vector3 attackDirection)
    {
      Spawn(
        hitEffectPrefab,
        hitPosition,
        attackDirection,
        positionOffset,
        rotationOffset,
        scale,
        tint,
        animationSpeed,
        lifetime,
        alignToAttackDirection
      );
    }

    private static void Spawn(
      GameObject effectPrefab,
      Vector3 position,
      Vector3 attackDirection,
      Vector3 offset,
      Vector3 eulerOffset,
      Vector3 effectScale,
      Color effectTint,
      float effectAnimationSpeed,
      float effectLifetime,
      bool alignToDirection)
    {
      if (effectPrefab == null)
        return;

      float directionAngle = 0f;
      if (alignToDirection && attackDirection.sqrMagnitude > 0.0001f)
      {
        directionAngle = Mathf.Atan2(attackDirection.y, attackDirection.x) * Mathf.Rad2Deg;
      }

      Quaternion rotation = Quaternion.Euler(
        eulerOffset.x,
        eulerOffset.y,
        eulerOffset.z + directionAngle
      );

      GameObject instance = Instantiate(
        effectPrefab,
        position + offset,
        rotation
      );

      instance.transform.localScale = Vector3.Scale(instance.transform.localScale, effectScale);

      SpriteRenderer[] renderers = instance.GetComponentsInChildren<SpriteRenderer>(true);
      foreach (SpriteRenderer renderer in renderers)
      {
        renderer.color *= effectTint;
      }

      Animator animator = instance.GetComponentInChildren<Animator>();
      if (animator != null)
      {
        float fittedSpeed = effectAnimationSpeed;
        RuntimeAnimatorController controller = animator.runtimeAnimatorController;
        if (controller != null && effectLifetime > 0f)
        {
          AnimationClip[] clips = controller.animationClips;
          if (clips != null && clips.Length > 0 && clips[0] != null)
          {
            // 원본 프레임을 생략하지 않고, 무기별 표시 시간 안에 한 번만 재생한다.
            fittedSpeed = Mathf.Max(fittedSpeed, clips[0].length / effectLifetime);
          }
        }

        animator.speed = fittedSpeed;
      }

      Destroy(instance, effectLifetime);
    }
  }
}
