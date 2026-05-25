using UnityEngine;
using NeoSurvive.Core;

namespace NeoSurvive.Weapon
{
  public class DroneLaserNet : MonoBehaviour
  {
    [Header("Visual")]
    [SerializeField] private LineRenderer lineRenderer;

    private Transform startPoint;
    private Transform endPoint;

    private float damagePerTick;
    private float tickInterval;
    private float laserWidth;
    private float tickTimer;

    private WeaponBase sourceWeapon;

    public void Initialize(
      Transform startPoint,
      Transform endPoint,
      float damagePerTick,
      float tickInterval,
      float laserWidth,
      WeaponBase sourceWeapon)
    {
      this.startPoint = startPoint;
      this.endPoint = endPoint;
      this.damagePerTick = damagePerTick;
      this.tickInterval = tickInterval;
      this.laserWidth = laserWidth;
      this.sourceWeapon = sourceWeapon;

      // =========================
      // ★ LineRenderer 자동 연결
      // =========================
      if (lineRenderer == null)
      {
        lineRenderer = GetComponent<LineRenderer>();
      }

      // =========================
      // ★ 레이저 기본 설정
      // =========================
      if (lineRenderer != null)
      {
        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = laserWidth;
        lineRenderer.endWidth = laserWidth;
        lineRenderer.useWorldSpace = true;
      }
    }

    private void Update()
    {
      // =========================
      // ★ 연결 대상이 사라지면 제거
      // =========================
      if (startPoint == null || endPoint == null)
      {
        Destroy(gameObject);
        return;
      }

      // =========================
      // ★ 레이저 위치 갱신
      // =========================
      if (lineRenderer != null)
      {
        lineRenderer.SetPosition(0, startPoint.position);
        lineRenderer.SetPosition(1, endPoint.position);
      }

      // =========================
      // ★ 지속딜 Tick 처리
      // =========================
      tickTimer += Time.deltaTime;

      if (tickTimer >= tickInterval)
      {
        ApplyLaserDamage();
        tickTimer = 0f;
      }
    }

    private void ApplyLaserDamage()
    {
      Vector2 a = startPoint.position;
      Vector2 b = endPoint.position;

      Vector2 center = (a + b) * 0.5f;
      float halfLength = Vector2.Distance(a, b) * 0.5f;

      Collider2D[] enemies = Physics2D.OverlapCircleAll(
        center,
        halfLength + laserWidth,
        LayerMask.GetMask("Enemy")
      );

      foreach (Collider2D enemy in enemies)
      {
        Vector2 enemyPos = enemy.transform.position;

        float distanceToLine =
          DistancePointToSegment(enemyPos, a, b);

        // =========================
        // ★ 레이저 폭 안에 들어온 적만 피해
        // =========================
        if (distanceToLine > laserWidth)
          continue;

        if (enemy.TryGetComponent<Character>(out Character character))
        {
          character.TakeDamage(damagePerTick, sourceWeapon);
        }
      }
    }

    private float DistancePointToSegment(Vector2 point, Vector2 a, Vector2 b)
    {
      Vector2 ab = b - a;

      if (ab.sqrMagnitude <= 0.0001f)
      {
        return Vector2.Distance(point, a);
      }

      float t =
        Vector2.Dot(point - a, ab) / ab.sqrMagnitude;

      t = Mathf.Clamp01(t);

      Vector2 closest = a + t * ab;

      return Vector2.Distance(point, closest);
    }
  }
}