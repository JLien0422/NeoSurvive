using UnityEngine;
using NeoSurvive.Network.Protocol;

namespace NeoSurvive.Network
{
  /// <summary>
  /// 서버에서 관리하는 아이템(드랍/투사체/체스트)의 클라이언트 대리자입니다.
  /// </summary>
  public class ItemProxy : MonoBehaviour
  {
    [SerializeField] private float syncSpeed = 15f;

    private Vector2 targetPosition;
    private bool isFirstUpdate = true;
    private Rigidbody2D rb;

    private void Awake()
    {
      rb = GetComponent<Rigidbody2D>();
    }

    public void UpdateState(ItemState state)
    {
      if (state == null) return;

      targetPosition = new Vector2(state.PosX, state.PosY);

      if (isFirstUpdate)
      {
        transform.position = new Vector3(targetPosition.x, targetPosition.y, transform.position.z);
        isFirstUpdate = false;
      }

      if (rb != null)
      {
        rb.velocity = new Vector2(state.VelX, state.VelY);
      }
    }

    private void Update()
    {
      if (isFirstUpdate) return;

      Vector3 currentPos = transform.position;
      Vector3 targetPos = new Vector3(targetPosition.x, targetPosition.y, currentPos.z);
      transform.position = Vector3.Lerp(currentPos, targetPos, Time.deltaTime * syncSpeed);
    }
  }
}
