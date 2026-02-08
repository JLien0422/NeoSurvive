using UnityEngine;
using NeoSurvive.Network.Protocol;

namespace NeoSurvive.Network
{
  /// <summary>
  /// 서버에서 관리하는 적의 클라이언트 대리자입니다.
  /// AI 대신 서버에서 받은 좌표를 보정(Lerp)하여 표시합니다.
  /// </summary>
  public class EnemyProxy : MonoBehaviour
  {
    public uint EnemyId { get; private set; }

    [SerializeField] private float syncSpeed = 15f;
    private Vector2 targetPosition;
    private bool isFirstUpdate = true;

    public void Initialize(uint id)
    {
      this.EnemyId = id;

      // 기존 AI 관련 컴포넌트가 있다면 비활성화 (멀티 전용)
      // ex) var controller = GetComponent<EnemyController>();
      // if (controller) controller.enabled = false;
    }

    public void UpdateState(NeoSurvive.Network.Protocol.EnemyState state)
    {
      targetPosition = new Vector2(state.PosX, state.PosY);

      if (isFirstUpdate)
      {
        transform.position = new Vector3(targetPosition.x, targetPosition.y, transform.position.z);
        isFirstUpdate = false;
      }

      // HP 정보 갱신
      Enemy enemy = GetComponent<Enemy>();
      if (enemy != null)
      {
        if (state.IsDead || state.CurrentHp == 0)
        {
          enemy.SetHealth(0);
          DisableVisuals();
        }
        else
        {
          EnableVisuals();
          enemy.SetHealth(state.CurrentHp);
        }
      }
    }

    /// <summary>
    /// 서버 이벤트 기반 피격 처리
    /// </summary>
    public void ApplyDamage(float damage)
    {
      if (damage <= 0) return;
      Enemy enemy = GetComponent<Enemy>();
      if (enemy != null)
      {
        enemy.TakeDamage(damage);
      }
    }

    /// <summary>
    /// 서버 이벤트 기반 즉시 사망 처리
    /// </summary>
    public void ForceDead()
    {
      Enemy enemy = GetComponent<Enemy>();
      if (enemy != null)
      {
        enemy.SetHealth(0);
        DisableVisuals();
      }
    }

    private void DisableVisuals()
    {
      var col = GetComponent<Collider2D>();
      if (col != null) col.enabled = false;

      var rb = GetComponent<Rigidbody2D>();
      if (rb != null) rb.velocity = Vector2.zero;

      var renderer = GetComponentInChildren<SpriteRenderer>();
      if (renderer != null) renderer.enabled = false;
    }

    private void EnableVisuals()
    {
      var col = GetComponent<Collider2D>();
      if (col != null) col.enabled = true;

      var renderer = GetComponentInChildren<SpriteRenderer>();
      if (renderer != null) renderer.enabled = true;
    }

    private void Update()
    {
      if (isFirstUpdate) return;

      // 부드러운 위치 보간
      Vector3 targetV3 = new Vector3(targetPosition.x, targetPosition.y, transform.position.z);
      transform.position = Vector3.Lerp(transform.position, targetV3, Time.deltaTime * syncSpeed);
    }
  }
}
