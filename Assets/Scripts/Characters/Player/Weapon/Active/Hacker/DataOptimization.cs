using System.Collections;
using UnityEngine;
using NeoSurvive.Core;

namespace NeoSurvive.Weapon
{
    public class DataOptimization : MonoBehaviour
    {
        public GameObject fieldPrefab;

        [Header("VFX")]
        public GameObject startVfxPrefab;
        public GameObject middleVfxPrefab;
        public GameObject endVfxPrefab;

        public float startVfxDuration = 0.5f;
        public float middleMinDuration = 0.5f; // ★ 추가: Middle 최소 표시 시간
        public float endVfxDuration = 0.5f;
        public float vfxScale = 1.0f;

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

            Vector3 spawnPos = transform.position;

            if (InGameSoundManager.Instance != null)
            {
                InGameSoundManager.Instance.PlayDataOptimizationSpawn();
            }

            GameObject obj = Instantiate(fieldPrefab, spawnPos, Quaternion.identity);

            if (obj.TryGetComponent<DataField>(out var field))
            {
                Vector2 fieldSize = new Vector2(fieldWidth, fieldHeight);

                field.Initialize(
                    fieldSize,
                    damageBuffMultiplier,
                    buffDuration,
                    currentLevel >= 5,
                    masterSlowMultiplier,
                    masterSlowDuration,
                    srcWeapon: null
                );

                Destroy(obj, fieldDuration);
            }

            StartCoroutine(FieldVFXRoutine(spawnPos));
        }

        // ★ 수정: Start → Middle → End VFX 순차 실행
        private IEnumerator FieldVFXRoutine(Vector3 spawnPos)
        {
            if (startVfxPrefab != null)
            {
                GameObject startVfx = Instantiate(startVfxPrefab, spawnPos, Quaternion.identity);
                startVfx.transform.localScale = Vector3.one * vfxScale;
                Destroy(startVfx, startVfxDuration);
            }

            yield return new WaitForSeconds(startVfxDuration);

            GameObject middleVfx = null;

            if (middleVfxPrefab != null)
            {
                middleVfx = Instantiate(middleVfxPrefab, spawnPos, Quaternion.identity);
                middleVfx.transform.localScale = Vector3.one * vfxScale;
            }

            // ★ 수정: fieldDuration이 짧아도 Middle이 최소 시간은 보이도록 처리
            float middleDuration = Mathf.Max(middleMinDuration, fieldDuration - startVfxDuration);

            yield return new WaitForSeconds(middleDuration);

            if (middleVfx != null)
                Destroy(middleVfx);

            if (endVfxPrefab != null)
            {
                GameObject endVfx = Instantiate(endVfxPrefab, spawnPos, Quaternion.identity);
                endVfx.transform.localScale = Vector3.one * vfxScale;
                Destroy(endVfx, endVfxDuration);
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

            masterSlowMultiplier = row.masterslowmul;
            masterSlowDuration = row.masterslowduration;

            if (row.firerate > 0f)
                fireRate = row.firerate;

            Debug.Log($"[DataOptimization] Lv={level}, fireRate={fireRate}");
        }
    }
}