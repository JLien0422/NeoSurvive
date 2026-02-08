using UnityEngine;

/// <summary>
/// 해킹 오브젝트가 씬에서 눈에 보이도록 하는 최소 연출 스크립트
/// (프리팹 생성용 placeholder)
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class HackableObjectVisual : MonoBehaviour
{
    [Header("간단 연출")]
    [SerializeField] private float rotateSpeed = 90f;
    [SerializeField] private float pulseSpeed = 2.5f;
    [SerializeField] private float pulseScale = 0.08f;

    private Vector3 baseScale;

    private void Awake()
    {
        baseScale = transform.localScale;

        // SpriteRenderer가 없으면 추가(RequireComponent라 웬만하면 없어도 붙음)
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite == null)
        {
            Debug.LogWarning($"[HackableObjectVisual] {name}: Sprite가 비어있습니다. 임시 스프라이트를 넣어주세요.");
        }
    }

    private void Update()
    {
        // 빙글빙글
        transform.Rotate(0f, 0f, rotateSpeed * Time.deltaTime);

        // 두근두근(스케일)
        float s = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseScale;
        transform.localScale = baseScale * s;
    }
}
