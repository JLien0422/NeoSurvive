using UnityEngine;

namespace NeoSurvive.Weapon
{
  /// <summary>
  /// LinkPistol Lv5 마스터 탄환 전용 적중 처리
  /// - 적중 시 Enemy에게 표식 부여
  /// - Projectile.cs는 공용이므로 수정하지 않음
  /// </summary>
  public class LinkPistolMarkOnHit : MonoBehaviour
  {
    [Header("표식 유지 시간")]
    [SerializeField] private float markDuration = 5f;
    [SerializeField] private Sprite markIcon;
    private WeaponBase sourceWeapon;

    /// <summary>
    /// LinkPistol에서 초기화
    /// </summary>
    public void Init(float duration, Sprite icon, WeaponBase weapon = null)
    {
      markDuration = duration;
      markIcon = icon;
      sourceWeapon = weapon;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
      // Enemy / Boss 모두 Enemy 태그 사용
      Character target = RunTraitTarget.Find(collision);
      if (target == null || target.IsDead) return;
      MarkedTarget mark = target.GetComponent<MarkedTarget>();

      // 표식이 없으면 새로 추가
      if (mark == null)
      {
        mark = target.gameObject.AddComponent<MarkedTarget>();
      }

      // 이미 있으면 유지시간 갱신
      mark.Refresh(markDuration, markIcon, sourceWeapon);

      Debug.Log($"[LinkPistol] 표식 부여: {collision.name}");
    }
  }
}
