using System.Collections;
using UnityEngine;

namespace NeoSurvive.Buff
{
    /// <summary>
    /// 넉백 디버프
    ///
    /// [역할]
    /// - EnemyController 안에 있던 넉백 상태 처리 분리
    /// - Rigidbody2D에 직접 velocity를 주고 duration 동안 AI 이동이 덮어쓰지 못하게 함
    /// - 넉백 종료 후 속도를 0으로 정리
    ///
    /// [사용 흐름]
    /// DigitalShield / ShieldWave
    /// -> enemy.GetComponent<KnockbackDebuff>()
    /// -> ApplyKnockback(velocity, duration)
    ///
    /// [왜 분리했는가]
    /// - EnemyController는 적 AI 이동/공격만 담당
    /// - 넉백은 상태이상/디버프 계열이므로 Buff 폴더에서 관리
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class KnockbackDebuff : MonoBehaviour
    {
        private Rigidbody2D rb;
        private Coroutine knockbackRoutine;
        private Player player;

        public bool IsKnockbackActive { get; private set; }

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            player = GetComponent<Player>();
            if (player == null)
                player = GetComponentInParent<Player>();
        }

        public void ApplyKnockback(Vector2 velocity, float duration)
        {
            if (rb == null) return;

            if (Player.Instance != null && Player.Instance.HasTrait("inertia_frame"))
            {
                float resistance = Player.Instance.GetKnockbackResistanceMultiplier();
                velocity *= resistance;
                duration *= resistance;
            }

            if (knockbackRoutine != null)
                StopCoroutine(knockbackRoutine);

            knockbackRoutine = StartCoroutine(KnockbackRoutine(velocity, duration));
        }

        private IEnumerator KnockbackRoutine(Vector2 velocity, float duration)
        {
            IsKnockbackActive = true;

            rb.velocity = velocity;

            yield return new WaitForSeconds(duration);

            rb.velocity = Vector2.zero;
            IsKnockbackActive = false;

            knockbackRoutine = null;
        }
    }
}