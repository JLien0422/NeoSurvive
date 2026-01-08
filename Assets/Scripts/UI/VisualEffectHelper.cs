using UnityEngine;
using System.Collections;

namespace NeoSurvive.UI
{
  public class VisualEffectHelper : MonoBehaviour
  {
    public static void CreateCircleEffect(Vector3 position, float radius, Color color, float duration = 0.5f)
    {
      GameObject obj = new("CircleEffect");
      obj.transform.position = position;
      LineRenderer lr = obj.AddComponent<LineRenderer>();
      lr.startWidth = 0.1f;
      lr.endWidth = 0.1f;
      lr.positionCount = 51;
      lr.useWorldSpace = false;
      lr.material = new Material(Shader.Find("Sprites/Default"));
      lr.startColor = color;
      lr.endColor = new Color(color.r, color.g, color.b, 0);

      for (int i = 0; i <= 50; i++)
      {
        float angle = i * Mathf.PI * 2f / 50f;
        lr.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0));
      }

      EffectAutoDestroy ad = obj.AddComponent<EffectAutoDestroy>();
      ad.duration = duration;
    }

    public static void CreateSlashEffect(Vector3 position, Vector3 direction, float range, float angle, Color color, float duration = 0.2f)
    {
      GameObject obj = new("SlashEffect");
      obj.transform.position = position;
      LineRenderer lr = obj.AddComponent<LineRenderer>();
      lr.startWidth = 0.2f;
      lr.endWidth = 0.05f;
      lr.positionCount = 11;
      lr.useWorldSpace = false;
      lr.material = new Material(Shader.Find("Sprites/Default"));
      lr.startColor = color;
      lr.endColor = new Color(color.r, color.g, color.b, 0);

      float startAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - angle / 2f;
      for (int i = 0; i <= 10; i++)
      {
        float currentAngle = (startAngle + (i * angle / 10f)) * Mathf.Deg2Rad;
        lr.SetPosition(i, new Vector3(Mathf.Cos(currentAngle) * range, Mathf.Sin(currentAngle) * range, 0));
      }

      EffectAutoDestroy ad = obj.AddComponent<EffectAutoDestroy>();
      ad.duration = duration;
    }
  }
}
