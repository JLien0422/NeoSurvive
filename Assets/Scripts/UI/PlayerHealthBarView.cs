using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class PlayerHealthBarView : MonoBehaviour
{
    [Header("Target Character (Player)")]
    public Character target;

    [Header("Optional: Fill GameObject (0이면 끄기)")]
    public GameObject fillObject;

    private Slider _slider;

    void Awake()
    {
        _slider = GetComponent<Slider>();

        // Fill 자동 탐색(슬라이더 기본 구조면 대부분 찾아짐)
        if (fillObject == null && _slider.fillRect != null)
            fillObject = _slider.fillRect.gameObject;
    }

    void OnEnable()
    {
        if (target != null)
            target.CurrentHP.onValueChanged += HandleHealthChanged;
    }

    void OnDisable()
    {
        if (target != null)
            target.CurrentHP.onValueChanged -= HandleHealthChanged;
    }

    void Start()
    {
        // 시작 시 즉시 동기화(초기 꽉 차게)
        ForceSyncNow();
    }

    private void ForceSyncNow()
    {
        if (target == null || _slider == null) return;

        float max = target.MaxHP != null ? target.MaxHP.GetValue() : 100f;
        float cur = Mathf.Clamp(target.CurrentHP.CurrentValue, 0f, max);
        Apply(cur, max);
    }

    private void HandleHealthChanged(float current, float max)
    {
        Apply(current, max);
    }

    private void Apply(float current, float max)
    {
        if (_slider == null) return;

        max = Mathf.Max(1f, max);
        current = Mathf.Clamp(current, 0f, max);

        _slider.minValue = 0f;
        _slider.maxValue = max;
        _slider.value = current;

        // 0이면 Fill은 아예 안 보이게
        if (fillObject != null)
            fillObject.SetActive(current > 0.001f);
    }
}
