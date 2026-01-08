using UnityEngine;
using NeoSurvive.Core;
using NeoSurvive.Buff;

namespace NeoSurvive.Enemy
{
    /// <summary>
    /// 임시 적 베이스 클래스
    /// </summary>
    public class EnemyBase : MonoBehaviour, IDamageable
    {
        public float health = 50f;
        public float moveSpeed = 2f;

        protected Transform player;

        protected virtual void Start()
        {
            // 플레이어 태그를 가진 객체를 찾음
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }

        protected virtual void Update()
        {
            if (player != null)
            {
                Vector3 direction = (player.position - transform.position).normalized;
                transform.position += direction * moveSpeed * Time.deltaTime;
            }
        }

        public void TakeDamage(float amount)
        {
            health -= amount;

            // 데미지 텍스트 표시
            if (DamageTextManager.Instance != null)
            {
                DamageTextManager.Instance.ShowDamageText(transform.position + Vector3.up * 0.5f, amount);
            }

            Debug.Log($"{gameObject.name} 체력: {health}");
            if (health <= 0)
            {
                Die();
            }
        }

        protected virtual void Die()
        {
            Debug.Log($"[EnemyBase] Die() 호출됨: {gameObject.name} (type={GetType().Name})");

        OnDeath();

        Debug.Log($"{gameObject.name} 처치됨!");
        Destroy(gameObject);
        }


        /// <summary>
        /// 죽을 때 추가 행동(경험치 드랍, 이펙트, 골드 등)을 파생 클래스에서 구현
        /// </summary>
        protected virtual void OnDeath()
        {
            // 기본 EnemyBase는 아무것도 안 함
        }
    }
}
