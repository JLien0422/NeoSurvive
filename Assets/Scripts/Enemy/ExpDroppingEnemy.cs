using UnityEngine;
using NeoSurvive.Exp;

namespace NeoSurvive.Enemy
{
    /// <summary>
    /// 경험치 오브를 드랍하는 적 (EnemyBase는 유지하고 상속으로 확장)
    /// </summary>
    public class ExpDroppingEnemy : EnemyBase
    {
        [Header("EXP Drop")]
        [SerializeField] private ExpOrb expOrbPrefab; // ExpOrb 프리팹 연결
        [SerializeField] private int expAmount = 1;   // 오브 1개가 주는 경험치
        [SerializeField] private int dropCount = 1;   // 죽을 때 몇 개 드랍
        [SerializeField] private float scatterRadius = 0.5f; // 퍼짐 반경

        protected override void OnDeath()
        {
            Debug.Log($"[ExpDroppingEnemy] OnDeath() 호출됨: {gameObject.name}");

            if (expOrbPrefab == null)
            {
                Debug.LogError($"[ExpDroppingEnemy] expOrbPrefab이 NULL 입니다! Inspector에서 프리팹 연결 필요");
                return;
            }

            for (int i = 0; i < dropCount; i++)
            {
                Vector2 offset = Random.insideUnitCircle * scatterRadius;
                Vector3 spawnPos = transform.position + (Vector3)offset;

                ExpOrb orb = Instantiate(expOrbPrefab, spawnPos, Quaternion.identity);
                orb.SetAmount(expAmount);

                Debug.Log($"[ExpDroppingEnemy] ExpOrb 생성: {orb.name} at {spawnPos}");
            }
        }

    }
}
