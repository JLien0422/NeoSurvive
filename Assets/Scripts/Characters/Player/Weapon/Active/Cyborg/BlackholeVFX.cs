using UnityEngine;

namespace NeoSurvive.Weapon
{
  public class BlackholeVFX : MonoBehaviour
  {
    [Header("Life")]
    public float lifetime = 2f;

    [Header("Spin")]
    public float rotateSpeed = 360f; // degrees/sec

    [Header("Pulse")]
    public float pulseScale = 0.12f;
    public float pulseSpeed = 6f;

    private float t;
    private Vector3 baseScale;

    private void Awake()
    {
      baseScale = transform.localScale;
    }

    private void Update()
    {
      t += Time.deltaTime;

      // 회전
      transform.Rotate(0, 0, rotateSpeed * Time.deltaTime);

      // 맥동(살짝 커졌다 작아졌다)
      float s = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseScale;
      transform.localScale = baseScale * s;

      // 수명 끝나면 제거 (GravityHammer에서도 Destroy하지만, 안전장치)
      if (t >= lifetime) Destroy(gameObject);
    }
  }
}
