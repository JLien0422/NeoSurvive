using UnityEngine;

namespace NeoSurvive.Weapon
{
  public class LightningStrike : MonoBehaviour
  {
    [Header("Scale")]
    [SerializeField]
    private float randomScaleMin = 0.8f;

    [SerializeField]
    private float randomScaleMax = 1.2f;

    private void Start()
    {
      float scale =
        Random.Range(
          randomScaleMin,
          randomScaleMax
        );

      transform.localScale =
        new Vector3(scale, scale, 1f);
    }
  }
}