using UnityEngine;
using NeoSurvive.Core;

namespace NeoSurvive.Weapon
{
  /// <summary>
  /// 에너지 방패 오브 1개
  /// - Trigger로 적/투사체를 막고(삭제), 적에 접촉딜
  /// </summary>
  [RequireComponent(typeof(Collider2D))]
  public class ShieldOrb : MonoBehaviour
  {
    private float damage;
    private LayerMask enemyMask;
    private string enemyTag;
    private bool blockProjectiles;
    private string projectileTag;

    private void Awake()
    {
      var col = GetComponent<Collider2D>();
      col.isTrigger = true;
    }

    public void Configure(
      float damage,
      LayerMask enemyMask,
      string enemyTag,
      bool blockProjectiles,
      string projectileTag)
    {
      this.damage = damage;
      this.enemyMask = enemyMask;
      this.enemyTag = enemyTag;
      this.blockProjectiles = blockProjectiles;
      this.projectileTag = projectileTag;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
      if (other == null) return;

      // 1) 투사체 차단
      if (blockProjectiles && !string.IsNullOrEmpty(projectileTag) && other.CompareTag(projectileTag))
      {
        Destroy(other.gameObject);
        return;
      }

      // 2) 적 접촉딜
      // 레이어 마스크 체크(빨리 거름)
      if (((1 << other.gameObject.layer) & enemyMask.value) == 0) return;

      // 태그 체크(자식 콜라이더 구조도 고려: parent도 허용)
      if (!string.IsNullOrEmpty(enemyTag) && !other.CompareTag(enemyTag))
      {
        if (other.transform.parent == null || !other.transform.parent.CompareTag(enemyTag))
          return;
      }

      // ✅ 핵심: Enemy는 부모에 붙어 있을 수 있음
      Enemy enemy = other.GetComponentInParent<Enemy>();
      if (enemy != null)
      {
        enemy.TakeDamage(damage);
      }
    }
  }
}
