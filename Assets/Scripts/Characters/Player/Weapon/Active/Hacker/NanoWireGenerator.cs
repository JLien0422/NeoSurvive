using UnityEngine;

namespace NeoSurvive.Weapon
{
    /// <summary>
    /// 10번 무기: 나노 전선 생성기
    /// 플레이어가 이동한 경로에 나노 전선을 설치.
    /// </summary>
    public class NanoWireGenerator : MonoBehaviour
    {
        [Header("Stats")]
        public GameObject nanoWirePrefab; // NanoWire 스크립트 필요

        public float damage = 15f;
        public float duration = 4f;       // 전선 유지 시간
        public float spawnDistance = 1.0f; // 이만큼 이동할 때마다 설치

        private Vector3 lastSpawnPos;
        private int currentLevel = 1;

        // Base Stats
        private float baseDamage;
        private float baseDuration;

        private void Start()
        {
            baseDamage = damage;
            baseDuration = duration;
            lastSpawnPos = transform.position;
        }

        private void Update()
        {
            // 이동 거리 체크
            float dist = Vector3.Distance(transform.position, lastSpawnPos);
            if (dist >= spawnDistance)
            {
                SpawnWire();
                lastSpawnPos = transform.position;
            }
        }

        private void SpawnWire()
        {
            if (nanoWirePrefab == null) return;

            // 현재 위치에 설치
            GameObject obj = Instantiate(nanoWirePrefab, transform.position, Quaternion.identity);

            // 회전 조절? (진행 방향에 수직 등) - 일단은 기본(Identity)

            if (obj.TryGetComponent<NanoWire>(out var wire))
            {
                var src = GetComponentInParent<WeaponSource>();
                wire.Initialize(damage, duration, src != null ? src.weaponData : null);
            }
        }

        public void OnLevelUp(int level)
        {
            currentLevel = level;
            if (baseDamage == 0 && damage > 0) baseDamage = damage;
            if (baseDuration == 0 && duration > 0) baseDuration = duration;

            // 레벨업: 데미지 20% 증가, 지속시간 10% 증가
            damage = baseDamage * (1f + (level - 1) * 0.2f);
            duration = baseDuration * (1f + (level - 1) * 0.1f);

            Debug.Log($"[NanoWire] Lv.{level} : Dmg {damage}, Duration {duration}");
        }
    }
}
