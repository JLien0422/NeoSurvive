using UnityEngine;

namespace NeoSurvive.Weapon
{
  /// <summary>
  /// LinkPistol Lv5 마스터 표식
  ///
  /// [역할]
  /// - LinkPistol 마스터 탄환에 맞은 Enemy에게 부여됨
  /// - 동시에 하나의 Enemy에게만 유지됨
  /// - 새 Enemy가 표식되면 기존 표식은 제거됨
  /// - AI 드론 / 자동포탑 등이 집중 공격 대상으로 사용 가능
  ///
  /// [동작 흐름]
  /// Enemy A 피격
  /// → A에게 MarkedTarget 추가
  ///
  /// Enemy B 피격
  /// → A 표식 제거
  /// → B 표식 생성
  ///
  /// 일정 시간 경과
  /// → 표식 자동 제거
  /// </summary>
  public class MarkedTarget : MonoBehaviour
  {
    private const float HackerWeaponDamageMultiplier = 1.2f;

    // =========================
    // ★ 현재 활성화된 표식 대상
    // - 동시에 하나만 유지
    // =========================
    public static MarkedTarget Current { get; private set; }

    [Header("표식 유지 시간")]
    [SerializeField] private float duration = 5f;
    [SerializeField] private Vector3 iconLocalOffset = new Vector3(0f, 1.2f, 0f);
    [SerializeField] private Vector3 iconScale = new Vector3(0.45f, 0.45f, 1f);
    [SerializeField] private int iconSortingOrder = 50;

    private float timer;
    private GameObject iconObject;
    private SpriteRenderer iconRenderer;

    private void OnEnable()
    {
      // =========================
      // ★ 기존 표식 제거
      // - 새 대상이 표식되면 이전 표식 제거
      // =========================
      if (Current != null && Current != this)
      {
        Destroy(Current);
      }

      // ★ 현재 표식 등록
      Current = this;

      // ★ 유지시간 시작
      timer = duration;
    }

    private void Update()
    {
      timer -= Time.deltaTime;

      // =========================
      // ★ 유지시간 종료 시 표식 제거
      // =========================
      if (timer <= 0f)
      {
        Destroy(this);
      }
    }

    private void OnDestroy()
    {
      // =========================
      // ★ 현재 표식 초기화
      // =========================
      if (Current == this)
      {
        Current = null;
      }

      if (iconObject != null)
      {
        Destroy(iconObject);
        iconObject = null;
        iconRenderer = null;
      }
    }

    /// <summary>
    /// 표식 유지시간 갱신
    /// - 이미 표식된 Enemy를 다시 맞췄을 때 사용
    /// </summary>
    public void Refresh(float newDuration, Sprite markIcon)
    {
      // =========================
      // ★ 다른 대상에게 표식이 있다면 제거
      // =========================
      if (Current != null && Current != this)
      {
        Destroy(Current);
      }

      // ★ 현재 표식 갱신
      Current = this;

      duration = newDuration;
      timer = duration;

      UpdateIcon(markIcon);
    }

    private void UpdateIcon(Sprite markIcon)
    {
      if (markIcon == null)
        return;

      if (iconObject == null)
      {
        iconObject = new GameObject("PistolMasterMarkIcon");
        iconObject.transform.SetParent(transform, false);
        iconObject.transform.localPosition = iconLocalOffset;
        iconObject.transform.localScale = iconScale;

        iconRenderer = iconObject.AddComponent<SpriteRenderer>();
        iconRenderer.sortingOrder = iconSortingOrder;
      }

      iconRenderer.sprite = markIcon;
    }

    /// <summary>
    /// 현재 집중 공격 대상 반환
    /// - AI 드론 / 자동포탑에서 사용 가능
    /// </summary>
    public static Transform GetCurrentTarget()
    {
      if (Current == null)
        return null;

      return Current.transform;
    }

    /// <summary>
    /// LinkPistol 마스터 표식 대상은 피스톨을 제외한 보유 Active Weapon 피해를 20% 더 받습니다.
    /// </summary>
    public float ApplyHackerWeaponDamageBonus(float amount, WeaponBase sourceWeapon)
    {
      if (!WeaponManager.IsMarkedDamageBonusSource(sourceWeapon))
        return amount;

      return amount * HackerWeaponDamageMultiplier;
    }
  }
}