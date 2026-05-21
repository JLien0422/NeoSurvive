using System.Collections;
using UnityEngine;

/// <summary>
/// SpriteHitFlash ???(_FlashAmount, _FlashColor)? ?? ??.
/// SpriteRenderer + Mat_SpriteHitFlash ??. Flash Color? ?????? Inspector?? ??.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteHitFlash : MonoBehaviour
{
    private static readonly int FlashAmountId = Shader.PropertyToID("_FlashAmount");
    private static readonly int FlashColorId = Shader.PropertyToID("_FlashColor");

    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float flashDuration = 0.08f;
    [SerializeField] private SpriteRenderer[] spriteRenderers;

    private MaterialPropertyBlock _mpb;
    private Coroutine _flashRoutine;

    private void Awake()
    {
        _mpb = new MaterialPropertyBlock();
        if (spriteRenderers == null || spriteRenderers.Length == 0)
            spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
    }

    public void PlayFlash()
    {
        if (_flashRoutine != null)
            StopCoroutine(_flashRoutine);
        _flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        SetFlash(1f);
        yield return new WaitForSeconds(flashDuration);
        SetFlash(0f);
        _flashRoutine = null;
    }

    private void SetFlash(float amount)
    {
        foreach (var sr in spriteRenderers)
        {
            if (sr == null) continue;
            sr.GetPropertyBlock(_mpb);
            _mpb.SetColor(FlashColorId, flashColor);
            _mpb.SetFloat(FlashAmountId, amount);
            sr.SetPropertyBlock(_mpb);
        }
    }
}
