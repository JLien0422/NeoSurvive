using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
  public Transform player;
  void Start()
  {
    player = GameObject.FindWithTag("Player").transform;
  }

  void LateUpdate()
  {
    transform.position = new Vector3(player.position.x, player.position.y, -10);
  }
}
