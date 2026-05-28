using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using NeoSurvive.Core;

namespace NeoSurvive.Weapon
{
  [RequireComponent(typeof(Collider2D))]
  public class ChainSaw : MonoBehaviour
  {
    [Header("Damage")]
    public float damage = 6f;
    public float hpRatioDamage = 0.05f;
    public bool useHpRatioDamage = true;
    public bool useCurrentHpRatio = false;
    public float hitCooldown = 0.2f;
    public LayerMask enemyMask;
    public string enemyTag = "Enemy";

    [Header("Movement (Player Rect Bound)")]
    public float speed = 8f;
    public float halfWidth = 6f;
    public float halfHeight = 3.5f;

    [Header("Lifetime")]
    public bool infiniteLifetime = true;
    public float lifetime = 8f;

    [Header("Visual / Size")]
    public float radiusScale = 1.0f;
    public float hitRadius = 0.6f;
    public float spinSpeed = 720f;

    [Header("ChainSaw VFX")]
    public GameObject chainSawVfxPrefab; // ★ 추가: 애니메이션/VFX 프리팹 넣는 칸
    public Vector3 vfxLocalOffset = Vector3.zero; // ★ 추가
    public Vector3 vfxLocalScale = Vector3.one; // ★ 추가
    public bool disableVfxPhysics = true; // ★ 추가: VFX 프리팹 collider/rigidbody 영향 방지

    [Header("Debug")]
    public bool debugDraw = false;
    public bool debugLog = false;

    private const string weaponId = "chainsaw";

    private Transform player;
    private Vector2 moveDir;

    private readonly Dictionary<int, float> lastHitTime = new();

    private float lifeTimer;
    private bool initialized = false;

    private Rigidbody2D rb;
    private Collider2D col;

    private GameObject vfxInstance; // ★ 추가

    private void Awake()
    {
      rb = GetComponent<Rigidbody2D>();
      col = GetComponent<Collider2D>();

      if (col != null)
        col.isTrigger = true;

      if (rb != null)
      {
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.freezeRotation = true;
      }
    }

    private void Start()
    {
      if (player == null)
        player = GetComponentInParent<Player>()?.transform;

      if (player == null)
      {
        Debug.LogError("[ChainSaw] Player not found!");
        enabled = false;
        return;
      }

      transform.SetParent(null, true);

      ApplyStatsFromCSV(1);

      lifeTimer = lifetime;

      moveDir = Random.insideUnitCircle.normalized;

      if (moveDir.sqrMagnitude < 0.001f)
        moveDir = Vector2.right;

      Vector2 startLocal = new Vector2(
        Random.Range(-halfWidth, halfWidth),
        Random.Range(-halfHeight, halfHeight)
      );

      transform.position = player.position + (Vector3)startLocal;

      SpawnVfx(); // ★ 추가

      initialized = true;
    }

    public void SetOwner(Transform owner)
    {
      player = owner;
      transform.SetParent(null, true);
    }

    private void Update()
    {
      if (!initialized) return;

      if (player == null)
      {
        Destroy(gameObject);
        return;
      }

      transform.localScale = Vector3.one * radiusScale;

      if (!infiniteLifetime)
      {
        lifeTimer -= Time.deltaTime;

        if (lifeTimer <= 0f)
        {
          Destroy(gameObject);
          return;
        }
      }

      if (rb != null)
      {
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
      }

      MoveRectBound();
      DamageEnemies();
      SpinVisual();

      if (debugDraw)
        DrawDebugRect();
    }

    private void SpawnVfx()
    {
      if (chainSawVfxPrefab == null) return;
      if (vfxInstance != null) return;

      // ★ ChainSaw의 자식으로 붙임
      // Player 자식이 아니므로 Player 좌우 반전 영향은 받지 않음
      vfxInstance = Instantiate(chainSawVfxPrefab, transform);
      vfxInstance.transform.localPosition = vfxLocalOffset;
      vfxInstance.transform.localRotation = Quaternion.identity;
      vfxInstance.transform.localScale = vfxLocalScale;

      if (disableVfxPhysics)
      {
        Collider2D[] cols = vfxInstance.GetComponentsInChildren<Collider2D>(true);
        foreach (Collider2D c in cols)
          c.enabled = false;

        Rigidbody2D[] bodies = vfxInstance.GetComponentsInChildren<Rigidbody2D>(true);
        foreach (Rigidbody2D body in bodies)
          body.simulated = false;
      }
    }

    private void MoveRectBound()
    {
      Vector3 center = player.position;

      Vector3 nextPos =
        transform.position +
        (Vector3)(moveDir * speed * Time.deltaTime);

      float minX = center.x - halfWidth;
      float maxX = center.x + halfWidth;

      float minY = center.y - halfHeight;
      float maxY = center.y + halfHeight;

      if (nextPos.x > maxX)
      {
        nextPos.x = maxX;
        moveDir.x = -Mathf.Abs(moveDir.x);
      }
      else if (nextPos.x < minX)
      {
        nextPos.x = minX;
        moveDir.x = Mathf.Abs(moveDir.x);
      }

      if (nextPos.y > maxY)
      {
        nextPos.y = maxY;
        moveDir.y = -Mathf.Abs(moveDir.y);
      }
      else if (nextPos.y < minY)
      {
        nextPos.y = Mathf.Clamp(nextPos.y, minY, maxY);
        moveDir.y = Mathf.Abs(moveDir.y);
      }

      moveDir.Normalize();

      transform.position = nextPos;
    }

