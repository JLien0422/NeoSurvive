using UnityEngine;
using NeoSurvive.Core;

namespace NeoSurvive.Weapon
{
    public class DataOptimization : MonoBehaviour
    {
        public GameObject fieldPrefab;

        public float fieldDuration;
        public float fieldWidth;
        public float fieldHeight;

        public float damageBuffMultiplier;
        public float buffDuration;

        public float masterSlowMultiplier;
        public float masterSlowDuration;

        public float fireRate = 5f;

        private float timer;
        private int currentLevel = 1;

        private readonly string weaponId = "dataoptimization";

        private void Start()
        {
            ApplyStatsFromCSV(1);
        }

        private void Update()
        {
            // 🔥 [추가] firerate 0 방어 (CSV 누락 대비)
            if (fireRate <= 0f) return;

            timer += Time.deltaTime;

            if (timer >= fireRate)
            {
                timer = 0f;
                SpawnField();
            }
        }

        private void SpawnField()
        {
            if (fieldPrefab == null) return;

            GameObject obj = Instantiate(fieldPrefab, transform.position, Quaternion.identity);

            if (obj.TryGetComponent<DataField>(out var field))
            {
                Vector2 fieldSize = new Vector2(fieldWidth, fieldHeight); // 🔥 [추가] 필드 크기 계산

                field.Initialize(
                    fieldSize,                     // 🔥 [수정] fieldSize 전달하도록 변경
                    damageBuffMultiplier,
                    buffDuration,
                    currentLevel >= 5,
                    masterSlowMultiplier,
                    masterSlowDuration,
                    srcWeapon: null
                );

                // ❌ [삭제] 기존 transform.localScale 방식 제거
                // obj.transform.localScale = new Vector3(fieldWidth, fieldHeight, 1f);

                Destroy(obj, fieldDuration);
            }
        }

        public void OnLevelUp(int level)
        {
            currentLevel = Mathf.Clamp(level, 1, 5);
            ApplyStatsFromCSV(currentLevel);
        }

        private void ApplyStatsFromCSV(int level)
        {
            if (!WeaponStatLoader.DB.rows.TryGetValue(weaponId, out var dict)) return;
            if (!dict.TryGetValue(level, out var row)) return;

            fieldDuration = row.fieldduration;
            fieldWidth = row.fieldwidth;
            fieldHeight = row.fieldheight;

            damageBuffMultiplier = row.damagebuffmultiplier;
            buffDuration = row.buffduration;

            masterSlowMultiplier = row.masterslowmul;       // 🔥 [수정] slowmultiplier → masterslowmul
            masterSlowDuration = row.masterslowduration;    // 🔥 [수정] slowduration → masterslowduration

            // 🔥 [추가] firerate 0 방어
            if (row.firerate > 0f)
                fireRate = row.firerate;

            Debug.Log($"[DataOptimization] Lv={level}, fireRate={fireRate}");
        }
    }
}

// CSV안에 firerate를 넣어둠 (값은 6으로 통일)