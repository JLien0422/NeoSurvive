using System.Collections;
using UnityEngine;

/// <summary>
/// 보안 터렛 해킹 성공 효과
/// 해킹된 터렛이 아군화되어 가장 가까운 적부터 순차적으로 고속 연사를 퍼붓습니다.
/// HackableObject(보안 터렛 프리팹)에 붙어 있으며, 성공 시 Activate() 호출로 활성화됩니다.
/// </summary>
public class SecurityTurretEffect : MonoBehaviour
{
    [Header("터렛 설정")]
    [SerializeField] private float attackRange    = 10f;   // 공격 사거리
    [SerializeField] private float fireRate       = 0.15f; // 발사 간격 (초) - 고속 연사
    [SerializeField] private float bulletDamage   = 20f;   // 탄환 1발 데미지
    [SerializeField] private float bulletSpeed    = 15f;   // 탄환 속도
    [SerializeField] private float turretDuration = 10f;   // 터렛 작동 지속 시간 (초)

    [Header("비주얼")]
    [SerializeField] private Color bulletColor = new Color(0f, 1f, 0.5f, 1f); // 아군화 녹색 탄환
    [SerializeField] private Color turretActiveColor = new Color(0f, 1f, 0.5f, 1f); // 아군화 시 터렛 색상

    private bool isActive = false;

    /// <summary>
    /// 외부(HackableObject)에서 호출하여 터렛을 아군화 + 활성화합니다.
    /// </summary>
    public void Activate()
    {
        if (isActive) return;
        isActive = true;

        // 터렛 색상을 아군화(녹색) 으로 변경
        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.color = turretActiveColor;

        StartCoroutine(FireCoroutine());
    }

    /// <summary>
    /// 지속적으로 가장 가까운 적을 찾아 탄환을 발사하는 코루틴
    /// </summary>
    private IEnumerator FireCoroutine()
    {
        float elapsed = 0f;

        while (elapsed < turretDuration)
        {
            elapsed += fireRate;
            yield return new WaitForSeconds(fireRate);

            // 사거리 내 가장 가까운 적 탐색
            Transform target = FindClosestEnemy();
            if (target == null) continue;

            // 탄환 발사
            FireBullet(target);
        }

        // 터렛 수명 종료 → 오브젝트 제거
        Debug.Log("[SecurityTurretEffect] 터렛 작동 종료");
        Destroy(gameObject);
    }

    /// <summary>
    /// 사거리 내 가장 가까운 적(Enemy 태그)을 반환합니다.
    /// </summary>
    private Transform FindClosestEnemy()
    {
        Collider2D[] cols = Physics2D.OverlapCircleAll(transform.position, attackRange);
        Transform closest     = null;
        float closestDistance = float.MaxValue;

        foreach (var col in cols)
        {
            if (!col.CompareTag("Enemy")) continue;
            float dist = Vector2.Distance(transform.position, col.transform.position);
            if (dist < closestDistance)
            {
                closestDistance = dist;
                closest = col.transform;
            }
        }

        return closest;
    }

    /// <summary>
    /// 타겟을 향해 탄환을 생성하고 발사합니다.
    /// </summary>
    private void FireBullet(Transform target)
    {
        if (target == null) return;

        // 탄환 오브젝트 생성
        GameObject bullet = new GameObject("TurretBullet");
        bullet.transform.position = transform.position;

        // 탄환 비주얼 (작은 원형 스프라이트)
        SpriteRenderer sr = bullet.AddComponent<SpriteRenderer>();
        sr.color        = bulletColor;
        sr.sortingOrder = 8;
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        bullet.transform.localScale = Vector3.one * 0.2f;

        // 물리 이동
        Rigidbody2D rb = bullet.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        Vector2 dir = ((Vector2)target.position - (Vector2)transform.position).normalized;
        rb.velocity = dir * bulletSpeed;

        // 데미지 처리 컴포넌트
        TurretBullet bulletComp = bullet.AddComponent<TurretBullet>();
        bulletComp.Initialize(bulletDamage);

        // 일정 시간 후 탄환 자동 제거 (화면 밖으로 나갔을 때)
        Destroy(bullet, 3f);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
#endif
}

/// <summary>
/// 터렛 탄환 컴포넌트 - 적과 충돌 시 데미지를 줍니다.
/// </summary>
public class TurretBullet : MonoBehaviour
{
    private float damage;

    public void Initialize(float dmg)
    {
        damage = dmg;

        // 탄환 콜라이더 추가
        CircleCollider2D col = gameObject.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius    = 0.15f;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 적에게 닿으면 데미지 후 탄환 제거
        if (!other.CompareTag("Enemy")) return;

        Character c = other.GetComponent<Character>();
        if (c != null) c.TakeDamage(damage);

        Destroy(gameObject);
    }
}
