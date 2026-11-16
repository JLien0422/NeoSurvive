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
    [SerializeField, Min(0.1f)] private float maxAnimationSpeed = 1.5f;
    [SerializeField] private bool alignToAttackDirection;

    [Header("Sustained Damage Readability")]
    [SerializeField, Min(0.1f)] private float sustainedHitLifetime = 0.4f;
    private readonly int[] recentTargets = new int[32];
    private readonly float[] recentTargetExpiry = new float[32];

    // Damage ticks remain unchanged. A target's visual finishes before another starts.
    public void PlaySustained(Transform target, Vector3 attackDirection)
    {
      if (target == null || hitEffectPrefab == null) return;
      int targetId = target.GetInstanceID();
      int available = -1;
      for (int i = 0; i < recentTargets.Length; i++)
      {
        if (recentTargetExpiry[i] <= Time.time) available = i;
        else if (recentTargets[i] == targetId) return;
      }
      // Bound simultaneous sustained effects per source without per-tick allocations.
      if (available < 0) return;
      float duration = Mathf.Max(0.1f, sustainedHitLifetime);
      recentTargets[available] = targetId;
      recentTargetExpiry[available] = Time.time + duration;
      Spawn(hitEffectPrefab, target.position, attackDirection, positionOffset,
        rotationOffset, scale, tint, animationSpeed, maxAnimationSpeed, duration,
        alignToAttackDirection);
    }

    public void Play(Vector3 hitPosition, Vector3 attackDirection, float maxLifetime = float.PositiveInfinity)
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
        maxAnimationSpeed,
        Mathf.Max(0.01f, Mathf.Min(lifetime, maxLifetime)),
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
      float effectMaxAnimationSpeed,
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
            fittedSpeed = Mathf.Min(
              Mathf.Max(fittedSpeed, clips[0].length / effectLifetime),
              effectMaxAnimationSpeed > 0f ? effectMaxAnimationSpeed : 1.5f
            );
          }
        }

        animator.speed = fittedSpeed;
      }

      Destroy(instance, effectLifetime);
    }
  }
}
