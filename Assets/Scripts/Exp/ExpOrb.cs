using UnityEngine;

namespace NeoSurvive.Exp
{
    /// <summary>
    /// 경험치 오브
    /// OnTriggerEnter2D 대신 DistanceManager의 거리기반 픽업 시스템을 사용합니다.
    /// </summary>
    public class ExpOrb : MonoBehaviour, IPickupable
    {
        [SerializeField] private int amount = 1;
        [SerializeField] private float pickupRange = 0.5f; // 픽업 판정 반경

        // IPickupable 구현
        public Transform Transform => transform;
        public float PickupRange => pickupRange;

        public void SetAmount(int value)
        {
            amount = Mathf.Max(1, value);
        }

        private void OnEnable()
        {
            // 스폰 시 DistanceManager에 등록
            if (DistanceManager.Instance != null)
                DistanceManager.Instance.Register(this);
        }

        private void OnDisable()
        {
            // 비활성화/소멸 시 등록 해제
            if (DistanceManager.Instance != null)
                DistanceManager.Instance.Unregister(this);
        }

        /// <summary>
        /// DistanceManager가 범위 이내 진입 시 호출합니다.
        /// </summary>
        public void Pickup(Player player)
        {
            if (player == null) return;
            player.GainExperience(amount);
            MainSceneSoundManager.Instance?.PlayExpPickup();
            Destroy(gameObject);
        }
    }
}
