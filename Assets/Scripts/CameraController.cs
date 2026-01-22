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
    if (player == null) return;
    transform.position = new Vector3(player.position.x, player.position.y, -10);
  }
}
