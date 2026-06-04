using System.Collections;
using UnityEngine;

public class SecurityTurretEffect : MonoBehaviour
{
    [Header("터렛 설정")]
    [SerializeField] private float attackRange = 10f;

    [Tooltip("초당 발사 수. 10이면 1초에 10발")]
    [SerializeField] private float attackSpeed = 12f;

    [SerializeField] private float bulletDamage = 20f;
    [SerializeField] private float bulletSpeed = 15f;
    [SerializeField] private float turretDuration = 10f;

    [Header("비주얼")]
    [SerializeField] private Color bulletColor = new Color(0f, 1f, 0.5f, 1f);
    [SerializeField] private Color turretActiveColor = new Color(0f, 1f, 0.5f, 1f);

    [Header("터렛 애니메이션 VFX")]
    [SerializeField] private GameObject spawnVfxPrefab;
    [SerializeField] private GameObject shootVfxPrefab;
    [SerializeField] private float spawnVfxLifetime = 0.8f;
    [SerializeField] private float vfxScale = 1f;

    private bool isActive = false;
    private GameObject shootVfxInstance;
    private float fireElapsed = 0f;

    public void Activate()
    {
        if (isActive) return;
        isActive = true;

        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
            sr.color = turretActiveColor;

        SpawnOneShotVFX(spawnVfxPrefab, transform.position, spawnVfxLifetime);
        SpawnPersistentShootVFX();

        StartCoroutine(FireCoroutine());
    }

    private IEnumerator FireCoroutine()
    {
        float elapsed = 0f;

        while (elapsed < turretDuration)
        {
            elapsed += Time.deltaTime;
            fireElapsed += Time.deltaTime;

            Transform target = FindClosestEnemy();

            if (target != null && fireElapsed >= 1f / attackSpeed)
            {
                FireBullet(target);
                fireElapsed = 0f;
            }

            yield return null;
        }

        if (shootVfxInstance != null)
            Destroy(shootVfxInstance);

        Destroy(gameObject);
    }

    private Transform FindClosestEnemy()
    {
        Collider2D[] cols =
            Physics2D.OverlapCircleAll(
                transform.position,
                attackRange
            );

        Transform closest = null;
        float closestDistance = float.MaxValue;

        foreach (var col in cols)
        {
            if (!col.CompareTag("Enemy"))
                continue;

            float dist =
                Vector2.Distance(
                    transform.position,
                    col.transform.position
                );

            if (dist < closestDistance)
            {
                closestDistance = dist;
                closest = col.transform;
            }
        }

        return closest;
    }

    private void FireBullet(Transform target)
    {
        if (target == null)
            return;

        GameObject bullet = new GameObject("TurretBullet");
        bullet.transform.position = transform.position;

        SpriteRenderer sr = bullet.AddComponent<SpriteRenderer>();
        sr.color = bulletColor;
        sr.sortingOrder = 8;

        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();

        sr.sprite = Sprite.Create(
            tex,
            new Rect(0, 0, 1, 1),
            new Vector2(0.5f, 0.5f),
            1f
        );

        bullet.transform.localScale = Vector3.one * 0.2f;

        Rigidbody2D rb = bullet.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        Vector2 dir =
            ((Vector2)target.position - (Vector2)transform.position).normalized;

        rb.velocity = dir * bulletSpeed;

        TurretBullet bulletComp = bullet.AddComponent<TurretBullet>();
        bulletComp.Initialize(bulletDamage);

        Destroy(bullet, 3f);
    }

    private void SpawnPersistentShootVFX()
    {
        if (shootVfxPrefab == null)
            return;

        shootVfxInstance =
            Instantiate(
                shootVfxPrefab,
                transform.position,
                Quaternion.identity
            );

        shootVfxInstance.transform.SetParent(transform);
        shootVfxInstance.transform.localPosition = Vector3.zero;
        shootVfxInstance.transform.localRotation = Quaternion.identity;
        shootVfxInstance.transform.localScale = Vector3.one * vfxScale;
    }

    private void SpawnOneShotVFX(GameObject prefab, Vector3 pos, float lifetime)
    {
        if (prefab == null)
            return;

        GameObject vfx =
            Instantiate(
                prefab,
                pos,
                Quaternion.identity
            );

        vfx.transform.localScale =
            Vector3.one * vfxScale;

        Destroy(vfx, lifetime);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
#endif
}

public class TurretBullet : MonoBehaviour
{
    private float damage;

    public void Initialize(float dmg)
    {
        damage = dmg;

        CircleCollider2D col =
            gameObject.AddComponent<CircleCollider2D>();

        col.isTrigger = true;
        col.radius = 0.15f;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Enemy"))
            return;

        Character c =
            other.GetComponent<Character>();

        if (c != null)
            c.TakeDamage(damage);

        Destroy(gameObject);
    }
}