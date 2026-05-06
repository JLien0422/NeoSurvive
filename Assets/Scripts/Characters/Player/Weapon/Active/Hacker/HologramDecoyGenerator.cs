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
        public GameObject decoyPrefab;

        public float hp = 50f;
        public float cooldown = 15f;
        public float prisonDuration = 3f;

        // ★ 추가: CSV prisonrange 적용용
        public float prisonRange = 3f;

        private float timer;
        private int currentLevel = 1;

        // ★ 추가: CSV weaponid
        private readonly string weaponId = "hologramdecoy";

        private void Start()
        {
            // ★ 수정: 시작 시 Lv1 CSV 적용
            ApplyStatsFromCSV(1);

            // 시작 시 즉시 쿨타임 완료 상태
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

            GameObject obj = Instantiate(decoyPrefab, transform.position, Quaternion.identity);

            if (obj.TryGetComponent<HologramDecoy>(out var decoy))
            {
                bool spawnPrison = (currentLevel >= 5);

                // ★ 수정: prisonRange까지 전달
                decoy.Initialize(hp, spawnPrison, prisonDuration, prisonRange);
            }
        }

        public void OnLevelUp(int level)
        {
            currentLevel = Mathf.Clamp(level, 1, 5);

            // ★ 수정: CSV 적용
            ApplyStatsFromCSV(currentLevel);

            Debug.Log($"[Hologram Decoy] CSV 적용 | Lv={currentLevel}, HP={hp}, Cooldown={cooldown}, PrisonDuration={prisonDuration}, PrisonRange={prisonRange}, Prison={currentLevel >= 5}");
        }

        // ★ 추가: CSV 적용 함수
        private void ApplyStatsFromCSV(int level)
        {
            if (WeaponStatLoader.DB == null)
            {
                Debug.LogWarning("[Hologram Decoy] WeaponStatLoader.DB 없음");
                return;
            }

            if (!WeaponStatLoader.DB.rows.TryGetValue(weaponId, out var levelDict))
            {
                Debug.LogWarning($"[Hologram Decoy] weaponId 없음: {weaponId}");
                return;
            }

            if (!levelDict.TryGetValue(level, out var row))
            {
                Debug.LogWarning($"[Hologram Decoy] level 데이터 없음: {level}");
                return;
            }

            WeaponStatDB.Row baseRow = row;
            if (levelDict.TryGetValue(1, out var levelOneRow))
            {
                baseRow = levelOneRow;
            }

            float hpPer = row.hpperlevel > 0f ? row.hpperlevel : baseRow.hpperlevel;
            float cooldownReduction = row.cooldownreductionperlevel > 0f
                ? row.cooldownreductionperlevel
                : baseRow.cooldownreductionperlevel;

            // hp = Lv1 hp 기준 + hpperlevel 증가
            hp = baseRow.hp * (1f + (level - 1) * hpPer);

            // cooldown = Lv1 cooldown 기준 - cooldownreductionperlevel 감소
            cooldown = baseRow.cooldown * (1f - (level - 1) * cooldownReduction);
            if (cooldown < 1f) cooldown = 1f;

            // 그대로 쓰는 값
            prisonDuration = row.prisonduration;
            prisonRange = row.prisonrange;

            Debug.Log($"[Hologram Decoy] CSV 적용 | Lv={level}, HP={hp}, Cooldown={cooldown}, PrisonDuration={prisonDuration}, PrisonRange={prisonRange}");
        }
    }
}