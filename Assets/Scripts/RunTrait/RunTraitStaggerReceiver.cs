using UnityEngine;
using NeoSurvive.Buff;

/// <summary>대상 전체에 내부 쿨을 공유합니다. 기존 기절/속박 플래그를 덮어쓰지 않습니다.</summary>
public class RunTraitStaggerReceiver : MonoBehaviour
{
    private float nextStagger;
    public void TryStagger(float duration, float cooldown)
    {
        if (Time.time < nextStagger) return;
        nextStagger = Time.time + Mathf.Max(duration, cooldown);
        var flags = GetComponent<StatusFlags>() ?? gameObject.AddComponent<StatusFlags>();
        flags.runTraitStaggerUntil = Mathf.Max(flags.runTraitStaggerUntil, Time.time + duration);
    }
    private void OnDisable()
    {
        nextStagger = 0f;
        var flags = GetComponent<StatusFlags>();
        if (flags != null) flags.runTraitStaggerUntil = 0f;
    }
}
