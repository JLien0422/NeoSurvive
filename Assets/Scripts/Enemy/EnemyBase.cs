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
            Debug.Log($"{gameObject.name} 처치됨!");
            Destroy(gameObject);
        }
    }
}
