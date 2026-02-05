using System.Collections.Generic;
using UnityEngine;
using NeoSurvive.Core;

namespace NeoSurvive.Weapon
{
  public class ChainSaw : MonoBehaviour
  {
    [Header("Damage")]
    public float damage = 6f;
    public float hitCooldown = 0.2f;
    public LayerMask enemyMask;
    public string enemyTag = "Enemy";

    [Header("Movement (Player Rect Bound)")]
    public float speed = 8f;

    // ✅ 플레이어 기준 직사각형 영역(반쪽 길이)
    public float halfWidth = 6f;    // 좌우
    public float halfHeight = 3.5f; // 상하

    [Header("Lifetime")]
    public float lifetime = 8f;

    [Header("Visual / Size")]
    public float radiusScale = 1.0f;
    public float hitRadius = 0.6f;

    [Header("Level Scaling")]
    public float damagePerLevel = 0.15f;
    public float speedPerLevel = 0.10f;
    public float sizePerLevel = 0.12f;
    public float cooldownMulPerLevel = 0.92f;
    public float lifetimePerLevel = 0.15f;

    [Header("Master (Lv5)")]
    public bool enableMaster = true;
    public float masterSizeFactor = 1.25f;

    [Header("Debug")]
    public bool debugDraw = false;
    public bool debugLog = false;

    private Transform player;
    private Vector2 moveDir;

    private float baseDamage;
    private float baseSpeed;
    private float baseCooldown;
    private float baseLifetime;
    private float baseSize;

    private int currentLevel = 1;

    private readonly Dictionary<int, float> lastHitTime = new();

    // ✅ 수명 안전 처리용
    private float lifeTimer;
    private bool dying = false;

    private void Start()
    {
      player = GetComponentInParent<Player>()?.transform;
      if (player == null)
      {
        Debug.LogError("[ChainSaw] Player not found! (ChainSaw가 Player의 자식으로 생성되었는지 확인)");
        enabled = false;
        return;
      }

      baseDamage = damage;
      baseSpeed = speed;
      baseCooldown = hitCooldown;
      baseLifetime = lifetime;
      baseSize = radiusScale;

      ApplyLevel(1);

      // ✅ 수명 타이머 (Destroy 예약 대신)
      lifeTimer = lifetime;

      // 랜덤 초기 방향
      moveDir = Random.insideUnitCircle.normalized;
      if (moveDir.sqrMagnitude < 0.01f) moveDir = Vector2.right;
    }

    private void Update()
    {
      // ✅ 이미 종료 처리 시작했으면 아무 것도 하지 않음
      if (dying) return;

      // ✅ 플레이어가 사라졌으면 즉시 종료 (씬 리셋/사망 등)
      if (player == null)
      {
        dying = true;
        Destroy(gameObject);
        return;
      }

      // ✅ 수명 처리
      lifeTimer -= Time.deltaTime;
      if (lifeTimer <= 0f)
      {
        dying = true;
        Destroy(gameObject);
        return;
      }

      MoveRectBound();
      DamageEnemies();

      if (debugDraw) DrawDebugRect();
    }

    private void MoveRectBound()
    {
      Vector3 center = player.position;

      // 다음 위치 예측
      Vector3 nextPos = transform.position + (Vector3)(moveDir * speed * Time.deltaTime);

      // 플레이어 기준 로컬 오프셋으로 판단
      Vector3 local = nextPos - center;

      bool flipped = false;

      // X 경계
      if (local.x > halfWidth)
      {
        moveDir.x = -Mathf.Abs(moveDir.x);
        flipped = true;
      }
      else if (local.x < -halfWidth)
      {
        moveDir.x = Mathf.Abs(moveDir.x);
        flipped = true;
      }

      // Y 경계
      if (local.y > halfHeight)
      {
        moveDir.y = -Mathf.Abs(moveDir.y);
        flipped = true;
      }
      else if (local.y < -halfHeight)
      {
        moveDir.y = Mathf.Abs(moveDir.y);
        flipped = true;
      }

      // 경계 밖에 박히는 것 방지: 튕겼으면 새 방향으로 다시 계산
      if (flipped)
      {
        nextPos = transform.position + (Vector3)(moveDir * speed * Time.deltaTime);
      }

      transform.position = nextPos;
    }

