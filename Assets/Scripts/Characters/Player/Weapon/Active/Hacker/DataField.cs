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
            WeaponBase srcWeapon)
        {
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

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryApplyBuff(other);
        }

        private void TryApplyBuff(Collider2D other)
        {
            Enemy enemy = other.GetComponentInParent<Enemy>();
            if (enemy == null) return;

            GameObject target = enemy.gameObject;

            BuffHandler buffHandler = target.GetComponent<BuffHandler>();
            if (buffHandler == null)
                buffHandler = target.AddComponent<BuffHandler>();

            // 🔥 [핵심 수정] 기존 DataOptimizationDamageBuff → 제거
            // 🔥 [변경] VulnerableDebuff 사용 (기존 Buff 시스템 활용)
            buffHandler.AddBuff(new VulnerableDebuff(damageMultiplier, buffDuration));

            // 🔥 [추가] Lv5 슬로우 → SlowDebuff로 처리
            if (enableMasterSlow)
            {
                buffHandler.AddBuff(new SlowDebuff(slowMultiplier, slowDuration));
            }

            // ❌ [삭제] 기존 거리 기반 슬로우 판정 제거
            // (slowRadius, distance 체크 로직 제거됨)
        }
    }
}