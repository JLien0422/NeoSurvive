using UnityEngine;
using NeoSurvive.Buff;
using NeoSurvive.Core;

namespace NeoSurvive.Weapon
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class DataField : MonoBehaviour
    {
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Vector2 visualSizeMultiplier = new Vector2(1.1f, 1.05f);

        private float damageMultiplier;
        private float buffDuration;
        private bool enableMasterSlow;
        private float slowMultiplier;
        private float slowDuration;

        private BoxCollider2D boxCol;
        private WeaponBase sourceWeapon;
        private float lifetime = -1f;
        private float age;
        private float performance = 1f;
        private const uint AmplificationBuffId = 0x5254414D;
        private readonly System.Collections.Generic.Dictionary<Character, float> nextRefresh =
            new System.Collections.Generic.Dictionary<Character, float>();

        private void Awake()
        {
            boxCol = GetComponent<BoxCollider2D>();
            boxCol.isTrigger = true;
        }

        public void Initialize(
            Vector2 fieldSize,          // 🔥 [추가] fieldSize를 직접 받도록 변경
            float damageMultiplier,
            float buffDuration,
            bool enableMasterSlow,
            float slowMultiplier,
            float slowDuration,
            WeaponBase srcWeapon, float fieldLifetime = -1f, float performance = 1f)
        {
            sourceWeapon = srcWeapon;
            this.performance = performance;
            lifetime = fieldLifetime;
            age = 0f;
            this.damageMultiplier = damageMultiplier;
            this.buffDuration = buffDuration;
            this.enableMasterSlow = enableMasterSlow;
            this.slowMultiplier = slowMultiplier;
            this.slowDuration = slowDuration;

            if (boxCol == null)
                boxCol = GetComponent<BoxCollider2D>();

            boxCol.isTrigger = true;

            // 🔥 [핵심 수정] 오브젝트 scale이 아니라 Collider 기준으로 크기 설정
            boxCol.size = fieldSize;
            boxCol.offset = Vector2.zero;

            // 🔥 [추가] 시각 오브젝트도 동일한 기준으로 맞춤
            if (visualRoot != null)
            {
                visualRoot.localScale = new Vector3(
                    fieldSize.x * visualSizeMultiplier.x,
                    fieldSize.y * visualSizeMultiplier.y,
                    1f
                );
            }
        }

        private void OnDestroy()
        {
            if (InGameSoundManager.Instance != null)
            {
                InGameSoundManager.Instance.PlayDataOptimizationDestroy();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryApplyBuff(other);
        }

        private void OnTriggerStay2D(Collider2D other) => TryApplyBuff(other);

        private void Update()
        {
            age += Time.deltaTime;
            if (lifetime >= 0f && age >= RunTraitWeaponStats.Scale(sourceWeapon, lifetime, RunTraitStat.Lifetime))
                Destroy(gameObject);
        }

        private void TryApplyBuff(Collider2D other)
        {
            Character enemy = RunTraitTarget.Find(other);
            if (enemy == null) return;
            if (nextRefresh.TryGetValue(enemy, out float refreshAt) && Time.time < refreshAt) return;
            nextRefresh[enemy] = Time.time + 0.2f;

            if (InGameSoundManager.Instance != null)
            {
                InGameSoundManager.Instance.PlayDataOptimizationAttack();
            }

            GameObject target = enemy.gameObject;

            BuffHandler buffHandler = target.GetComponent<BuffHandler>();
            if (buffHandler == null)
                buffHandler = target.AddComponent<BuffHandler>();

            // 🔥 [핵심 수정] 기존 DataOptimizationDamageBuff → 제거
            // 🔥 [변경] VulnerableDebuff 사용 (기존 Buff 시스템 활용)
            float effectiveMultiplier = 1f + Mathf.Max(0f, RunTraitWeaponStats.Amplification(sourceWeapon, damageMultiplier) - 1f) * performance;
            float effectiveDuration = RunTraitWeaponStats.Scale(sourceWeapon, buffDuration, RunTraitStat.AmplificationDuration) * performance;
            if (!RunTraitAmplificationReceiver.ShouldReplaceField(enemy, effectiveMultiplier)) return;
            buffHandler.AddBuffWithId(AmplificationBuffId, new VulnerableDebuff(effectiveMultiplier, effectiveDuration));
            RunTraitAmplificationReceiver.Record(enemy, sourceWeapon, effectiveMultiplier, effectiveDuration);

            // 🔥 [추가] Lv5 슬로우 → SlowDebuff로 처리
            if (enableMasterSlow)
            {
                RunTraitStatus.ApplySlow(target, sourceWeapon, slowMultiplier, slowDuration, performance);
            }

            // ❌ [삭제] 기존 거리 기반 슬로우 판정 제거
            // (slowRadius, distance 체크 로직 제거됨)
        }
    }
}
