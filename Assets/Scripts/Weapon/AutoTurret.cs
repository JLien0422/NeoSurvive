using UnityEngine;
using NeoSurvive.Core;

namespace NeoSurvive.Weapon
{
    /// <summary>
    /// 설치형 자동 포탑 생성기 (Spawn Weapon)
    /// </summary>
    public class AutoTurret : WeaponBase
    {
        public GameObject projectilePrefab;
        public float lifeTime = 10f; // 터렛 지속 시간
        public float turretFireRate = 1f; // 터렛 공격 속도

        private void Start()
        {
            // 이 컴포넌트는 플레이어에게 장착된 무기(생성기)이므로 스스로 파괴되지 않습니다.
        }

        public override void Attack()
        {
            // 터렛 본체 생성
            GameObject turretObj = new GameObject("DeployedTurret");
            turretObj.transform.position = transform.position; // 플레이어 위치에 생성
            
            // DeployedTurret 컴포넌트 추가 및 설정
            DeployedTurret turretScript = turretObj.AddComponent<DeployedTurret>();
            
            // 시각적 요소 설정을 위해 스프라이트 렌더러 추가 (DeployedTurret 내부 Init에서도 처리하지만, 프리팹이 없을때를 위해)
            // 여기서는 Initialize만 호출하면 DeployedTurret 내부에서 처리하도록 함.
            
            turretScript.Initialize(damage, range, turretFireRate, lifeTime, projectilePrefab);
            
            Debug.Log("포탑 설치 완료!");
        }
    }
}
