using UnityEngine;
using NeoSurvive.Core;

namespace NeoSurvive.Player
{
    /// <summary>
    /// 임시 플레이어 컨트롤러
    /// </summary>
    public class PlayerController : MonoBehaviour, IDamageable
    {
        public float moveSpeed = 5f;
        public float health = 100f;

        private Vector2 moveInput;
        private Rigidbody2D rb;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
        }

        void Update()
        {
            moveInput.x = Input.GetAxisRaw("Horizontal");
            moveInput.y = Input.GetAxisRaw("Vertical");
        }

        void FixedUpdate()
        {
            if (rb != null)
            {
                rb.velocity = moveInput.normalized * moveSpeed;
            }
            else
            {
                transform.Translate(moveInput.normalized * moveSpeed * Time.fixedDeltaTime);
            }
        }

        public void TakeDamage(float amount)
        {
            health -= amount;
            Debug.Log($"플레이어 체력: {health}");
            if (health <= 0)
            {
                Debug.Log("플레이어 사망!");
            }
        }
    }
}
