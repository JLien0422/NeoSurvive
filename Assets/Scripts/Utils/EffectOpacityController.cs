using UnityEngine;

namespace NeoSurvive.Utils
{
  /// <summary>
  /// 이펙트 투명도 제어 유틸리티
  /// 투사체를 제외한 모든 이펙트(SpriteRenderer, ParticleSystem 등)에 투명도를 적용합니다.
  /// </summary>
  public class EffectOpacityController : MonoBehaviour
  {
    [Header("투명도 적용 대상")]
    [SerializeField] private bool applyToSpriteRenderers = true;
    [SerializeField] private bool applyToParticleSystems = true;
    
    [Header("투사체 제외 설정")]
    [SerializeField] private bool isProjectile = false; // 이 오브젝트가 투사체인 경우 체크

    private SpriteRenderer[] spriteRenderers;
    private ParticleSystem[] particleSystems;
    private Color[] originalSpriteColors;
    private Color[] originalParticleColors;

    private void Start()
    {
      // 투사체는 투명도 적용 제외
      if (isProjectile) return;

      // SpriteRenderer 수집
      if (applyToSpriteRenderers)
      {
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        originalSpriteColors = new Color[spriteRenderers.Length];
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
          originalSpriteColors[i] = spriteRenderers[i].color;
        }
      }

      // ParticleSystem 수집
      if (applyToParticleSystems)
      {
        particleSystems = GetComponentsInChildren<ParticleSystem>();
        originalParticleColors = new Color[particleSystems.Length];
        for (int i = 0; i < particleSystems.Length; i++)
        {
          var main = particleSystems[i].main;
          originalParticleColors[i] = main.startColor.color;
        }
      }

      // 초기 투명도 적용
      ApplyOpacity();
    }

    private void Update()
    {
      // 투사체는 투명도 적용 제외
      if (isProjectile) return;

      // 매 프레임 투명도 갱신 (설정이 변경될 수 있으므로)
      ApplyOpacity();
    }

    /// <summary>
    /// SettingsManager의 effectOpacity를 모든 이펙트에 적용
    /// </summary>
    private void ApplyOpacity()
    {
      if (SettingsManager.Instance == null) return;

      float opacity = SettingsManager.Instance.effectOpacity;

      // SpriteRenderer 투명도 적용
      if (applyToSpriteRenderers && spriteRenderers != null)
      {
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
          if (spriteRenderers[i] != null)
          {
            Color newColor = originalSpriteColors[i];
            newColor.a = originalSpriteColors[i].a * opacity;
            spriteRenderers[i].color = newColor;
          }
        }
      }

      // ParticleSystem 투명도 적용
      if (applyToParticleSystems && particleSystems != null)
      {
        for (int i = 0; i < particleSystems.Length; i++)
        {
          if (particleSystems[i] != null)
          {
            var main = particleSystems[i].main;
            Color newColor = originalParticleColors[i];
            newColor.a = originalParticleColors[i].a * opacity;
            main.startColor = newColor;
          }
        }
      }
    }
  }
}
