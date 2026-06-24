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
    public class ElectricLeakZone : MonoBehaviour, IHackPromptProvider
    {
        [Header("CSV")]
        [SerializeField] private string mapId = "Map1";
        [SerializeField] private string gimmickId = "electric_leak";

        [Header("Zone")]
        [SerializeField] private Collider2D zoneTrigger;

        [Header("Pulse")]
        [SerializeField] private bool startActive = true;
        [SerializeField] private float pulseInterval = 2f;
        [SerializeField] private float pulseDamage = 10f;

        [Header("Hack")]
        [SerializeField] private KeyCode hackKey = KeyCode.E;

        [Header("After Hack")]
        [SerializeField] private float hackedEnemyDamage = 10f;
        [SerializeField] private float stunDuration = 1.25f;
        [SerializeField] private string enemyTag = "Enemy";

        [Header("Target Filter")]
        [SerializeField] private LayerMask targetMask;

        [Header("Visual")]
        [SerializeField] private SpriteRenderer zoneRenderer;
        [SerializeField] private Color normalColor = new Color(1f, 0.2f, 0.2f, 0.5f);
        [SerializeField] private Color hackedColor = new Color(0.3f, 0.8f, 1f, 0.6f);

        [Header("Debug")]
        [SerializeField] private bool debugLog = true;

        private readonly HashSet<Collider2D> insideTargets = new HashSet<Collider2D>();

        private float pulseTimer = 0f;
        private bool isHacked = false;
        private bool canHack = true;
        private bool isHackRequestPending = false;

        private Player currentPlayerInZone;

        private void Awake()
        {
            if (zoneTrigger == null)
                zoneTrigger = GetComponent<Collider2D>();

            if (zoneRenderer == null)
                zoneRenderer = GetComponent<SpriteRenderer>();

            if (zoneTrigger != null)
                zoneTrigger.isTrigger = true;

            HackableObject hackable = GetComponent<HackableObject>();

            if (hackable == null)
            {
                hackable = gameObject.AddComponent<HackableObject>();

                if (debugLog)
                    Debug.Log("[ElectricLeakZone] HackableObject 자동 추가");
            }

            ApplyVisualState();
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
                    $"[ElectricLeakZone] Start Check | " +
                    $"canHack={canHack}, " +
                    $"zoneTrigger={(zoneTrigger != null ? zoneTrigger.name : "NULL")}, " +
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
                Debug.LogWarning("[ElectricLeakZone] MapEnvironmentGimmickLoader.DB == null");
                return;
            }

            var row = MapEnvironmentGimmickLoader.DB.Get(mapId, gimmickId);

            if (row == null)
            {
                Debug.LogWarning($"[ElectricLeakZone] CSV row 없음 | mapId={mapId}, gimmickId={gimmickId}");
                return;
            }

            startActive = row.isActive;
            canHack = row.canHack;

            pulseInterval = row.pulseInterval;
            pulseDamage = row.damage;

            hackedEnemyDamage = row.damage;
            stunDuration = row.hackedEffectDuration;

            if (debugLog)
            {
                Debug.Log(
                    $"[ElectricLeakZone] CSV 로드 완료 | " +
                    $"startActive={startActive}, canHack={canHack}, " +
                    $"pulseInterval={pulseInterval}, pulseDamage={pulseDamage}, " +
                    $"hackedEnemyDamage={hackedEnemyDamage}, stunDuration={stunDuration}"
                );
            }
        }

        private void HandleHackStartInput()
        {
            if (!Input.GetKeyDown(hackKey))
                return;

            Debug.Log("[ElectricLeakZone] E 입력 감지");

            if (!canHack)
            {
                Debug.Log("[ElectricLeakZone] 해킹 불가: canHack=false");
                return;
            }

            if (isHacked)
            {
                Debug.Log("[ElectricLeakZone] 해킹 불가: 이미 해킹 완료");
                return;
            }

            if (isHackRequestPending)
            {
                Debug.Log("[ElectricLeakZone] 해킹 불가: 이미 해킹 요청 중");
                return;
            }

            if (currentPlayerInZone == null)
            {
                Debug.Log("[ElectricLeakZone] 해킹 불가: currentPlayerInZone == null");
                return;
            }

            Debug.Log(
                $"[ElectricLeakZone] Player 감지 | " +
                $"name={currentPlayerInZone.name}, " +
                $"type={currentPlayerInZone.CharacterType}, " +
                $"layer={LayerMask.LayerToName(currentPlayerInZone.gameObject.layer)}, " +
                $"tag={currentPlayerInZone.tag}"
            );

            if (HackingSystem.Instance == null)
            {
                Debug.Log("[ElectricLeakZone] 해킹 불가: HackingSystem.Instance == null");
                return;
            }

            if (HackingSystem.Instance.IsHacking)
            {
                Debug.Log("[ElectricLeakZone] 해킹 불가: 이미 다른 해킹 진행 중");
                return;
            }

            isHackRequestPending = true;

            HackableObjectType randomType = GetRandomHackableObjectType();

            Debug.Log($"[ElectricLeakZone] 해킹 시작 | 랜덤 미니게임: {randomType}");

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

        private void OnHackSucceededFromSystem()
        {
            if (isHacked) return;

            isHacked = true;
            isHackRequestPending = false;

            ApplyVisualState();

            Debug.Log("[ElectricLeakZone] Hack Success → 누전구역 활성화");
        }

        private void OnHackFailedFromSystem()
        {
            isHackRequestPending = false;

            if (currentPlayerInZone != null)
            {
                currentPlayerInZone.AddPsychoCorruption(50f);
                Debug.Log("[ElectricLeakZone] 해킹 실패 → 사이코잠식도 증가");
            }
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
            foreach (var col in new List<Collider2D>(insideTargets))
            {
                if (col == null) continue;

                Player player = col.GetComponentInParent<Player>();

                if (player != null)
                {
                    if (!IsInTargetMask(player.gameObject.layer))
                        continue;

                    if (!isHacked)
                    {
                        Character ch = player.GetComponent<Character>();

                        if (ch != null)
                        {
                            float dmg = player.ApplyIncomingDamage(pulseDamage);
                            ch.TakeDamage(dmg);

                            if (debugLog)
                                Debug.Log($"[ElectricLeakZone] Player Damage | {player.name}, dmg={dmg}");
                        }
                    }

                    continue;
                }

                Transform root = col.transform.root;

                if (root == null)
                    continue;

                if (!root.CompareTag(enemyTag))
                    continue;

                if (!IsInTargetMask(root.gameObject.layer))
                    continue;

                Character enemy = root.GetComponent<Character>();

                if (enemy == null)
                    enemy = root.GetComponentInParent<Character>();

                if (enemy == null)
                    continue;

                if (isHacked)
                {
                    enemy.TakeDamage(hackedEnemyDamage);

                    BuffHandler buff = root.GetComponent<BuffHandler>();

                    if (buff == null)
                        buff = root.gameObject.AddComponent<BuffHandler>();

                    buff.AddBuff(new StunDebuff(stunDuration));

                    if (debugLog)
                        Debug.Log($"[ElectricLeakZone] Enemy Damage + Stun | {root.name}");
                }
                else
                {
                    enemy.TakeDamage(pulseDamage);

                    if (debugLog)
                        Debug.Log($"[ElectricLeakZone] Enemy Damage | {root.name}");
                }
            }
        }

        private void ApplyVisualState()
        {
            if (zoneRenderer != null)
                zoneRenderer.color = isHacked ? hackedColor : normalColor;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            insideTargets.Add(other);

            Player player = other.GetComponentInParent<Player>();

            if (player != null)
            {
                currentPlayerInZone = player;

                Debug.Log(
                    $"[ElectricLeakZone] Player Enter | " +
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
                    $"[ElectricLeakZone] Player Stay Recover | " +
                    $"name={player.name}, type={player.CharacterType}, " +
                    $"layer={LayerMask.LayerToName(player.gameObject.layer)}, tag={player.tag}"
                );
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            insideTargets.Remove(other);

            Player player = other.GetComponentInParent<Player>();

            if (player != null && currentPlayerInZone == player)
            {
                currentPlayerInZone = null;
                isHackRequestPending = false;

                Debug.Log($"[ElectricLeakZone] Player Exit | {player.name}");
            }
        }

        private bool IsInTargetMask(int layer)
        {
            if (targetMask.value == 0)
                return true;

            return (targetMask.value & (1 << layer)) != 0;
        }
    }
}