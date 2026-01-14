using UnityEngine;
using System.Collections.Generic;

namespace NeoSurvive.Weapon
{
    /// <summary>
    /// 9번 무기: 홀로그램 디코이 생성기
    /// 적을 유인하는 분신 소환.
    /// Lv.5 달성 시 분신 파괴될 때 데이터 감옥(속박) 발동.
    /// </summary>
    public class HologramDecoyGenerator : MonoBehaviour
    {
        [Header("Stats")]
        public GameObject decoyPrefab; // HologramDecoy 스크립트가 붙은 프리팹
        
        public float hp = 50f;
        public float cooldown = 15f; 
        
        // 데이터 감옥 지속 시간 (Lv.5 이상)
        public float prisonDuration = 3f;

        private float timer;
        private int currentLevel = 1;

        // Base Stats
        private float baseHp;
        private float baseCooldown;

        private void Start()
        {
            baseHp = hp;
            baseCooldown = cooldown;
            
            // 시작 시 즉시 쿨타임 완료 상태로 시작? 
            timer = cooldown; 
        }

        private void Update()
        {
            timer += Time.deltaTime;
            if (timer >= cooldown)
            {
                SpawnDecoy();
                timer = 0f;
            }
        }

        private void SpawnDecoy()
        {
            if (decoyPrefab == null) return;

            // 플레이어 위치에 소환
            GameObject obj = Instantiate(decoyPrefab, transform.position, Quaternion.identity);
            
            if (obj.TryGetComponent<HologramDecoy>(out var decoy))
            {
                bool spawnPrison = (currentLevel >= 5);
                decoy.Initialize(hp, spawnPrison, prisonDuration);
            }
        }

        public void OnLevelUp(int level)
        {
            currentLevel = level;
            if (baseHp == 0 && hp > 0) baseHp = hp;
            if (baseCooldown == 0 && cooldown > 0) baseCooldown = cooldown;
            
            // 레벨업: 체력 20% 증가, 쿨타임 10% 감소
            hp = baseHp * (1f + (level - 1) * 0.2f);
            
            // 쿨타임 감소는 점감법 적용 (10%씩 계속 까면 0됨. 복리 or 단순 합?)
            // 기획: "쿨감". 단순하게 10%씩 감소로. (최대 50% 제한 등 두면 좋음).
            // 여기선 base * (1 - 0.05 * level) 정도로 완만하게.
            cooldown = baseCooldown * (1f - (level - 1) * 0.05f); // 렙당 5% 감소
            if (cooldown < 1f) cooldown = 1f;

            Debug.Log($"[Hologram Decoy] Lv.{level} : HP {hp}, Cooldown {cooldown}, Prison: {level >= 5}");
        }
    }
}
