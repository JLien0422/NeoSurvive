using UnityEngine;
using System.Collections;

namespace NeoSurvive.Weapon
{
    public class HologramDecoyGenerator : MonoBehaviour
    {
        [Header("Stats")]
        public GameObject decoyPrefab;

        public float hp = 50f;
        public float cooldown = 15f;

        public float prisonDuration = 3f;
        public float prisonRange = 3f;

        [Header("Base VFX")]
        public GameObject baseStartVfxPrefab;
        public GameObject baseEndVfxPrefab;

        [Header("Field VFX")]
        public GameObject fieldStartVfxPrefab;
        public GameObject fieldMiddleVfxPrefab;
        public GameObject fieldEndVfxPrefab;

        [Header("Body VFX")]
        public GameObject bodyVfxPrefab;

        [Header("VFX Timing")]
        public float baseStartDuration = 0.35f;
        public float fieldStartDuration = 0.35f;
        public float fieldMiddleDuration = 0.7f;

        public float fieldEndLifetime = 0.4f;
        public float baseEndLifetime = 0.4f;

        [Header("VFX Scale")]
        public float vfxScale = 1f;
        public float bodyVfxScale = 1f;

        private float timer;
        private int currentLevel = 1;
        private bool spawning = false;

        private readonly string weaponId = "hologramdecoy";

        private void Start()
        {
            ApplyStatsFromCSV(1);
            timer = cooldown;
        }

        private void Update()
        {
            timer += Time.deltaTime;

            if (timer >= cooldown && !spawning)
            {
                StartCoroutine(SpawnDecoyRoutine());
                timer = 0f;
            }
        }

        private IEnumerator SpawnDecoyRoutine()
        {
            spawning = true;

            Vector3 spawnPos = transform.position;

            SpawnOneShotVFX(baseStartVfxPrefab, spawnPos, baseStartDuration, vfxScale);
            yield return new WaitForSeconds(baseStartDuration);

            SpawnOneShotVFX(fieldStartVfxPrefab, spawnPos, fieldStartDuration, vfxScale);
            yield return new WaitForSeconds(fieldStartDuration);

            SpawnOneShotVFX(fieldMiddleVfxPrefab, spawnPos, fieldMiddleDuration, vfxScale);
            yield return new WaitForSeconds(fieldMiddleDuration);

            SpawnDecoy(spawnPos);

            spawning = false;
        }

        private void SpawnDecoy(Vector3 spawnPos)
        {
            if (decoyPrefab == null) return;

            GameObject obj =
                Instantiate(decoyPrefab, spawnPos, Quaternion.identity);

            if (obj.TryGetComponent<HologramDecoy>(out var decoy))
            {
                bool spawnPrison = currentLevel >= 5;

                decoy.Initialize(
                    hp,
                    spawnPrison,
                    prisonDuration,
                    prisonRange,
                    bodyVfxPrefab,
                    fieldEndVfxPrefab,
                    baseEndVfxPrefab,
                    fieldEndLifetime,
                    baseEndLifetime,
                    vfxScale,
                    bodyVfxScale
                );
            }
        }

        private void SpawnOneShotVFX(
            GameObject prefab,
            Vector3 pos,
            float lifetime,
            float scale)
        {
            if (prefab == null) return;

            GameObject vfx = Instantiate(prefab, pos, Quaternion.identity);
            vfx.transform.localScale = Vector3.one * scale;

            Destroy(vfx, lifetime);
        }

        public void OnLevelUp(int level)
        {
            currentLevel = Mathf.Clamp(level, 1, 5);
            ApplyStatsFromCSV(currentLevel);

            Debug.Log(
                $"[Hologram Decoy] CSV 적용 | Lv={currentLevel}, HP={hp}, Cooldown={cooldown}, " +
                $"PrisonDuration={prisonDuration}, PrisonRange={prisonRange}, Prison={currentLevel >= 5}"
            );
        }

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

            hp = row.hp;
            cooldown = row.cooldown;
            if (cooldown < 1f)
                cooldown = 1f;

            prisonDuration = row.prisonduration;
            prisonRange = row.prisonrange;

            Debug.Log(
                $"[Hologram Decoy] CSV 적용 | Lv={level}, HP={hp}, Cooldown={cooldown}, " +
                $"PrisonDuration={prisonDuration}, PrisonRange={prisonRange}"
            );
        }
    }
}