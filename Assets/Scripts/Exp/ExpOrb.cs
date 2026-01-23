using UnityEngine;

namespace NeoSurvive.Exp
{
  /// <summary>
  /// 경험치 오브 (플레이어가 닿으면 경험치 지급 후 파괴)
  /// </summary>
  public class ExpOrb : MonoBehaviour
  {
    [SerializeField] private int amount = 1;

    public void SetAmount(int value)
    {
      amount = Mathf.Max(1, value);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
      if (!other.CompareTag("Player")) return;
      Player player = other.GetComponent<Player>();
      if (player != null)
      {
        player.GainExperience(amount);
        Destroy(gameObject);
      }
    }
  }
}

