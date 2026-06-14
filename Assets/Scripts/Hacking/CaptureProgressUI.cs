using UnityEngine;
using TMPro;

public class CaptureProgressUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("Bar")]
    [SerializeField] private Transform fillBar;

    [Header("Text")]
    [SerializeField] private TMP_Text percentText;

    [Header("Settings")]
    [SerializeField] private float fullWidth = 1f;

    private void Awake()
    {
        ResetProgress();
    }

    public void Show()
    {
        if (root != null)
            root.SetActive(true);
        else
            gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (root != null)
            root.SetActive(false);
        else
            gameObject.SetActive(false);
    }

    public void SetProgress(float normalized)
    {
        normalized = Mathf.Clamp01(normalized);

        if (fillBar != null)
        {
            Vector3 scale = fillBar.localScale;
            scale.x = fullWidth * normalized;
            fillBar.localScale = scale;
        }

        if (percentText != null)
        {
            percentText.text = $"{normalized * 100f:0}%";
        }
    }

    public void ResetProgress()
    {
        SetProgress(0f);
        Hide();
    }

    public void ShowCompleted()
    {
        Show();
        SetProgress(1f);

        if (percentText != null)
            percentText.text = "점령 완료";
    }
}