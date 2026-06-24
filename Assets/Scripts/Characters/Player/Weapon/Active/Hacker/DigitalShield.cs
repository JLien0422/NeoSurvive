using UnityEngine;

namespace NeoSurvive.Weapon
{
    /// <summary>
    /// 해커 무기: 디지털 실드
    /// - shieldWavePrefab: 판정/데미지/넉백 전용
    /// - shieldWaveVfxPrefab: 애니메이션 전용
    /// </summary>
    public class DigitalShield : MonoBehaviour
    {
        [Header("Prefab")]
        [SerializeField] private GameObject shieldWavePrefab;

        [Header("VFX")]
        [SerializeField] private GameObject shieldWaveVfxPrefab;
        [SerializeField] private float vfxLifetime = 0.5f;
        [SerializeField] private float vfxScale = 1.0f;

        [Header("Runtime Stats (CSV 적용값)")]
        [SerializeField] private float fireRate = 4.0f;
        [SerializeField] private float damage = 12f;
        [SerializeField] private float hitRadius = 2.5f;
        [SerializeField] private float knockbackForce = 9f;
        [SerializeField] private float knockbackDuration = 0.25f;

        [Header("Master (Lv5 Collision)")]
        [SerializeField] private float collisionDamageMultiplier = 1.0f;
        [SerializeField] private float collisionDetectRadius = 0.7f;
        [SerializeField] private float collisionImpactRadius = 1.2f;
        [SerializeField] private float collisionCarrierDuration = 0.8f;

        [Header("Debug")]
        [SerializeField] private bool debugLog = false;

        private const string weaponId = "digitalshield";

        private Player owner;
        private float timer = 0f;
        private int level = 1;

        private void Awake()
        {
            owner = GetComponentInParent<Player>();
            if (owner == null)
                owner = GetComponent<Player>();

            // [수정] 무기 프리팹에 잘못 부착되어 웨이브가 무한 증식/중첩 발사되는 문제(소리 증폭 등) 방지
            if (owner == null)
            {
                if (debugLog) Debug.LogWarning("[DigitalShield] owner가 없습니다. 잘못된 부착이므로 스크립트를 비활성화합니다.");
                enabled = false;
                return;
            }

            ApplyStatsFromCSV(level);
        }

        private void Update()
        {
            timer += Time.deltaTime;

            if (timer >= fireRate)
            {
                timer = 0f;
                Fire();
            }
        }

        private void Fire()
        {
            if (shieldWavePrefab == null)
            {
                if (debugLog)
                    Debug.LogWarning("[DigitalShield] shieldWavePrefab이 비어 있음");
                return;
            }

            if (InGameSoundManager.Instance != null)
            {
                InGameSoundManager.Instance.PlayDigitalShieldSpawn();
            }

            Vector3 spawnPos = owner != null ? owner.transform.position : transform.position;

            // =========================
            // 기능/판정 프리팹 생성
            // =========================
            GameObject obj = Instantiate(shieldWavePrefab, spawnPos, Quaternion.identity);
            ShieldWave wave = obj.GetComponent<ShieldWave>();

            if (wave == null)
            {
                if (debugLog)
                    Debug.LogWarning("[DigitalShield] ShieldWave 컴포넌트가 프리팹에 없음");
                return;
            }

            float finalDamage = damage;

            bool isMaster = level >= 5;
            WeaponSource source = GetComponentInParent<WeaponSource>();
            WeaponBase sourceWeapon = source != null ? source.weaponData : null;

            wave.InitializeCircle(
                owner,
                finalDamage,
                hitRadius,
                knockbackForce,
                knockbackDuration,
                isMaster,
                finalDamage * collisionDamageMultiplier,
                collisionDetectRadius,
                collisionImpactRadius,
                collisionCarrierDuration,
                "Enemy",
                debugLog,
                sourceWeapon
            );

            // =========================
            // ★ 추가: 애니메이션 전용 VFX 생성
            // =========================
            SpawnShieldWaveVFX(
                spawnPos,
                owner != null ? owner.transform : transform
            );

            if (debugLog)
            {
                Debug.Log(
                    $"[DigitalShield] 발동 | Lv={level} | dmg={finalDamage} | hitRadius={hitRadius} | kb={knockbackForce}"
                );
            }
        }

        // =========================
        // ★ 수정: 플레이어를 따라다니는 VFX
        // =========================
        private void SpawnShieldWaveVFX(Vector3 spawnPos, Transform followTarget)
        {
            if (shieldWaveVfxPrefab == null) return;

            GameObject vfx = Instantiate(
                shieldWaveVfxPrefab,
                spawnPos,
                Quaternion.identity
            );

            vfx.transform.localScale = Vector3.one * vfxScale;

            // ★ 추가: 플레이어를 따라가도록 부모 설정
            if (followTarget != null)
            {
                vfx.transform.SetParent(followTarget);
                vfx.transform.localPosition = Vector3.zero;
            }

            Destroy(vfx, vfxLifetime);
        }

        public void OnLevelUp(int newLevel)
        {
            level = Mathf.Max(1, newLevel);
            ApplyStatsFromCSV(level);

            if (debugLog)
            {
                Debug.Log($"[DigitalShield] OnLevelUp | level={level}");
            }
        }

        private void ApplyStatsFromCSV(int targetLevel)
        {
            if (WeaponStatLoader.DB == null)
            {
                if (debugLog)
                    Debug.LogWarning("[DigitalShield] WeaponStatLoader.DB가 null이라 기본값 사용");
                return;
            }

            if (!WeaponStatLoader.DB.rows.TryGetValue(weaponId, out var levels))
            {
                if (debugLog)
                    Debug.LogWarning($"[DigitalShield] CSV에 weaponId={weaponId} 없음");
                return;
            }

            if (!levels.TryGetValue(targetLevel, out var row))
            {
                if (debugLog)
                    Debug.LogWarning($"[DigitalShield] CSV에 level={targetLevel} 행 없음");
                return;
            }

            if (row.firerate > 0f) fireRate = row.firerate;
            if (row.damage > 0f) damage = row.damage;
            if (row.hitradius > 0f) hitRadius = row.hitradius;
            if (row.knockbackforce > 0f) knockbackForce = row.knockbackforce;
            if (row.knockbackduration > 0f) knockbackDuration = row.knockbackduration;

            if (Player.Instance != null)
            {
                fireRate *= Player.Instance.GetComputeOptimizationCooldownMultiplier();
                knockbackDuration *= Player.Instance.GetSuperconductiveCircuitsDurationMultiplier();
            }

            if (targetLevel >= 5)
            {
                if (row.collisiondamagemultiplier > 0f)
                    collisionDamageMultiplier = row.collisiondamagemultiplier;

                if (row.collisiondetectradius > 0f)
                    collisionDetectRadius = row.collisiondetectradius;

                if (row.collisionimpactradius > 0f)
                    collisionImpactRadius = row.collisionimpactradius;

                if (row.collisioncarrierduration > 0f)
                    collisionCarrierDuration = row.collisioncarrierduration;
            }

            if (debugLog)
            {
                Debug.Log(
                    $"[DigitalShield] CSV 적용 | Lv={targetLevel} | fireRate={fireRate} | damage={damage} | hitRadius={hitRadius} | knockbackForce={knockbackForce}"
                );
            }
        }
    }
}