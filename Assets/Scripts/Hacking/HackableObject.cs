using UnityEngine;
using NeoSurvive.Characters;
using System;

public class HackableObject : MonoBehaviour
{
    [Header("설정")]
    [SerializeField] private HackableObjectType objectType;
    [SerializeField] private float hackRange = 3f;

    [Header("사이코 잠식도")]
    [SerializeField] private float psychoIncreaseOnFail = 50f;

    [Header("Hack VFX")]
    [SerializeField] private GameObject idleVfxPrefab;
    [SerializeField] private GameObject failVfxPrefab;
    [SerializeField] private float failVfxLifetime = 1f;
    [SerializeField] private float vfxScale = 1f;

    [Header("Fail VFX Sorting")]
    [SerializeField] private bool hideBaseSpriteOnFail = true;
    [SerializeField] private int failVfxOrderOffset = 10;

    [Header("Cyborg Capture")]
    [SerializeField] private bool canCapture = true;
    [SerializeField] private float captureTime = 5f;
    [SerializeField] private bool resetCaptureWhenOutOfRange = true;
    [SerializeField] private CaptureProgressUI captureUI;

    // =========================
    // ★ 추가: Effect 이벤트
    // =========================
    public event Action OnActivated;

    private GameObject currentIdleVfx;

    private bool canHack = false;
    private bool isHacked = false;
    private bool isCaptured = false;

    private float captureProgress = 0f;

    private Player cachedPlayer;
    private float playerSearchTimer = 0f;
    private const float PlayerSearchInterval = 1f;

    private float checkTimer = 0f;
    private const float CheckInterval = 0.1f;

    private bool IsActivated => isHacked || isCaptured;

    private void Start()
    {
        SpawnIdleVFX();

        if (captureUI == null)
            captureUI = GetComponentInChildren<CaptureProgressUI>(true);

        if (captureUI != null)
            captureUI.ResetProgress();
    }

    private void Update()
    {
        if (IsActivated) return;

        checkTimer += Time.deltaTime;

        if (checkTimer >= CheckInterval)
        {
            checkTimer = 0f;
            CheckForPlayer();
        }

        HandleHackerInput();
        HandleCyborgCapture();
    }

    private void HandleHackerInput()
    {
        if (!canHack || cachedPlayer == null)
            return;

        if (cachedPlayer.CharacterType != CharacterType.Hacker)
            return;

        if (Input.GetKeyDown(KeyCode.E))
            StartHacking();
    }

    private void HandleCyborgCapture()
    {
        if (!canCapture || !canHack || cachedPlayer == null)
        {
            StopCapture();
            return;
        }

        if (cachedPlayer.CharacterType != CharacterType.Cyborg)
        {
            StopCapture();
            return;
        }

        captureProgress +=
            Time.deltaTime / Mathf.Max(0.01f, captureTime);

        captureProgress = Mathf.Clamp01(captureProgress);

        if (captureUI != null)
        {
            captureUI.Show();
            captureUI.SetProgress(captureProgress);
        }

        if (captureProgress >= 1f)
            CompleteCapture();
    }

    private void StopCapture()
    {
        if (captureProgress <= 0f)
            return;

        if (resetCaptureWhenOutOfRange)
            captureProgress = 0f;

        if (captureUI != null)
            captureUI.ResetProgress();
    }

    private void CompleteCapture()
    {
        if (isCaptured)
            return;

        isCaptured = true;

        Debug.Log(
            $"[HackableObject] {objectType} 사이보그 점령 성공! 효과 발동!"
        );

        if (captureUI != null)
            captureUI.ShowCompleted();

        DestroyIdleVFX();
        ActivateEffect();
    }

    private void CheckForPlayer()
    {
        playerSearchTimer += CheckInterval;

        if (cachedPlayer == null ||
            playerSearchTimer >= PlayerSearchInterval)
        {
            playerSearchTimer = 0f;
            cachedPlayer = FindPlayers();
        }

        if (cachedPlayer == null)
        {
            canHack = false;
            return;
        }

        float sqrDist =
            (transform.position -
             cachedPlayer.transform.position).sqrMagnitude;

        canHack =
            sqrDist <= hackRange * hackRange;
    }

    private Player FindPlayers()
    {
        Player[] players =
            FindObjectsOfType<Player>();

        return players.Length > 0
            ? players[0]
            : null;
    }

    private void StartHacking()
    {
        if (cachedPlayer == null ||
            cachedPlayer.CharacterType != CharacterType.Hacker)
            return;

        if (HackingSystem.Instance == null)
        {
            Debug.LogWarning(
                "[HackableObject] HackingSystem 인스턴스가 없습니다!"
            );

            return;
        }

        if (HackingSystem.Instance.IsHacking)
            return;

        GameAnalyticsTracker.TrackHackableInteraction(
            objectType,
            transform.position
        );

        Debug.Log(
            $"[HackableObject] {objectType} 해킹 시작"
        );

        HackingSystem.Instance.StartHacking(
            objectType,
            OnHackingSuccess,
            OnHackingFailed,
            transform.position
        );
    }

    private void OnHackingSuccess()
    {
        isHacked = true;

        GameAnalyticsTracker.TrackHackingResult(
            objectType,
            success: true,
            psychoIncreaseOnFail
        );

        Debug.Log(
            $"[HackableObject] {objectType} 해킹 성공! 효과 발동!"
        );

        if (captureUI != null)
            captureUI.ResetProgress();

        DestroyIdleVFX();
        ActivateEffect();
    }

