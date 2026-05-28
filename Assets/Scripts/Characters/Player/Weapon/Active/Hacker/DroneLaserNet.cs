using System.Collections.Generic;
using UnityEngine;
using NeoSurvive.Core;

namespace NeoSurvive.Weapon
{
  public class DroneLaserNet : MonoBehaviour
  {
    [Header("Visual")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private int circleSegmentCount = 96;

    private List<Transform> dronePoints = new List<Transform>();

    private float damagePerTick;
    private float tickInterval;
    private float laserWidth;
    private float tickTimer;

    private Vector3 circleCenter;
    private float circleRadius;

    private WeaponBase sourceWeapon;

    public void Initialize(
      List<Transform> dronePoints,
      float damagePerTick,
      float tickInterval,
      float laserWidth,
      WeaponBase sourceWeapon)
    {
      this.dronePoints = dronePoints;
      this.damagePerTick = damagePerTick;
      this.tickInterval = tickInterval;
      this.laserWidth = laserWidth;
      this.sourceWeapon = sourceWeapon;

      if (lineRenderer == null)
      {
        lineRenderer = GetComponent<LineRenderer>();
      }

      if (lineRenderer != null)
      {
        lineRenderer.positionCount = circleSegmentCount + 1;
        lineRenderer.startWidth = laserWidth;
        lineRenderer.endWidth = laserWidth;
        lineRenderer.useWorldSpace = true;
        lineRenderer.loop = true;
        lineRenderer.enabled = true;
        lineRenderer.sortingOrder = 20;

        if (lineRenderer.material == null)
        {
          lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        }
      }
    }

    private void Update()
    {
      RemoveNullDrones();

      if (dronePoints.Count < 2)
      {
        Destroy(gameObject);
        return;
      }

      UpdateCircleData();
      DrawCircle();

      tickTimer += Time.deltaTime;

      if (tickTimer >= tickInterval)
      {
        ApplyLaserDamage();
        tickTimer = 0f;
      }
    }

    private void RemoveNullDrones()
    {
      for (int i = dronePoints.Count - 1; i >= 0; i--)
      {
        if (dronePoints[i] == null)
        {
          dronePoints.RemoveAt(i);
        }
      }
    }

    private void UpdateCircleData()
    {
      circleCenter = Vector3.zero;

      for (int i = 0; i < dronePoints.Count; i++)
      {
        circleCenter += dronePoints[i].position;
      }

      circleCenter /= dronePoints.Count;

      circleRadius = 0f;

      for (int i = 0; i < dronePoints.Count; i++)
      {
        float distance = Vector3.Distance(
          circleCenter,
          dronePoints[i].position
        );

        if (distance > circleRadius)
        {
          circleRadius = distance;
        }
      }
    }

    private void DrawCircle()
    {
      if (lineRenderer == null)
        return;

      for (int i = 0; i <= circleSegmentCount; i++)
      {
        float angle =
          ((float)i / circleSegmentCount) * Mathf.PI * 2f;

        Vector3 pos = new Vector3(
          circleCenter.x + Mathf.Cos(angle) * circleRadius,
          circleCenter.y + Mathf.Sin(angle) * circleRadius,
          circleCenter.z
        );

        lineRenderer.SetPosition(i, pos);
      }
    }

    private void ApplyLaserDamage()
    {
      Collider2D[] enemies = Physics2D.OverlapCircleAll(
        circleCenter,
        circleRadius + laserWidth,
        LayerMask.GetMask("Enemy")
      );

      foreach (Collider2D enemy in enemies)
      {
        Vector2 enemyPos = enemy.transform.position;

        float distanceFromCenter =
          Vector2.Distance(enemyPos, circleCenter);

        float distanceToCircleLine =
          Mathf.Abs(distanceFromCenter - circleRadius);

        // ★ 원 안 전체가 아니라, 원형 선 근처에 있는 적만 피해
        if (distanceToCircleLine > laserWidth)
          continue;

        if (enemy.TryGetComponent<Character>(out Character character))
        {
          character.TakeDamage(damagePerTick, sourceWeapon);
        }
      }
    }
  }
}