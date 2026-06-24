using System.Collections.Generic;
using UnityEngine;
using NeoSurvive.Buff;
using NeoSurvive.Core;
using NeoSurvive.Balancing.Map;
using NeoSurvive.Characters;

namespace NeoSurvive.Map.Map1.Gimmicks
{
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class SteamVentZone : MonoBehaviour, IHackPromptProvider
    {
        [Header("CSV")]
        [SerializeField] private string mapId = "Map1";
        [SerializeField] private string gimmickId = "steam_vent";

        [Header("Zone")]
        [SerializeField] private Collider2D zoneTrigger;

        [Header("Pulse")]
        [SerializeField] private bool startActive = true;
        [SerializeField] private float pulseInterval = 0.25f;

        [Header("Normal Effect")]
        [SerializeField] private float slowMultiplier = 0.5f;
        [SerializeField] private float slowDuration = 0.35f;

        [Header("Hack")]
        [SerializeField] private KeyCode hackKey = KeyCode.E;
        [SerializeField] private HackableObject hackableObject;

        [Header("After Hack")]
        [SerializeField] private float stunDuration = 1.25f;
        [SerializeField] private string enemyTag = "Enemy";

        [Header("Target Filter")]
        [SerializeField] private LayerMask targetMask;

        [Header("Visual")]
        [SerializeField] private SpriteRenderer zoneRenderer;
        [SerializeField] private Color normalColor = new Color(0.8f, 0.8f, 0.8f, 0.5f);
        [SerializeField] private Color hackedColor = new Color(0.5f, 0.9f, 1f, 0.6f);

        [Header("Auto Fit")]
        [SerializeField] private bool autoFitCollider = true;
        [SerializeField] private Vector2 colliderPadding = Vector2.zero;

        [Header("Debug")]
        [SerializeField] private bool debugLog = true;

        private readonly HashSet<Collider2D> insideTargets = new HashSet<Collider2D>();

        private float pulseTimer = 0f;

        private bool isHacked = false;
        private bool canHack = true;
        private bool isHackRequestPending = false;

        private Player currentPlayerInZone;

        private uint slowBuffId;

        private void Awake()
        {
            if (zoneTrigger == null)
                zoneTrigger = GetComponent<Collider2D>();

            if (zoneRenderer == null)
                zoneRenderer = GetComponent<SpriteRenderer>();

            if (hackableObject == null)
                hackableObject = GetComponent<HackableObject>();

            if (zoneTrigger != null)
                zoneTrigger.isTrigger = true;

            if (autoFitCollider)
                FitColliderToSprite();

            ApplyVisualState();

            slowBuffId = (uint)(100000 + Mathf.Abs(GetInstanceID()));
            EnsurePromptAnchor();
        }

        public Transform PromptAnchorRoot => transform;
        public float HackInteractRange => HackPromptBoundsUtility.GetInteractRadius(transform, 2f);
        public bool IsHackInteractionComplete => isHacked;
        public bool IsHackInteractionEnabled => canHack && !isHackRequestPending;
        public bool ShouldShowHackPrompt =>
            canHack
            && !isHacked
            && !isHackRequestPending
            && currentPlayerInZone != null;

        private void EnsurePromptAnchor()
        {
            if (GetComponent<HackInteractPromptAnchor>() == null)
                gameObject.AddComponent<HackInteractPromptAnchor>();
        }

        private void Start()
        {
            LoadFromCSV();

            if (debugLog)
            {
                Debug.Log(
                    $"[SteamVentZone] Start Check | " +
                    $"canHack={canHack}, " +
                    $"zoneTrigger={(zoneTrigger != null ? zoneTrigger.name : "NULL")}, " +
                    $"hackableObject={(hackableObject != null ? hackableObject.name : "NULL")}, " +
                    $"HackingSystem={(HackingSystem.Instance != null ? "OK" : "NULL")}"
                );
            }
        }

        private void Update()
        {
            HandleHackStartInput();
            HandlePulse();
        }

        private void LoadFromCSV()
        {
            if (MapEnvironmentGimmickLoader.DB == null)
            {
                Debug.LogWarning("[SteamVentZone] MapEnvironmentGimmickLoader.DB == null");
                return;
            }

            var row = MapEnvironmentGimmickLoader.DB.Get(mapId, gimmickId);

            if (row == null)
            {
                Debug.LogWarning($"[SteamVentZone] CSV row 없음 | mapId={mapId}, gimmickId={gimmickId}");
                return;
            }

            startActive = row.isActive;
            canHack = row.canHack;
            pulseInterval = row.pulseInterval;

            if (row.effectType == "slow")
            {
                slowMultiplier = row.effectValue;
                slowDuration = row.effectDuration;
            }

            if (row.hackedEffectType == "stun")
            {
                stunDuration = row.hackedEffectDuration;
            }

            if (debugLog)
            {
                Debug.Log(
                    $"[SteamVentZone] CSV 로드 완료 | " +
                    $"startActive={startActive}, canHack={canHack}, pulseInterval={pulseInterval}, " +
                    $"slowMultiplier={slowMultiplier}, slowDuration={slowDuration}, stunDuration={stunDuration}"
                );
            }
        }

        private void HandleHackStartInput()
        {
            if (!Input.GetKeyDown(hackKey))
                return;

            Debug.Log("[SteamVentZone] E 입력 감지");

            if (!canHack)
            {
                Debug.Log("[SteamVentZone] 해킹 불가: canHack=false");
                return;
            }

            if (isHacked)
            {
                Debug.Log("[SteamVentZone] 해킹 불가: 이미 해킹 완료");
                return;
            }

            if (isHackRequestPending)
            {
                Debug.Log("[SteamVentZone] 해킹 불가: 이미 해킹 요청 진행 중");
                return;
            }

            if (currentPlayerInZone == null)
            {
                Debug.Log("[SteamVentZone] 해킹 불가: currentPlayerInZone == null / 플레이어가 Trigger 안에 잡히지 않음");
                return;
            }

            Debug.Log(
                $"[SteamVentZone] Player 감지 | " +
                $"name={currentPlayerInZone.name}, " +
                $"type={currentPlayerInZone.CharacterType}, " +
                $"layer={LayerMask.LayerToName(currentPlayerInZone.gameObject.layer)}, " +
                $"tag={currentPlayerInZone.tag}"
            );

            if (hackableObject == null)
            {
                Debug.Log("[SteamVentZone] 해킹 불가: hackableObject == null");
                return;
            }

            if (HackingSystem.Instance == null)
            {
                Debug.Log("[SteamVentZone] 해킹 불가: HackingSystem.Instance == null");
                return;
            }

            if (HackingSystem.Instance.IsHacking)
            {
                Debug.Log("[SteamVentZone] 해킹 불가: 이미 다른 해킹 진행 중");
                return;
            }

            isHackRequestPending = true;

            HackableObjectType randomType = GetRandomHackableObjectType();

            Debug.Log($"[SteamVentZone] 해킹 시작 | 랜덤 미니게임: {randomType}");

            HackingSystem.Instance.StartHacking(
                randomType,
                OnHackSucceededFromSystem,
                OnHackFailedFromSystem,
                transform.position
            );
        }

        private HackableObjectType GetRandomHackableObjectType()
        {
            HackableObjectType[] types =
            {
                HackableObjectType.SecurityTurret,
                HackableObjectType.ElectricFence,
                HackableObjectType.SatelliteUplink,
                HackableObjectType.SynapseServer,
                HackableObjectType.MagneticBeacon
            };

            return types[Random.Range(0, types.Length)];
        }

        public void OnHackEndedFromSystem()
        {
            isHackRequestPending = false;
        }

        private void OnHackFailedFromSystem()
        {
            isHackRequestPending = false;

            if (currentPlayerInZone != null)
            {
                currentPlayerInZone.AddPsychoCorruption(50f);
                Debug.Log("[SteamVentZone] 해킹 실패 -> 사이코잠식도 +50%");
            }
        }

        public void OnHackSucceededFromSystem()
        {
            if (isHacked) return;

            isHacked = true;
            isHackRequestPending = false;

            ApplyVisualState();
            ClearSlowFromAllInsideTargets();

            Debug.Log("[SteamVentZone] Hack Success");
        }

        private void HandlePulse()
        {
            if (!startActive) return;

            pulseTimer += Time.deltaTime;

            if (pulseTimer < pulseInterval) return;

            pulseTimer = 0f;
            ExecutePulse();
        }

        private void ExecutePulse()
        {
            var snapshot = new List<Collider2D>(insideTargets);

            foreach (var hitCol in snapshot)
            {
                if (hitCol == null) continue;

                GameObject hitObject = hitCol.gameObject;

                Player player = hitObject.GetComponentInParent<Player>();

                if (player != null)
                {
                    if (!IsInTargetMask(player.gameObject.layer))
                        continue;

                    if (!isHacked)
                    {
                        ApplyOrRefreshSlow(player.gameObject);

                        if (debugLog)
                            Debug.Log($"[SteamVentZone] Player Slow Refresh | {player.name}");
                    }

                    continue;
                }

                Transform root = hitObject.transform.root;

                if (root == null) continue;
                if (!root.CompareTag(enemyTag)) continue;
                if (!IsInTargetMask(root.gameObject.layer)) continue;

                BuffHandler enemyBuff = root.GetComponent<BuffHandler>();

                if (enemyBuff == null)
                    enemyBuff = root.gameObject.AddComponent<BuffHandler>();

                if (isHacked)
                {
                    enemyBuff.AddBuff(new StunDebuff(stunDuration));

                    if (debugLog)
                        Debug.Log($"[SteamVentZone] Enemy Stun | {root.name}");
                }
                else
                {
                    ApplyOrRefreshSlow(root.gameObject);

                    if (debugLog)
                        Debug.Log($"[SteamVentZone] Enemy Slow Refresh | {root.name}");
                }
            }
        }

        private void ApplyOrRefreshSlow(GameObject targetRoot)
        {
            if (targetRoot == null) return;

            BuffHandler buffHandler = targetRoot.GetComponent<BuffHandler>();

            if (buffHandler == null)
                buffHandler = targetRoot.AddComponent<BuffHandler>();

            buffHandler.AddBuffWithId(
                slowBuffId,
                new SlowDebuff(slowMultiplier, slowDuration),
                1,
                slowDuration,
                true
            );
        }

        private void RemoveSlow(GameObject targetRoot)
        {
            if (targetRoot == null) return;

            BuffHandler buffHandler = targetRoot.GetComponent<BuffHandler>();

            if (buffHandler == null) return;

            buffHandler.RemoveBuffById(slowBuffId);

            if (debugLog)
                Debug.Log($"[SteamVentZone] Slow Removed | {targetRoot.name}");
        }

        private void ClearSlowFromAllInsideTargets()
        {
            var snapshot = new List<Collider2D>(insideTargets);

            foreach (var hitCol in snapshot)
            {
                if (hitCol == null) continue;

                Player player = hitCol.GetComponentInParent<Player>();

                if (player != null)
                {
                    RemoveSlow(player.gameObject);
                    continue;
                }

                Transform root = hitCol.transform.root;

                if (root != null)
                    RemoveSlow(root.gameObject);
            }
        }

        private void ApplyVisualState()
        {
            if (zoneRenderer != null)
                zoneRenderer.color = isHacked ? hackedColor : normalColor;
        }

        public void FitColliderToSprite()
        {
            if (zoneRenderer == null || zoneRenderer.sprite == null || zoneTrigger == null)
                return;

            Vector2 size = (Vector2)zoneRenderer.sprite.bounds.size + colliderPadding;
            Vector2 offset = (Vector2)zoneRenderer.sprite.bounds.center;

            if (zoneTrigger is BoxCollider2D box)
            {
                box.size = size;
                box.offset = offset;
            }
            else if (zoneTrigger is CircleCollider2D circle)
            {
                float radius = Mathf.Max(size.x, size.y) * 0.5f;
                circle.radius = radius;
                circle.offset = offset;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            insideTargets.Add(other);

            Player player = other.GetComponentInParent<Player>();

            if (player != null)
            {
                currentPlayerInZone = player;

                Debug.Log(
                    $"[SteamVentZone] Player Enter | " +
                    $"name={player.name}, type={player.CharacterType}, " +
                    $"layer={LayerMask.LayerToName(player.gameObject.layer)}, tag={player.tag}"
                );
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            insideTargets.Add(other);

            if (currentPlayerInZone != null)
                return;

            Player player = other.GetComponentInParent<Player>();

            if (player != null)
            {
                currentPlayerInZone = player;

                Debug.Log(
                    $"[SteamVentZone] Player Stay Recover | " +
                    $"name={player.name}, type={player.CharacterType}, " +
                    $"layer={LayerMask.LayerToName(player.gameObject.layer)}, tag={player.tag}"
                );
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            insideTargets.Remove(other);

            Player player = other.GetComponentInParent<Player>();

            if (player != null)
            {
                RemoveSlow(player.gameObject);

                if (currentPlayerInZone == player)
                {
                    currentPlayerInZone = null;
                    isHackRequestPending = false;
                }

                Debug.Log($"[SteamVentZone] Player Exit | {player.name}");

                return;
            }

            Transform root = other.transform.root;

            if (root != null && root.CompareTag(enemyTag))
            {
                RemoveSlow(root.gameObject);
            }
        }

        private bool IsInTargetMask(int layer)
        {
            if (targetMask.value == 0)
                return true;

            return (targetMask.value & (1 << layer)) != 0;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (zoneTrigger == null)
                zoneTrigger = GetComponent<Collider2D>();

            if (zoneRenderer == null)
                zoneRenderer = GetComponent<SpriteRenderer>();

            if (hackableObject == null)
                hackableObject = GetComponent<HackableObject>();

            if (zoneTrigger != null)
                zoneTrigger.isTrigger = true;

            if (autoFitCollider)
                FitColliderToSprite();
        }
#endif
    }
}