using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 카메라 컨트롤러
/// 플레이어를 추적하고, 화면 흔들림 효과를 제공합니다.
/// </summary>
public class CameraController : MonoBehaviour
{
  public static CameraController Instance { get; private set; }

  public Transform player;

  [Header("화면 흔들림 설정")]
  [SerializeField] private float shakeDuration = 0.3f;
  [SerializeField] private float shakeMagnitude = 0.2f;

  private Vector3 originalPosition;
  private float shakeTimer = 0f;
  private bool isShaking = false;

  private void Awake()
  {
    if (Instance == null)
    {
      Instance = this;
    }
    else
    {
      Destroy(gameObject);
    }
  }

  public void SetTarget(Transform target)
  {
    player = target;
  }

  /// <summary>
  /// 화면 흔들림 효과 시작
  /// </summary>
  public void Shake(float duration = -1f, float magnitude = -1f)
  {
    // SettingsManager에서 화면 흔들림 옵션 확인
    if (SettingsManager.Instance != null && !SettingsManager.Instance.screenShakeEnabled)
    {
      return; // 옵션이 꺼져 있으면 흔들림 없음
    }

    if (duration < 0) duration = shakeDuration;
    if (magnitude < 0) magnitude = shakeMagnitude;

    shakeTimer = duration;
    shakeMagnitude = magnitude;
    isShaking = true;
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

    // 기본 카메라 위치 (플레이어 추적)
    Vector3 targetPosition = new Vector3(player.position.x, player.position.y, -10);

    // 화면 흔들림 효과 적용
    if (isShaking)
    {
      shakeTimer -= Time.deltaTime;

      if (shakeTimer > 0)
      {
        // 랜덤 오프셋 추가
        Vector3 shakeOffset = Random.insideUnitCircle * shakeMagnitude;
        transform.position = targetPosition + shakeOffset;
      }
      else
      {
        // 흔들림 종료
        isShaking = false;
        transform.position = targetPosition;
      }
    }
    else
    {
      transform.position = targetPosition;
    }
  }
}
