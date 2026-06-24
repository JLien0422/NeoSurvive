using UnityEngine;
using NeoSurvive.Characters;

/// <summary>
/// 해킹 프롬프트 우상단 위치·범위 계산 공통 유틸.
/// </summary>
public static class HackPromptBoundsUtility
{
    public static bool TryGetCombinedBounds(Transform root, out Bounds bounds)
    {
        bounds = default;
        bool hasBounds = false;

        SpriteRenderer[] renderers = root.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer sr in renderers)
        {
            if (sr == null || !sr.enabled)
                continue;

            if (!hasBounds)
            {
                bounds = sr.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(sr.bounds);
            }
        }

        if (hasBounds)
            return true;

        Collider2D[] colliders = root.GetComponentsInChildren<Collider2D>(true);
        foreach (Collider2D col in colliders)
        {
            if (col == null || !col.enabled)
                continue;

            if (!hasBounds)
            {
                bounds = col.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(col.bounds);
            }
        }

        return hasBounds;
    }

    public static float GetInteractRadius(Transform root, float fallback = 3f)
    {
        if (!TryGetCombinedBounds(root, out Bounds bounds))
            return fallback;

        return Mathf.Max(bounds.extents.x, bounds.extents.y);
    }

    public static Vector3 GetTopRightWorld(Transform root, Vector2 worldOffset)
    {
        SpriteRenderer rootRenderer = root.GetComponent<SpriteRenderer>();
        if (rootRenderer != null && rootRenderer.enabled)
        {
            Bounds bounds = rootRenderer.bounds;
            return new Vector3(
                bounds.max.x + worldOffset.x,
                bounds.max.y + worldOffset.y,
                root.position.z
            );
        }

        if (!TryGetCombinedBounds(root, out Bounds combinedBounds))
        {
            return root.position + new Vector3(worldOffset.x, worldOffset.y, 0f);
        }

        return new Vector3(
            combinedBounds.max.x + worldOffset.x,
            combinedBounds.max.y + worldOffset.y,
            root.position.z
        );
    }

    public static bool IsPlayerWithinPromptRange(
        Transform origin,
        float interactRange,
        Player player,
        float visibilityMultiplier)
    {
        if (origin == null || player == null || interactRange <= 0f)
            return false;

        float promptRange = interactRange * Mathf.Max(1f, visibilityMultiplier);
        float sqrDist = (origin.position - player.transform.position).sqrMagnitude;
        return sqrDist <= promptRange * promptRange;
    }

    public static Player FindPlayer()
    {
        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject != null)
        {
            Player taggedPlayer = playerObject.GetComponent<Player>();
            if (taggedPlayer != null)
                return taggedPlayer;
        }

        Player[] players = Object.FindObjectsOfType<Player>();
        return players.Length > 0 ? players[0] : null;
    }
}
