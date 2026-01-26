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

      // HP 정보 등 수신 시 갱신 로직 추가 가능
      // ex) ui.SetHP(state.CurrentHp);
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
