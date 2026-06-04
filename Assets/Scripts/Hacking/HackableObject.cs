using UnityEngine;

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

    private GameObject currentIdleVfx;

    private bool canHack = false;
    private bool isHacked = false;

    private Player cachedPlayer;
    private float playerSearchTimer = 0f;
    private const float PlayerSearchInterval = 1f;

    private float checkTimer = 0f;
    private const float CheckInterval = 0.1f;

    private void Start()
    {
        SpawnIdleVFX();
    }

    private void Update()
    {
        if (isHacked) return;

        if (canHack && Input.GetKeyDown(KeyCode.E))
        {
            StartHacking();
            return;
        }

        checkTimer += Time.deltaTime;

        if (checkTimer < CheckInterval) return;

        checkTimer = 0f;

        CheckForPlayer();
    }

    private void CheckForPlayer()
    {
        playerSearchTimer += CheckInterval;

        if (cachedPlayer == null || playerSearchTimer >= PlayerSearchInterval)
        {
            playerSearchTimer = 0f;
            cachedPlayer = FindPlayers();
        }

        if (cachedPlayer == null)
        {
            canHack = false;
            return;
        }

        float sqrDist = (transform.position - cachedPlayer.transform.position).sqrMagnitude;
        canHack = sqrDist <= hackRange * hackRange;
    }

    private Player FindPlayers()
    {
        Player[] players = FindObjectsOfType<Player>();
        return players.Length > 0 ? players[0] : null;
    }

    private void StartHacking()
    {
        if (HackingSystem.Instance == null)
        {
            Debug.LogWarning("[HackableObject] HackingSystem 인스턴스가 없습니다!");
            return;
        }

        if (HackingSystem.Instance.IsHacking)
            return;

        GameAnalyticsTracker.TrackHackableInteraction(objectType, transform.position);

        Debug.Log($"[HackableObject] {objectType} 해킹 시작");

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

        GameAnalyticsTracker.TrackHackingResult(objectType, success: true, psychoIncreaseOnFail);
        Debug.Log($"[HackableObject] {objectType} 해킹 성공! 효과 발동!");

        DestroyIdleVFX();
        ActivateEffect();
    }

    private void OnHackingFailed()
    {
        GameAnalyticsTracker.TrackHackingResult(objectType, success: false, psychoIncreaseOnFail);
        Debug.Log($"[HackableObject] {objectType} 해킹 실패! 사이코잠식도 +{psychoIncreaseOnFail}%");

        DestroyIdleVFX();
        SpawnFailVFX();

        Player player = cachedPlayer != null ? cachedPlayer : FindPlayers();

        if (player != null)
            player.AddPsychoCorruption(psychoIncreaseOnFail);
    }

    private void SpawnIdleVFX()
    {
        if (idleVfxPrefab == null)
            return;

        if (currentIdleVfx != null)
            return;

        currentIdleVfx = Instantiate(
            idleVfxPrefab,
            transform.position,
            Quaternion.identity,
            transform
        );

        currentIdleVfx.transform.localPosition = Vector3.zero;
        currentIdleVfx.transform.localRotation = Quaternion.identity;
        currentIdleVfx.transform.localScale = Vector3.one * vfxScale;
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
            Debug.LogWarning($"[HackableObject] {objectType} failVfxPrefab이 비어 있음");
            return;
        }

        if (hideBaseSpriteOnFail)
        {
            SpriteRenderer baseSr = GetComponent<SpriteRenderer>();

            if (baseSr != null)
                baseSr.enabled = false;
        }

        GameObject vfx = Instantiate(
            failVfxPrefab,
            transform.position,
            Quaternion.identity
        );

        vfx.transform.localScale = Vector3.one * vfxScale;

        ApplyFailVFXSorting(vfx);
        DisablePhysicsOnVFX(vfx);

        Debug.Log($"[HackableObject] {objectType} Fail VFX 생성: {vfx.name}");

        Destroy(vfx, failVfxLifetime);
    }

    private void ApplyFailVFXSorting(GameObject vfx)
    {
        if (vfx == null)
            return;

        SpriteRenderer baseSr = GetComponent<SpriteRenderer>();
        SpriteRenderer[] renderers = vfx.GetComponentsInChildren<SpriteRenderer>(true);

        foreach (SpriteRenderer sr in renderers)
        {
            if (sr == null)
                continue;

            if (baseSr != null)
            {
                sr.sortingLayerID = baseSr.sortingLayerID;
                sr.sortingOrder = baseSr.sortingOrder + failVfxOrderOffset;
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

        Collider2D[] colliders = vfx.GetComponentsInChildren<Collider2D>(true);

        foreach (Collider2D col in colliders)
        {
            if (col != null)
                col.enabled = false;
        }

        Rigidbody2D[] rigidbodies = vfx.GetComponentsInChildren<Rigidbody2D>(true);

        foreach (Rigidbody2D rb in rigidbodies)
        {
            if (rb != null)
                rb.simulated = false;
        }
    }

    private void ActivateEffect()
    {
        switch (objectType)
        {
            case HackableObjectType.SecurityTurret:
            {
                Debug.Log("[Effect] 보안 터렛 활성화!");
                var turret = GetComponent<SecurityTurretEffect>()
                             ?? gameObject.AddComponent<SecurityTurretEffect>();
                turret.Activate();
                break;
            }

            case HackableObjectType.ElectricFence:
            {
                Debug.Log("[Effect] 전기 울타리 생성!");
                var fence = GetComponent<ElectricFenceEffect>()
                            ?? gameObject.AddComponent<ElectricFenceEffect>();
                fence.Activate();
                break;
            }

            case HackableObjectType.SatelliteUplink:
            {
                Debug.Log("[Effect] 새틀라이트 레이저 발동!");
                var satellite = GetComponent<SatelliteUplinkEffect>()
                                ?? gameObject.AddComponent<SatelliteUplinkEffect>();
                satellite.Activate();
                break;
            }

            case HackableObjectType.SynapseServer:
            {
                Debug.Log("[Effect] 적 절반 아군화!");
                var synapse = GetComponent<SynapseServerEffect>()
                              ?? gameObject.AddComponent<SynapseServerEffect>();
                synapse.Activate();
                break;
            }

            case HackableObjectType.MagneticBeacon:
            {
                Debug.Log("[Effect] 마그네틱 비컨 발동 - 적 견인!");
                var beacon = GetComponent<MagneticBeaconEffect>()
                             ?? gameObject.AddComponent<MagneticBeaconEffect>();
                beacon.Activate();
                break;
            }

            default:
                return;
        }

        HideVisuals();
    }

    private void HideVisuals()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();

        if (sr != null)
            sr.enabled = false;

        foreach (Collider2D col in GetComponents<Collider2D>())
            col.enabled = false;

        this.enabled = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, hackRange);
    }
}