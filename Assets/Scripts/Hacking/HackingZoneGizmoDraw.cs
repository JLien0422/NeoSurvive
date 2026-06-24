using UnityEngine;

/// <summary>
/// 사이보그 수비 존 Scene/Gizmo 공통 그리기
/// </summary>
public static class HackingZoneGizmoDraw
{
    public static float ResolveCyborgZoneRadius(float fallback = 2.5f)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            HackingSystem system = Object.FindObjectOfType<HackingSystem>();
            if (system != null)
                return system.CyborgZoneRadius;
        }
#endif

        if (HackingSystem.Instance != null)
            return HackingSystem.Instance.CyborgZoneRadius;

        return fallback;
    }

    public static void DrawWireCircle(Vector3 center, float radius, Color color, int segments = 48)
    {
        if (radius <= 0f)
            return;

        Gizmos.color = color;

        Vector3 previous = center + new Vector3(radius, 0f, 0f);
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            Vector3 next = center + new Vector3(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius,
                0f
            );
            Gizmos.DrawLine(previous, next);
            previous = next;
        }
    }
}