    private void DamageEnemies()
    {
      Collider2D[] hits = Physics2D.OverlapCircleAll(
        transform.position,
        hitRadius * radiusScale,
        enemyMask
      );

      float now = Time.time;

      foreach (var col in hits)
      {
        if (col == null) continue;

        if (!string.IsNullOrEmpty(enemyTag))
        {
          bool okTag =
            col.CompareTag(enemyTag) ||
            (col.transform.parent != null && col.transform.parent.CompareTag(enemyTag));
          if (!okTag) continue;
        }

        Enemy enemy = col.GetComponentInParent<Enemy>();
        if (enemy == null) continue;

        int id = enemy.gameObject.GetInstanceID();
        if (lastHitTime.TryGetValue(id, out float last) && now - last < hitCooldown)
          continue;

        lastHitTime[id] = now;

        enemy.TakeDamage(damage);

        if (debugLog)
          Debug.Log($"[ChainSaw] Hit {enemy.name} dmg={damage} (lv={currentLevel})");
      }
    }

    // WeaponManager → SendMessage("OnLevelUp")
    public void OnLevelUp(int level)
    {
      // base값 방어(혹시 Start 전에 호출될 경우 대비)
      if (baseDamage <= 0f && damage > 0f) baseDamage = damage;
      if (baseSpeed <= 0f && speed > 0f) baseSpeed = speed;
      if (baseCooldown <= 0f && hitCooldown > 0f) baseCooldown = hitCooldown;
      if (baseLifetime <= 0f && lifetime > 0f) baseLifetime = lifetime;
      if (baseSize <= 0f && radiusScale > 0f) baseSize = radiusScale;

      ApplyLevel(level);

      // ✅ 현재 남은 수명도 “레벨 기준 수명”으로 맞추고 싶으면 아래 한 줄 사용(선택)
      // lifeTimer = Mathf.Min(lifeTimer, lifetime);

      Debug.Log($"[ChainSaw] Lv.{currentLevel} dmg={damage} speed={speed} size={radiusScale} cd={hitCooldown} life={lifetime}");
    }

    private void ApplyLevel(int level)
    {
      currentLevel = Mathf.Clamp(level, 1, 5);

      damage = baseDamage * (1f + (currentLevel - 1) * damagePerLevel);
      speed = baseSpeed * (1f + (currentLevel - 1) * speedPerLevel);
      hitCooldown = baseCooldown * Mathf.Pow(cooldownMulPerLevel, (currentLevel - 1));
      lifetime = baseLifetime * (1f + (currentLevel - 1) * lifetimePerLevel);
      radiusScale = baseSize * (1f + (currentLevel - 1) * sizePerLevel);

      if (enableMaster && currentLevel >= 5)
      {
        radiusScale *= masterSizeFactor;
      }

      transform.localScale = Vector3.one * radiusScale;
    }

    private void DrawDebugRect()
    {
      Vector3 c = player.position;

      Vector3 a = c + new Vector3(-halfWidth, -halfHeight, 0);
      Vector3 b = c + new Vector3( halfWidth, -halfHeight, 0);
      Vector3 d = c + new Vector3(-halfWidth,  halfHeight, 0);
      Vector3 e = c + new Vector3( halfWidth,  halfHeight, 0);

      Debug.DrawLine(a, b, Color.yellow);
      Debug.DrawLine(b, e, Color.yellow);
      Debug.DrawLine(e, d, Color.yellow);
      Debug.DrawLine(d, a, Color.yellow);
    }

    private void OnDrawGizmosSelected()
    {
      // 히트 반경
      Gizmos.color = Color.cyan;
      Gizmos.DrawWireSphere(transform.position, hitRadius * radiusScale);

      // 플레이어 영역(에디터에서만)
      Transform p = player != null ? player : (GetComponentInParent<Player>()?.transform);
      if (p != null)
      {
        Gizmos.color = Color.yellow;
        Vector3 c = p.position;

        Vector3 center = c;
        Vector3 size = new Vector3(halfWidth * 2f, halfHeight * 2f, 0.01f);

        Gizmos.DrawWireCube(center, size);
      }
    }
  }
}