    private void SpinVisual()
    {
      transform.Rotate(0f, 0f, -spinSpeed * Time.deltaTime);
    }

    private void DamageEnemies()
    {
      Collider2D[] hits = Physics2D.OverlapCircleAll(
        transform.position,
        hitRadius * radiusScale,
        enemyMask
      );

      float now = Time.time;

      foreach (var hit in hits)
      {
        if (hit == null) continue;

        if (!string.IsNullOrEmpty(enemyTag))
        {
          bool okTag =
            hit.CompareTag(enemyTag) ||
            (hit.transform.parent != null &&
             hit.transform.parent.CompareTag(enemyTag));

          if (!okTag) continue;
        }

        Enemy enemy = hit.GetComponentInParent<Enemy>();

        if (enemy == null) continue;

        int id = enemy.gameObject.GetInstanceID();

        if (lastHitTime.TryGetValue(id, out float last) &&
            now - last < hitCooldown)
          continue;

        lastHitTime[id] = now;

        float finalDamage = CalculateDamage(enemy);

        enemy.TakeDamage(finalDamage);

        if (debugLog)
          Debug.Log($"[ChainSaw] Hit {enemy.name} dmg={finalDamage}");
      }
    }

    private float CalculateDamage(Enemy enemy)
    {
      if (!useHpRatioDamage)
        return damage;

      float hpBase =
        useCurrentHpRatio
          ? GetEnemyCurrentHp(enemy)
          : GetEnemyMaxHp(enemy);

      if (hpBase <= 0f)
        return damage;

      return hpBase * hpRatioDamage;
    }

    private float GetEnemyMaxHp(Enemy enemy)
    {
      return ReadFloatMember(enemy, new string[]
      {
        "maxHp",
        "MaxHp",
        "maxHP",
        "MaxHP",
        "maxHealth",
        "MaxHealth"
      });
    }

    private float GetEnemyCurrentHp(Enemy enemy)
    {
      return ReadFloatMember(enemy, new string[]
      {
        "currentHp",
        "CurrentHp",
        "hp",
        "Hp",
        "currentHealth",
        "CurrentHealth"
      });
    }

    private float ReadFloatMember(object target, string[] names)
    {
      if (target == null) return -1f;

      System.Type t = target.GetType();

      foreach (string name in names)
      {
        FieldInfo field =
          t.GetField(
            name,
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic
          );

        if (field != null)
        {
          object value = field.GetValue(target);

          if (value is float f) return f;
          if (value is int i) return i;
        }

        PropertyInfo prop =
          t.GetProperty(
            name,
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic
          );

        if (prop != null && prop.CanRead)
        {
          object value = prop.GetValue(target);

          if (value is float f) return f;
          if (value is int i) return i;
        }
      }

      return -1f;
    }

    public void OnLevelUp(int level)
    {
      ApplyStatsFromCSV(level);

      Debug.Log(
        $"[ChainSaw] Lv.{level} " +
        $"damage={damage}, " +
        $"hpRatioDamage={hpRatioDamage}, " +
        $"speed={speed}, " +
        $"hitCooldown={hitCooldown}, " +
        $"halfWidth={halfWidth}, " +
        $"halfHeight={halfHeight}"
      );
    }

    private void ApplyStatsFromCSV(int level)
    {
      if (WeaponStatLoader.DB == null)
      {
        Debug.LogWarning("[ChainSaw] WeaponStatLoader.DB가 null입니다.");
        return;
      }

      if (!WeaponStatLoader.DB.rows.TryGetValue(
            weaponId,
            out var weaponLevels))
      {
        Debug.LogWarning($"[ChainSaw] CSV에 '{weaponId}'가 없습니다.");
        return;
      }

      int clampedLevel = Mathf.Clamp(level, 1, 5);

      if (!weaponLevels.TryGetValue(clampedLevel, out var row))
      {
        Debug.LogWarning(
          $"[ChainSaw] '{weaponId}'의 level {clampedLevel} 데이터가 없습니다.");
        return;
      }

      if (row.damage > 0f) damage = row.damage;
      if (row.hpratiodamage > 0f) hpRatioDamage = row.hpratiodamage;
      if (row.hitcooldown > 0f) hitCooldown = row.hitcooldown;
      if (row.speed > 0f) speed = row.speed;
      if (row.radiusscale > 0f) radiusScale = row.radiusscale;
      if (row.hitradius > 0f) hitRadius = row.hitradius;
      if (row.halfwidth > 0f) halfWidth = row.halfwidth;
      if (row.halfheight > 0f) halfHeight = row.halfheight;

      Debug.Log(
        $"[ChainSaw] CSV 적용 완료 | " +
        $"Lv={clampedLevel}, " +
        $"damage={damage}, " +
        $"hpRatioDamage={hpRatioDamage}, " +
        $"hitCooldown={hitCooldown}, " +
        $"speed={speed}, " +
        $"radiusScale={radiusScale}, " +
        $"hitRadius={hitRadius}, " +
        $"halfWidth={halfWidth}, " +
        $"halfHeight={halfHeight}"
      );
    }

    private void DrawDebugRect()
    {
      if (player == null) return;

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
  }
}