    private void OnHackingFailed()
    {
        GameAnalyticsTracker.TrackHackingResult(
            objectType,
            success: false,
            psychoIncreaseOnFail
        );

        Debug.Log(
            $"[HackableObject] {objectType} 해킹 실패! 사이코잠식도 +{psychoIncreaseOnFail}%"
        );

        if (captureUI != null)
            captureUI.ResetProgress();

        DestroyIdleVFX();
        SpawnFailVFX();

        Player player =
            cachedPlayer != null
                ? cachedPlayer
                : FindPlayers();

        if (player != null)
            player.AddPsychoCorruption(psychoIncreaseOnFail);
    }

    private void SpawnIdleVFX()
    {
        if (idleVfxPrefab == null)
            return;

        if (currentIdleVfx != null)
            return;

        currentIdleVfx =
            Instantiate(
                idleVfxPrefab,
                transform.position,
                Quaternion.identity,
                transform
            );

        currentIdleVfx.transform.localPosition =
            Vector3.zero;

        currentIdleVfx.transform.localRotation =
            Quaternion.identity;

        currentIdleVfx.transform.localScale =
            Vector3.one * vfxScale;
    }

    private void DestroyIdleVFX()
    {
        if (currentIdleVfx == null)
            return;

        Destroy(currentIdleVfx);
        currentIdleVfx = null;
    }

    private void SpawnFailVFX()
    {
        if (failVfxPrefab == null)
        {
            Debug.LogWarning(
                $"[HackableObject] {objectType} failVfxPrefab이 비어 있음"
            );

            return;
        }

        if (hideBaseSpriteOnFail)
        {
            SpriteRenderer baseSr =
                GetComponent<SpriteRenderer>();

            if (baseSr != null)
                baseSr.enabled = false;
        }

        GameObject vfx =
            Instantiate(
                failVfxPrefab,
                transform.position,
                Quaternion.identity
            );

        vfx.transform.localScale =
            Vector3.one * vfxScale;

        ApplyFailVFXSorting(vfx);
        DisablePhysicsOnVFX(vfx);

        Debug.Log(
            $"[HackableObject] {objectType} Fail VFX 생성: {vfx.name}"
        );

        Destroy(vfx, failVfxLifetime);
    }

    private void ApplyFailVFXSorting(GameObject vfx)
    {
        if (vfx == null)
            return;

        SpriteRenderer baseSr =
            GetComponent<SpriteRenderer>();

        SpriteRenderer[] renderers =
            vfx.GetComponentsInChildren<SpriteRenderer>(true);

        foreach (SpriteRenderer sr in renderers)
        {
            if (sr == null)
                continue;

            if (baseSr != null)
            {
                sr.sortingLayerID =
                    baseSr.sortingLayerID;

                sr.sortingOrder =
                    baseSr.sortingOrder +
                    failVfxOrderOffset;
            }
            else
            {
                sr.sortingOrder += failVfxOrderOffset;
            }
        }
    }

    private void DisablePhysicsOnVFX(GameObject vfx)
    {
        if (vfx == null)
            return;

        Collider2D[] colliders =
            vfx.GetComponentsInChildren<Collider2D>(true);

        foreach (Collider2D col in colliders)
        {
            if (col != null)
                col.enabled = false;
        }

        Rigidbody2D[] rigidbodies =
            vfx.GetComponentsInChildren<Rigidbody2D>(true);

        foreach (Rigidbody2D rb in rigidbodies)
        {
            if (rb != null)
                rb.simulated = false;
        }
    }

    private void ActivateEffect()
    {
        // =========================
        // ★ 이벤트 호출
        // =========================
        OnActivated?.Invoke();

        switch (objectType)
        {
            case HackableObjectType.SecurityTurret:
            {
                Debug.Log("[Effect] 보안 터렛 활성화!");

                var turret =
                    GetComponent<SecurityTurretEffect>()
                    ?? gameObject.AddComponent<SecurityTurretEffect>();

                turret.Activate();
                break;
            }

            case HackableObjectType.ElectricFence:
            {
                Debug.Log("[Effect] 전기 울타리 생성!");

                var fence =
                    GetComponent<ElectricFenceEffect>()
                    ?? gameObject.AddComponent<ElectricFenceEffect>();

                fence.Activate();
                break;
            }

            case HackableObjectType.SatelliteUplink:
            {
                Debug.Log("[Effect] 새틀라이트 레이저 발동!");

                var satellite =
                    GetComponent<SatelliteUplinkEffect>()
                    ?? gameObject.AddComponent<SatelliteUplinkEffect>();

                satellite.Activate();
                break;
            }

            // =========================
            // ★ Synapse / Beacon 직접 호출 제거
            // 이벤트 기반으로 동작
            // =========================

            default:
                break;
        }

        HideVisuals();
    }

    private void HideVisuals()
    {
        SpriteRenderer sr =
            GetComponent<SpriteRenderer>();

        if (sr != null)
            sr.enabled = false;

        foreach (Collider2D col in GetComponents<Collider2D>())
            col.enabled = false;

        this.enabled = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        Gizmos.DrawWireSphere(
            transform.position,
            hackRange
        );
    }
}