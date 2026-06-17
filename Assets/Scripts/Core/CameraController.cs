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

  private Camera controlledCamera;
  private float defaultOrthographicSize;
  private bool hasDefaultOrthographicSize;

  private Object areaLockOwner;
  private bool hasAreaLock;
  private Vector3 areaLockCenter;
  private Vector2 areaLockSize;
  private float areaLockPadding;

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
      return;
    }

    CacheCamera();
  }

  public void SetTarget(Transform target)
  {
    player = target;
  }

  public void LockToArea(Object owner, Vector3 center, Vector2 size, float padding)
  {
    if (owner == null)
      return;

    if (hasAreaLock && areaLockOwner != owner)
      return;

    areaLockOwner = owner;
    hasAreaLock = true;
    areaLockCenter = center;
    areaLockSize = size;
    areaLockPadding = Mathf.Max(0f, padding);
  }

  public void ReleaseAreaLock(Object owner)
  {
    if (!hasAreaLock)
      return;

    if (areaLockOwner != null && areaLockOwner != owner)
      return;

    hasAreaLock = false;
    areaLockOwner = null;

    if (controlledCamera != null && hasDefaultOrthographicSize)
      controlledCamera.orthographicSize = defaultOrthographicSize;
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
    if (!hasAreaLock && player == null)
    {
      // "Player" 태그로 타겟을 찾습니다.
      {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
          player = playerObj.transform;
          Debug.Log("[Camera] 플레이어로 카메라 타겟 설정");
        }
      }
    }

    if (!hasAreaLock && player == null) return;

    Vector3 targetPosition = hasAreaLock
      ? new Vector3(areaLockCenter.x, areaLockCenter.y, -10)
      : new Vector3(player.position.x, player.position.y, -10);

    UpdateOrthographicSize();

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

  private void CacheCamera()
  {
    controlledCamera = GetComponent<Camera>();
    if (controlledCamera == null)
      controlledCamera = Camera.main;

    if (controlledCamera == null)
      return;

    defaultOrthographicSize = controlledCamera.orthographicSize;
    hasDefaultOrthographicSize = true;
  }

  private void UpdateOrthographicSize()
  {
    if (controlledCamera == null)
      CacheCamera();

    if (controlledCamera == null)
      return;

    if (!hasAreaLock)
    {
      if (hasDefaultOrthographicSize)
        controlledCamera.orthographicSize = defaultOrthographicSize;

      return;
    }

    float aspect = Mathf.Max(0.01f, controlledCamera.aspect);
    float sizeByHeight = areaLockSize.y * 0.5f;
    float sizeByWidth = areaLockSize.x / (2f * aspect);
    controlledCamera.orthographicSize = Mathf.Max(sizeByHeight, sizeByWidth) + areaLockPadding;
  }
}
