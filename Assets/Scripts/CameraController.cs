using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
  public Transform player;

  public void SetTarget(Transform target)
  {
    player = target;
  }

  void LateUpdate()
  {
    if (player == null)
    {
      // 멀티플레이인 경우: UDPClient의 LocalPlayer를 추적
      if (UDPClient.Instance != null && UDPClient.Instance.LocalPlayer != null)
      {
        player = UDPClient.Instance.LocalPlayer.transform;
        Debug.Log("[Camera] 멀티플레이 로컬 플레이어로 카메라 타겟 설정");
      }
      // 싱글플레이인 경우: "Player" 태그로 찾기
      else
      {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
          player = playerObj.transform;
          Debug.Log("[Camera] 싱글플레이 플레이어로 카메라 타겟 설정");
        }
      }
    }

    if (player == null) return;
    transform.position = new Vector3(player.position.x, player.position.y, -10);
  }
}
