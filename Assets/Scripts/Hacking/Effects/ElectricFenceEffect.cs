using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NeoSurvive.Buff;

public class ElectricFenceEffect : MonoBehaviour
{
    [Header("울타리 설정")]
    [SerializeField] private float fenceSize = 4.2f;
    [SerializeField] private float fenceHeight = 0.3f;
    [SerializeField] private float duration = 8f;

    [Header("전개 넉백 설정")]
    [SerializeField] private bool knockbackEnemiesOnSpawn = true;
    [SerializeField] private float spawnKnockbackForce = 14f;
    [SerializeField] private float spawnKnockbackDuration = 0.25f;
    [SerializeField] private float insideCheckPadding = 0.2f;

    [Header("데미지 설정")]
    [SerializeField] private float damagePerSecond = 15f;
    [SerializeField] private float damageInterval = 0.5f;

    [Header("Fence VFX")]
    [SerializeField] private GameObject fenceVfxPrefab;
    [SerializeField] private float fenceVfxBaseLength = 5f;
    [SerializeField] private float horizontalLengthPadding = 2.4f;
    [SerializeField] private float verticalLengthPadding = 2.4f;
    [SerializeField] private float fenceVfxThicknessScale = 1.6f;

    [Header("방향별 VFX 위치 보정")]
    [SerializeField] private Vector2 topVfxOffset = new Vector2(0f, -2f);
    [SerializeField] private Vector2 bottomVfxOffset = new Vector2(0f, 2f);
    [SerializeField] private Vector2 leftVfxOffset = new Vector2(2f, 0f);
    [SerializeField] private Vector2 rightVfxOffset = new Vector2(-2f, 0f);

    private GameObject[] fenceWalls = new GameObject[4];

    private HashSet<StatusFlags> stunnedEnemies =
        new HashSet<StatusFlags>();

    public void Activate()
    {
        StartCoroutine(SpawnFence());
    }

    private IEnumerator SpawnFence()
    {
        Player player = FindObjectOfType<Player>();
        if (player == null)
            yield break;

        Vector3 center = player.transform.position;
        float baseLength = fenceSize * 2f;

        fenceWalls[0] = CreateFenceWall(
            center + new Vector3(0f, -fenceSize, 0f),
            new Vector2(baseLength, fenceHeight),
            0f,
            bottomVfxOffset
        );

        fenceWalls[1] = CreateFenceWall(
            center + new Vector3(fenceSize, 0f, 0f),
            new Vector2(fenceHeight, baseLength),
            90f,
            rightVfxOffset
        );

        fenceWalls[2] = CreateFenceWall(
            center + new Vector3(0f, fenceSize, 0f),
            new Vector2(baseLength, fenceHeight),
            180f,
            topVfxOffset
        );

        fenceWalls[3] = CreateFenceWall(
            center + new Vector3(-fenceSize, 0f, 0f),
            new Vector2(fenceHeight, baseLength),
            270f,
            leftVfxOffset
        );

        if (knockbackEnemiesOnSpawn)
        {
            KnockbackEnemiesInsideFence(center);
        }

        StartCoroutine(DamageCoroutine());

        yield return new WaitForSeconds(duration);

        ReleaseAllStuns();

        foreach (var wall in fenceWalls)
        {
            if (wall != null)
                Destroy(wall);
        }

        Destroy(this);
    }

    private void KnockbackEnemiesInsideFence(Vector3 center)
    {
        Vector2 checkSize =
            new Vector2(
                fenceSize * 2f - insideCheckPadding,
                fenceSize * 2f - insideCheckPadding
            );

        Collider2D[] hits =
            Physics2D.OverlapBoxAll(
                center,
                checkSize,
                0f
            );

        HashSet<Rigidbody2D> knockedBodies =
            new HashSet<Rigidbody2D>();

        foreach (Collider2D hit in hits)
        {
            if (hit == null)
                continue;

            if (!hit.CompareTag("Enemy"))
                continue;

            Rigidbody2D rb =
                hit.GetComponentInParent<Rigidbody2D>();

            if (rb == null)
                continue;

            if (knockedBodies.Contains(rb))
                continue;

            knockedBodies.Add(rb);

            Vector2 dir =
                rb.position - (Vector2)center;

            if (dir.sqrMagnitude < 0.001f)
            {
                dir = Vector2.right;
            }

            dir.Normalize();

            KnockbackDebuff knockback =
                rb.GetComponent<KnockbackDebuff>();

            if (knockback == null)
                knockback = rb.gameObject.AddComponent<KnockbackDebuff>();

            knockback.ApplyKnockback(
                dir * spawnKnockbackForce,
                spawnKnockbackDuration
            );
        }
    }

