using System.Collections.Generic;
using UnityEngine;
using NeoSurvive.Core;
using NeoSurvive.Buff;

namespace NeoSurvive.Weapon
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class DataField : MonoBehaviour
    {
        [Header("Enemy Filter")]
        [SerializeField] private string enemyTag = "Enemy";

        [Header("Visual")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Vector2 visualSizeMultiplier = new Vector2(1.1f, 1.05f);

        private Player owner;
        private float duration;
        private Vector2 fieldSize;
        private float damageMultiplier;
        private float buffDuration;
        private bool enableMasterSlow;
        private float slowMultiplier;
        private float slowDuration;
        private bool debugLog;

        private float lifeTimer = 0f;
        private BoxCollider2D boxCol;

        private readonly HashSet<GameObject> buffedProjectiles = new();


        public void Initialize(
            Player owner,
            float duration,
            Vector2 fieldSize,
            float damageMultiplier,
            float buffDuration,
            bool enableMasterSlow,
            float slowMultiplier,
            float slowDuration,
            bool debugLog)
        {
            this.owner = owner;
            this.duration = duration;
            this.fieldSize = fieldSize;
            this.damageMultiplier = damageMultiplier;
            this.buffDuration = buffDuration;
            this.enableMasterSlow = enableMasterSlow;
            this.slowMultiplier = slowMultiplier;
            this.slowDuration = slowDuration;
            this.debugLog = debugLog;

            if (boxCol == null)
                boxCol = GetComponent<BoxCollider2D>();

            boxCol.isTrigger = true;
            boxCol.size = fieldSize;

            transform.localScale = Vector3.one;

            if (visualRoot != null)
            {
                visualRoot.localScale = new Vector3(
                    fieldSize.x * visualSizeMultiplier.x,
                    fieldSize.y * visualSizeMultiplier.y,
                    1f
                );
            }
        }

        private void Awake()
        {
            boxCol = GetComponent<BoxCollider2D>();
            boxCol.isTrigger = true;
        }

        private void Update()
        {
            lifeTimer += Time.deltaTime;
            if (lifeTimer >= duration)
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryBuffProjectile(other);
            TryApplyFieldSlow(other);
        }

        private void TryBuffProjectile(Collider2D other)
        {
            if (other == null) return;

            GameObject target = other.gameObject;

            Projectile projectile = target.GetComponent<Projectile>();
            if (projectile == null)
                return;

            if (buffedProjectiles.Contains(target))
                return;

            BuffHandler buffHandler = target.GetComponent<BuffHandler>();
            if (buffHandler == null)
                buffHandler = target.AddComponent<BuffHandler>();

            StatusFlags flags = target.GetComponent<StatusFlags>();
            if (flags == null)
                flags = target.AddComponent<StatusFlags>();

            buffHandler.AddBuff(new DataOptimizationDamageBuff(damageMultiplier, buffDuration));
            buffedProjectiles.Add(target);

            if (debugLog)
            {
                Debug.Log($"[DataField] 투사체 버프 적용 | projectile={target.name} | x{damageMultiplier:0.00}");
            }
        }

        private void TryApplyFieldSlow(Collider2D other)
        {
            if (!enableMasterSlow || other == null)
                return;

            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy == null)
                enemy = other.GetComponentInParent<Enemy>();

            if (enemy == null)
                return;

            GameObject target = enemy.gameObject;

            if (!target.CompareTag(enemyTag))
                return;

            BuffHandler buffHandler = target.GetComponent<BuffHandler>();
            if (buffHandler == null)
                buffHandler = target.AddComponent<BuffHandler>();

            buffHandler.AddBuff(new SlowDebuff(slowMultiplier, slowDuration));

            if (debugLog)
            {
                Debug.Log($"[DataField] SlowDebuff 적용 | target={target.name} | multiplier={slowMultiplier:0.00} | duration={slowDuration:0.00}");
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(fieldSize.x, fieldSize.y, 0.1f));
        }
#endif
    }
}