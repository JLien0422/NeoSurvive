using NeoSurvive.Characters;
using UnityEngine;

/// <summary>
/// 사이코 잠식도 6칸 글리치 미터 UI.
/// 임계값(15/30/45/60/80/100) 이상일 때 해당 칸의 FillGlitch를 표시합니다.
/// </summary>
public class PsychoMeterUI : MonoBehaviour
{
  [System.Serializable]
  public class Segment
  {
    public GameObject fillGlitchHacker;
    public GameObject fillGlitchCyborg;
  }

  [SerializeField] private Segment[] segments = new Segment[6];
  [SerializeField] private float[] thresholds = { 15f, 30f, 45f, 60f, 80f, 100f };

  private CharacterType characterType = CharacterType.Hacker;

  public void SetCharacter(CharacterType type)
  {
    characterType = type;
    DisableAllFills();
  }

  public void SetCorruption(float current)
  {
    int count = Mathf.Min(segments.Length, thresholds.Length);
    for (int i = 0; i < count; i++)
    {
      Segment segment = segments[i];
      if (segment == null)
        continue;

      bool filled = current >= thresholds[i];
      GameObject fill = characterType == CharacterType.Cyborg
        ? segment.fillGlitchCyborg
        : segment.fillGlitchHacker;

      if (fill != null)
        fill.SetActive(filled);
    }
  }

  private void DisableAllFills()
  {
    if (segments == null)
      return;

    foreach (Segment segment in segments)
    {
      if (segment == null)
        continue;

      if (segment.fillGlitchHacker != null)
        segment.fillGlitchHacker.SetActive(false);
      if (segment.fillGlitchCyborg != null)
        segment.fillGlitchCyborg.SetActive(false);
    }
  }
}