    private IEnumerator DamageCoroutine()
    {
        float damagePerTick =
            damagePerSecond * damageInterval;

        while (true)
        {
            yield return new WaitForSeconds(damageInterval);

            bool anyAlive = false;

            foreach (var wall in fenceWalls)
            {
                if (wall != null)
                {
                    anyAlive = true;
                    break;
                }
            }

            if (!anyAlive)
            {
                ReleaseAllStuns();
                yield break;
            }

            HashSet<StatusFlags> currentlyTouching =
                new HashSet<StatusFlags>();

            foreach (var wall in fenceWalls)
            {
                if (wall == null)
                    continue;

                BoxCollider2D box =
                    wall.GetComponent<BoxCollider2D>();

                if (box == null)
                    continue;

                Collider2D[] hits =
                    Physics2D.OverlapBoxAll(
                        wall.transform.position,
                        box.size * 1.2f,
                        0f
                    );

                foreach (var hit in hits)
                {
                    if (!hit.CompareTag("Enemy"))
                        continue;

                    Character c =
                        hit.GetComponent<Character>();

                    if (c != null)
                        c.TakeDamage(damagePerTick);

                    StatusFlags flags =
                        hit.GetComponent<StatusFlags>();

                    if (flags == null)
                        flags = hit.gameObject.AddComponent<StatusFlags>();

                    flags.moveBlocked = true;
                    flags.attackBlocked = true;

                    currentlyTouching.Add(flags);
                    stunnedEnemies.Add(flags);
                }
            }

            HashSet<StatusFlags> toRelease =
                new HashSet<StatusFlags>();

            foreach (var flags in stunnedEnemies)
            {
                if (!currentlyTouching.Contains(flags))
                    toRelease.Add(flags);
            }

            foreach (var flags in toRelease)
            {
                ReleaseStun(flags);
                stunnedEnemies.Remove(flags);
            }
        }
    }

    private void ReleaseStun(StatusFlags flags)
    {
        if (flags == null)
            return;

        flags.moveBlocked = false;
        flags.attackBlocked = false;
    }

    private void ReleaseAllStuns()
    {
        foreach (var flags in stunnedEnemies)
        {
            ReleaseStun(flags);
        }

        stunnedEnemies.Clear();
    }

    private GameObject CreateFenceWall(
        Vector3 position,
        Vector2 colliderSize,
        float vfxRotationZ,
        Vector2 vfxOffset)
    {
        GameObject wall =
            new GameObject("ElectricFenceWall");

        wall.transform.position = position;
        wall.transform.rotation = Quaternion.identity;
        wall.transform.localScale = Vector3.one;
        wall.tag = "ElectricFence";

        BoxCollider2D col =
            wall.AddComponent<BoxCollider2D>();

        col.size = colliderSize;
        col.isTrigger = false;

        Rigidbody2D rb =
            wall.AddComponent<Rigidbody2D>();

        rb.bodyType = RigidbodyType2D.Static;

        SpawnFenceVFX(
            wall,
            colliderSize,
            vfxRotationZ,
            vfxOffset
        );

        return wall;
    }

    private void SpawnFenceVFX(
        GameObject wall,
        Vector2 colliderSize,
        float vfxRotationZ,
        Vector2 vfxOffset)
    {
        if (fenceVfxPrefab == null)
            return;

        Vector3 finalPosition =
            wall.transform.position +
            new Vector3(vfxOffset.x, vfxOffset.y, 0f);

        GameObject vfx =
            Instantiate(
                fenceVfxPrefab,
                finalPosition,
                Quaternion.Euler(0f, 0f, vfxRotationZ)
            );

        vfx.transform.SetParent(wall.transform, true);

        bool isHorizontal =
            Mathf.Approximately(vfxRotationZ, 0f) ||
            Mathf.Approximately(vfxRotationZ, 180f);

        float lengthPadding =
            isHorizontal
                ? horizontalLengthPadding
                : verticalLengthPadding;

        float targetLength =
            Mathf.Max(colliderSize.x, colliderSize.y) +
            lengthPadding;

        float lengthScale =
            targetLength /
            Mathf.Max(0.01f, fenceVfxBaseLength);

        vfx.transform.localScale =
            new Vector3(
                lengthScale,
                fenceVfxThicknessScale,
                1f
            );
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        Gizmos.DrawWireCube(
            transform.position,
            new Vector3(
                fenceSize * 2f,
                fenceSize * 2f,
                0.1f
            )
        );
    }
#endif
}