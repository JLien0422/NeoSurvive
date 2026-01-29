using UnityEngine;
using NeoSurvive.Core;

namespace NeoSurvive.Weapon
{
  /// <summary>
  /// 적에게 붙는 과부하 스택
  /// - 스택이 stacksToExplode에 도달하면 폭발(주변 적 광역 피해)
  /// </summary>
  public class OverloadStack : MonoBehaviour
  {
    public int stacks = 0;

    // (선택) 스택 유지시간을 넣고 싶으면 여기서 타이머로 관리 가능

    public void AddStack(
      int amount,
      int stacksToExplode,
      float explosionRadius,
      float explosionDamage,
      LayerMask enemyMask,
      string enemyTag)
    {
      stacks += amount;

      if (stacks >= stacksToExplode)
      {
        Explode(explosionRadius, explosionDamage, enemyMask, enemyTag);
        stacks = 0;
      }
    }

    private void Explode(float radius, float damage, LayerMask enemyMask, string enemyTag)
    {
      Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius, enemyMask);
      foreach (var col in hits)
      {
        if (col == null) continue;

        if (!string.IsNullOrEmpty(enemyTag) && !col.CompareTag(enemyTag))
        {
          if (col.transform.parent == null || !col.transform.parent.CompareTag(enemyTag))
            continue;
        }

        Enemy e = col.GetComponentInParent<Enemy>();
        if (e != null)
        {
          e.TakeDamage(damage);
        }
      }

      // 시각효과가 있다면 여기서(파티클/사운드)
      // Debug.Log($"[Overload] Explode! radius={radius} damage={damage}");

      // 스택 컴포넌트 제거(원하면 유지)
      Destroy(this);
    }

    private void OnDrawGizmosSelected()
    {
      // 디버그용: 폭발 범위는 호출 시 값이 들어오니 여기선 생략
    }
  }
}
