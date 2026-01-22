using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NeoSurvive.Network;

namespace NeoSurvive.Network
{
    /// <summary>
    /// 실시간 게임 동기화 관리자 (UDP 전담 예정)
    /// </summary>
    public class CoopManager : MonoBehaviour
    {
        public static CoopManager Instance { get; private set; }

        public event Action<EnemyState> OnEnemySpawned;
        public event Action<int> OnEnemyDied;
        public event Action<DropItemState> OnItemDropped;
        public event Action<int, int> OnItemCollected;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        #region 실시간 데이터 송신 (UDP로 변경 예정)

        public void SendPlayerPosition(Vector2 pos, Vector2 vel, float rot)
        {
            // TODO: UDPClient를 통한 전송 로직
            // 지금은 NetworkManager의 WebSocket을 임시로 사용할 수 있으나 구조상 분리
        }

        public void SendEnemySpawn(EnemyState enemy)
        {
            // TODO: UDP 전송
        }

        #endregion

        #region 데이터 수신 핸들러 (NetworkManager로부터 전달받음)

        public void HandleEnemySpawn(EnemySpawnMessage msg)
        {
            OnEnemySpawned?.Invoke(msg.enemy);
        }

        public void HandleEnemyUpdate(EnemyUpdateMessage msg)
        {
            // 적 위치 업데이트 로직
        }

        public void HandleEnemyDeath(EnemyDeathMessage msg)
        {
            OnEnemyDied?.Invoke(msg.enemyId);
        }

        public void HandleItemDrop(ItemDropMessage msg)
        {
            OnItemDropped?.Invoke(msg.item);
        }

        public void HandleItemCollect(ItemCollectMessage msg)
        {
            OnItemCollected?.Invoke(msg.itemId, msg.collectorPlayerId);
        }

        #endregion
    }
}
