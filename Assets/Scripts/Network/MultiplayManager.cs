using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NeoSurvive.Network;
using NeoSurvive.Characters;
using System.Linq;
using System;

namespace NeoSurvive.Network
{
  /// <summary>
  /// 멀티플레이어 게임 씬에서의 플레이어 생성 및 초기 관리를 담당합니다.
  /// </summary>
  public class MultiplayManager : MonoBehaviour
  {
    public static MultiplayManager Instance { get; private set; }

    [Header("Prefabs")]
    [SerializeField] private GameObject playerPrefab;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;

    private void Awake()
    {
      if (Instance == null)
      {
        Instance = this;
      }
      else
      {
        Destroy(gameObject);
        return;
      }
    }

    private void Start()
    {
      SpawnPlayers();
    }

    private void SpawnPlayers()
    {
      if (MultiLobbyManager.Instance == null)
      {
        Debug.LogError("[MultiplayManager] MultiLobbyManager instance not found!");
        return;
      }

      var lobbyPlayers = MultiLobbyManager.Instance.GetLobbyPlayers();
      int localPlayerId = DBManager.Instance?.PlayerId ?? -1;

      Debug.Log($"[MultiplayManager] 플레이어 생성 시작. 총 인원: {lobbyPlayers.Length}");

      for (int i = 0; i < lobbyPlayers.Length; i++)
      {
        var pState = lobbyPlayers[i];
        Transform spawnPoint = (spawnPoints != null && i < spawnPoints.Length) ? spawnPoints[i] : transform;

        GameObject pObj = Instantiate(playerPrefab, spawnPoint.position, Quaternion.identity);
        pObj.name = $"Player_{pState.playerId}_{pState.nickname}";

        Player playerScript = pObj.GetComponent<Player>();
        if (playerScript != null)
        {
          // 캐릭터 타입에 따른 초기화
          if (Enum.TryParse(pState.characterType, out CharacterType charType))
          {
            playerScript.InitCharacter(charType);
          }
          else
          {
            Debug.LogWarning($"[MultiplayManager] 알 수 없는 캐릭터 타입: {pState.characterType}. 기본값으로 설정합니다.");
            playerScript.InitCharacter(CharacterType.Hacker);
          }
        }

        // 로컬 플레이어 여부에 따른 컨트롤러 처리
        PlayerController controller = pObj.GetComponent<PlayerController>();
        bool isLocal = (pState.playerId == localPlayerId);

        if (isLocal)
        {
          pObj.tag = "Player";
          Debug.Log($"[MultiplayManager] 로컬 플레이어 생성: {pState.nickname} (ID: {pState.playerId})");

          // 카메라 및 맵 매니저 대상 설정
          CameraController cam = FindObjectOfType<CameraController>();
          if (cam != null) cam.SetTarget(pObj.transform);

          var mapManager = FindObjectOfType<NeoSurvive.UI.Map.MapManager>();
          if (mapManager != null) mapManager.SetTarget(pObj.transform);
        }
        else
        {
          if (controller != null) controller.enabled = false;
          Debug.Log($"[MultiplayManager] 원격 플레이어 생성: {pState.nickname} (ID: {pState.playerId})");
        }

        // UDPClient에 플레이어 등록 (ID 기반 매핑)
        if (UDPClient.Instance != null && playerScript != null)
        {
          UDPClient.Instance.RegisterPlayer(pState.playerId, playerScript, isLocal);
        }
      }
    }
  }
}
