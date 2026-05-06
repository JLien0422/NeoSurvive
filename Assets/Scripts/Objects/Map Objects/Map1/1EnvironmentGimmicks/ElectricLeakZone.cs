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
    public class ElectricLeakZone : MonoBehaviour
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

            zoneTrigger.isTrigger = true;
            ApplyVisualState();
        }

        private void Start()
        {
            LoadFromCSV();
        }

        private void Update()
        {
            HandleHackStartInput();
            HandlePulse();
        }

        private void LoadFromCSV()
        {
            var row = MapEnvironmentGimmickLoader.DB.Get(mapId, gimmickId);
            if (row == null) return;

            startActive = row.isActive;
            canHack = row.canHack;

            pulseInterval = row.pulseInterval;
            pulseDamage = row.damage;

            hackedEnemyDamage = row.damage;
            stunDuration = row.hackedEffectDuration;

            Debug.Log("[ElectricLeakZone] CSV 로드 완료");
        }

        private void HandleHackStartInput()
        {
            if (!canHack) return;
            if (isHacked) return;
            if (isHackRequestPending) return;
            if (currentPlayerInZone == null) return;
            if (!currentPlayerInZone.CharacterType.Equals(CharacterType.Hacker)) return;
            if (HackingSystem.Instance == null) return;

            if (Input.GetKeyDown(hackKey))
            {
                // 🔥 중복 방지
                if (HackingSystem.Instance.IsHacking)
                {
                    Debug.Log("[ElectricLeakZone] 이미 다른 해킹 진행 중");
                    return;
                }

                isHackRequestPending = true;

                // 🔥 랜덤 미니게임
                HackableObjectType randomType = GetRandomHackableObjectType();

                Debug.Log($"[ElectricLeakZone] 해킹 시작 | 랜덤 미니게임: {randomType}");

                HackingSystem.Instance.StartHacking(
                    randomType,
                    OnHackSucceededFromSystem,
                    OnHackFailedFromSystem,
                    transform.position
                );
            }
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
                    if (!isHacked)
                    {
                        var ch = player.GetComponent<Character>();
                        if (ch != null)
                        {
                            float dmg = player.ApplyIncomingDamage(pulseDamage);
                            ch.TakeDamage(dmg);
                        }
                    }
                    continue;
                }

                Transform root = col.transform.root;
                if (!root.CompareTag(enemyTag)) continue;

                var enemy = root.GetComponent<Character>();
                if (enemy == null) continue;

                if (isHacked)
                {
                    enemy.TakeDamage(hackedEnemyDamage);

                    var buff = root.GetComponent<BuffHandler>() ?? root.gameObject.AddComponent<BuffHandler>();
                    buff.AddBuff(new StunDebuff(stunDuration));
                }
                else
                {
                    enemy.TakeDamage(pulseDamage);
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
                currentPlayerInZone = player;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            insideTargets.Remove(other);

            Player player = other.GetComponentInParent<Player>();
            if (player != null && currentPlayerInZone == player)
            {
                currentPlayerInZone = null;
                isHackRequestPending = false;
            }
        }
    }
}