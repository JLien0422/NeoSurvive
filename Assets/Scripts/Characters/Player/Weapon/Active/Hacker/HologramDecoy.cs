using UnityEngine;
using NeoSurvive.Characters;
using NeoSurvive.Buff;

namespace NeoSurvive.Weapon
{
    /// <summary>
    /// 9번 무기 소환물: 홀로그램 디코이
    /// 적을 유인하며, 파괴 시 데이터 감옥 발동
    /// </summary>
    public class HologramDecoy : Character
    {
        private bool spawnPrisonOnDeath;
        private float prisonDuration;
        private float prisonRange;

        private Vector3 fixedPosition;

        private GameObject bodyVfxInstance;

        private GameObject fieldEndVfxPrefab;
        private GameObject baseEndVfxPrefab;

        private float fieldEndVfxLifetime;
        private float baseEndVfxLifetime;

        private float vfxScale = 1f;
        private float bodyVfxScale = 1f;

        private bool initialized = false;
        private bool destroyedHandled = false;

        public void Initialize(
            float hp,
            bool spawnPrison,
            float prisonDur,
            float prisonRange,
            GameObject bodyVfxPrefab,
            GameObject fieldEndVfxPrefab,
            GameObject baseEndVfxPrefab,
            float fieldEndVfxLifetime,
            float baseEndVfxLifetime,
            float vfxScale,
            float bodyVfxScale)
        {
            currentHP = new Stat(hp);

            this.spawnPrisonOnDeath = spawnPrison;
            this.prisonDuration = prisonDur;
            this.prisonRange = prisonRange;

            this.fieldEndVfxPrefab = fieldEndVfxPrefab;
            this.baseEndVfxPrefab = baseEndVfxPrefab;

            this.fieldEndVfxLifetime = fieldEndVfxLifetime;
            this.baseEndVfxLifetime = baseEndVfxLifetime;

            this.vfxScale = vfxScale;
            this.bodyVfxScale = bodyVfxScale;

            gameObject.tag = "Decoy";

            fixedPosition = transform.position;

            // ★ 수정: Body VFX를 먼저 만들고, 그 크기 기준으로 Collider 설정
            SpawnBodyVFX(bodyVfxPrefab);
            SetupDecoyHitBody();

            initialized = true;
        }

        private void LateUpdate()
        {
            transform.position = fixedPosition;
        }

        private void SetupDecoyHitBody()
        {
            float radius = 0.25f;

            // ★ 추가: Body VFX Sprite 크기 기준으로 Collider 크기 자동 계산
            SpriteRenderer sr =
                bodyVfxInstance != null
                    ? bodyVfxInstance.GetComponentInChildren<SpriteRenderer>()
                    : null;

            if (sr != null)
            {
                Bounds b = sr.bounds;

                radius =
                    Mathf.Max(
                        b.extents.x,
                        b.extents.y
                    ) * 0.35f;
            }

            Collider2D col = GetComponent<Collider2D>();

            if (col == null)
            {
                CircleCollider2D circle =
                    gameObject.AddComponent<CircleCollider2D>();

                circle.radius = radius;
                circle.isTrigger = true;
            }
            else
            {
                col.isTrigger = true;

                if (col is CircleCollider2D circle)
                {
                    circle.radius = radius;
                }
            }

            Rigidbody2D rb = GetComponent<Rigidbody2D>();

            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody2D>();
            }

            rb.gravityScale = 0f;
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.constraints = RigidbodyConstraints2D.FreezeAll;
        }

        private void SpawnBodyVFX(GameObject bodyVfxPrefab)
        {
            if (bodyVfxPrefab == null)
                return;

            bodyVfxInstance =
                Instantiate(
                    bodyVfxPrefab,
                    transform.position,
                    Quaternion.identity
                );

            bodyVfxInstance.transform.localScale =
                Vector3.one * bodyVfxScale;

            Animator animator =
                bodyVfxInstance.GetComponentInChildren<Animator>();

            if (animator != null)
            {
                animator.Rebind();
                animator.Update(0f);
                animator.Play(0, 0, 0f);
            }

            HologramVfxFollower follower =
                bodyVfxInstance.AddComponent<HologramVfxFollower>();

            follower.Initialize(transform);
        }

        private void OnDestroy()
        {
            if (!initialized)
                return;

            if (destroyedHandled)
                return;

            destroyedHandled = true;

            Vector3 pos = transform.position;

            if (bodyVfxInstance != null)
            {
                Destroy(bodyVfxInstance);
            }

            SpawnOneShotVFX(
                fieldEndVfxPrefab,
                pos,
                fieldEndVfxLifetime,
                vfxScale
            );

            SpawnOneShotVFX(
                baseEndVfxPrefab,
                pos,
                baseEndVfxLifetime,
                vfxScale
            );

            if (spawnPrisonOnDeath)
            {
                SpawnDataPrison();
            }
        }

        private void SpawnOneShotVFX(
            GameObject prefab,
            Vector3 pos,
            float lifetime,
            float scale)
        {
            if (prefab == null)
                return;

            GameObject vfx =
                Instantiate(
                    prefab,
                    pos,
                    Quaternion.identity
                );

            vfx.transform.localScale =
                Vector3.one * scale;

            Destroy(vfx, lifetime);
        }

        private void SpawnDataPrison()
        {
            GameObject prison =
                new GameObject("DataPrison");

            prison.transform.position =
                transform.position;

            DataPrisonEffect prisonScript =
                prison.AddComponent<DataPrisonEffect>();

            prisonScript.Initialize(
                prisonRange,
                prisonDuration
            );
        }
    }

    public class HologramVfxFollower : MonoBehaviour
    {
        private Transform target;

        public void Initialize(Transform target)
        {
            this.target = target;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                Destroy(gameObject);
                return;
            }

            transform.position =
                target.position;
        }
    }

    public class DataPrisonEffect : MonoBehaviour
    {
        private float range;
        private float duration;

        public void Initialize(float range, float duration)
        {
            this.range = range;
            this.duration = duration;

            ApplySnareOnce();

            Destroy(gameObject, duration);
        }

        private void ApplySnareOnce()
        {
            Collider2D[] hits =
                Physics2D.OverlapCircleAll(
                    transform.position,
                    range
                );

            foreach (var hit in hits)
            {
                if (hit == null)
                    continue;

                Enemy enemy =
                    hit.GetComponentInParent<Enemy>();

                if (enemy == null)
                    continue;

                if (!enemy.CompareTag("Enemy"))
                    continue;

                BuffUtil.Apply(
                    enemy.gameObject,
                    new RootDebuff(duration)
                );
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.blue;

            Gizmos.DrawWireSphere(
                transform.position,
                range
            );
        }
#endif
    }
}