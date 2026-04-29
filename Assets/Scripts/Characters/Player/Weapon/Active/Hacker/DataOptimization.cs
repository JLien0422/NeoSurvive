using UnityEngine;
using NeoSurvive.Core;

namespace NeoSurvive.Weapon
{
    /// <summary>
    /// 해커 무기: 데이터 최적화
    /// - 주기적으로 필드를 생성
    /// - 필드를 통과한 플레이어 투사체는 피해 증가 버프 획득
    /// - Lv5(마스터)일 때만 필드에 닿은 적에게 슬로우
    /// - 수치는 CSV에서 로드
    /// </summary>
    public class DataOptimization : MonoBehaviour
    {
        [Header("Field Prefab")]
        [SerializeField] private GameObject dataFieldPrefab;

        [Header("Runtime Stats (CSV 적용값)")]
        [SerializeField] private float fireRate = 6f;
        [SerializeField] private float fieldDuration = 3f;
        [SerializeField] private Vector2 fieldSize = new Vector2(20f, 1f);
        [SerializeField] private float damageBuffMultiplier = 1.2f;
        [SerializeField] private float buffDuration = 5f;
        [SerializeField] private float masterSlowMultiplier = 0.5f;
        [SerializeField] private float masterSlowDuration = 3f;

        [Header("Placement")]
        [SerializeField] private float forwardOffset = 0f;

        [Header("Debug")]
        [SerializeField] private bool debugLog = false;

        private const string weaponId = "dataoptimization";

        private Player owner;
        private int level = 1;
        private float timer = 0f;

        private void Awake()
        {
            owner = GetComponentInParent<Player>();
            if (owner == null)
                owner = GetComponent<Player>();

            ApplyStatsFromCSV(level);
        }

        private void Update()
        {
            timer += Time.deltaTime;

            float actualFireRate = GetActualFireRate();
            if (timer >= actualFireRate)
            {
                timer = 0f;
                SpawnField();
            }
        }

        private float GetActualFireRate()
        {
            if (owner == null) return fireRate;
            return owner.ApplyWeaponFireRate(fireRate);
        }

        private void SpawnField()
        {
            if (dataFieldPrefab == null)
            {
                if (debugLog)
                    Debug.LogWarning("[DataOptimization] dataFieldPrefab이 비어 있음");
                return;
            }

            Vector3 basePos = (owner != null) ? owner.transform.position : transform.position;
            Vector3 spawnPos = basePos + Vector3.right * forwardOffset;

            GameObject obj = Instantiate(dataFieldPrefab, spawnPos, Quaternion.identity);
            DataField field = obj.GetComponent<DataField>();

            if (field == null)
            {
                if (debugLog)
                    Debug.LogWarning("[DataOptimization] DataField 컴포넌트가 프리팹에 없음");
                return;
            }

            field.Initialize(
                owner,
                fieldDuration,
                fieldSize,
                damageBuffMultiplier,
                buffDuration,
                level >= 5,
                masterSlowMultiplier,
                masterSlowDuration,
                debugLog
            );

            if (debugLog)
            {
                Debug.Log($"[DataOptimization] 필드 생성 | Lv={level} | fireRate={fireRate} | size={fieldSize} | dmgBuff={damageBuffMultiplier}");
            }
        }

        public void OnLevelUp(int newLevel)
        {
            level = Mathf.Max(1, newLevel);
            ApplyStatsFromCSV(level);

            if (debugLog)
            {
                Debug.Log($"[DataOptimization] OnLevelUp | level={level}");
            }
        }

        private void ApplyStatsFromCSV(int targetLevel)
        {
            if (WeaponStatLoader.DB == null)
            {
                if (debugLog)
                    Debug.LogWarning("[DataOptimization] WeaponStatLoader.DB가 null이라 기본값 사용");
                return;
            }

            if (!WeaponStatLoader.DB.rows.TryGetValue(weaponId, out var levels))
            {
                if (debugLog)
                    Debug.LogWarning($"[DataOptimization] CSV에 weaponId={weaponId} 없음");
                return;
            }

            if (!levels.TryGetValue(targetLevel, out var row))
            {
                if (debugLog)
                    Debug.LogWarning($"[DataOptimization] CSV에 level={targetLevel} 행 없음");
                return;
            }

            if (row.firerate > 0f) fireRate = row.firerate;
            if (row.fieldduration > 0f) fieldDuration = row.fieldduration;

            if (row.fieldwidth > 0f || row.fieldheight > 0f)
            {
                float width = (row.fieldwidth > 0f) ? row.fieldwidth : fieldSize.x;
                float height = (row.fieldheight > 0f) ? row.fieldheight : fieldSize.y;
                fieldSize = new Vector2(width, height);
            }

            if (row.damagebuffmultiplier > 0f) damageBuffMultiplier = row.damagebuffmultiplier;
            if (row.buffduration > 0f) buffDuration = row.buffduration;

            if (targetLevel >= 5)
            {
                if (row.masterslowmul > 0f) masterSlowMultiplier = row.masterslowmul;
                if (row.masterslowduration > 0f) masterSlowDuration = row.masterslowduration;
            }

            if (debugLog)
            {
                Debug.Log($"[DataOptimization] CSV 적용 | Lv={targetLevel} | fireRate={fireRate} | fieldDuration={fieldDuration} | fieldSize={fieldSize} | buffMul={damageBuffMultiplier}");
            }
        }
    }
}