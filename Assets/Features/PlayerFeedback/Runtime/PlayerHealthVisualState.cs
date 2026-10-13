using UnityEngine;

public enum PlayerHealthVisualState
{
  Healthy,
  Caution,
  Danger,
  Critical
}

public static class PlayerHealthVisualStateUtility
{
  public static PlayerHealthVisualState Evaluate(float currentHealth, float maximumHealth)
  {
    float ratio = maximumHealth > 0f
      ? Mathf.Clamp01(currentHealth / maximumHealth)
      : 0f;

    if (ratio > 0.75f) return PlayerHealthVisualState.Healthy;
    if (ratio > 0.50f) return PlayerHealthVisualState.Caution;
    if (ratio > 0.25f) return PlayerHealthVisualState.Danger;
    return PlayerHealthVisualState.Critical;
  }

  public static void GetPlayerHitShake(
    PlayerHealthVisualState state,
    out float duration,
    out float magnitude)
  {
    switch (state)
    {
      case PlayerHealthVisualState.Caution:
        duration = 0.09f;
        magnitude = 0.055f;
        break;
      case PlayerHealthVisualState.Danger:
        duration = 0.10f;
        magnitude = 0.07f;
        break;
      case PlayerHealthVisualState.Critical:
        duration = 0.12f;
        magnitude = 0.09f;
        break;
      default:
        duration = 0.08f;
        magnitude = 0.045f;
        break;
    }
  }
}
