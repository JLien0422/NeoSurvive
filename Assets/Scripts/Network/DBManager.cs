using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

namespace NeoSurvive.Network
{
  /// <summary>
  /// DB 및 웹 API 통신 관리자 (HTTP 전담)
  /// 인증, 사용자 정보, 랭크, 레벨업 등의 DB 관련 작업을 담당합니다.
  /// </summary>
  public class DBManager : MonoBehaviour
  {
    public static DBManager Instance { get; private set; }

    [Header("서버 설정")]
    [SerializeField] private string serverUrl = "http://nasdac.kro.kr:5157/api";

    private int playerId = -1;
    private string deviceUID;
    private string nickname;

    public int PlayerId => playerId;
    public string DeviceUID => deviceUID;
    public string Nickname => nickname;

    public event Action<LoginResponse> OnLoginSuccess;
    public event Action<string> OnLoginFailed;

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
        return;
      }

      deviceUID = GetOrCreateDeviceUID();
    }

    private void Start()
    {
      StartCoroutine(Login());
    }

    private string GetOrCreateDeviceUID()
    {
      string uid = PlayerPrefs.GetString("DeviceUID", "");
      if (string.IsNullOrEmpty(uid))
      {
        uid = Guid.NewGuid().ToString();
        PlayerPrefs.SetString("DeviceUID", uid);
        PlayerPrefs.Save();
      }

      // [ParrelSync 지원] 클론 클라이언트인 경우 UID를 구분하여 중복 로그인 방지
      if (Application.dataPath.Contains("_clone"))
      {
        uid += "_clone";
      }

      return uid;
    }

    public IEnumerator Login()
    {
      var loginData = new { deviceUID = deviceUID };
      string json = JsonConvert.SerializeObject(loginData);

      using (UnityWebRequest request = CreatePostRequest("/auth/login", json))
      {
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
          var response = JsonConvert.DeserializeObject<LoginResponse>(request.downloadHandler.text);
          playerId = response.playerId;
          nickname = response.nickname;
          Debug.Log($"✅ [DB] 로그인 성공! PlayerId: {playerId}, Nickname: {nickname}");
          OnLoginSuccess?.Invoke(response);
        }
        else
        {
          Debug.LogError($"❌ [DB] 로그인 실패: {request.error}");
          OnLoginFailed?.Invoke(request.error);
        }
      }
    }

    public UnityWebRequest CreatePostRequest(string endpoint, string json)
    {
      UnityWebRequest request = new UnityWebRequest(serverUrl + endpoint, "POST");
      byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
      request.uploadHandler = new UploadHandlerRaw(bodyRaw);
      request.downloadHandler = new DownloadHandlerBuffer();
      request.SetRequestHeader("Content-Type", "application/json");
      return request;
    }

    public UnityWebRequest CreateGetRequest(string endpoint)
    {
      UnityWebRequest request = UnityWebRequest.Get(serverUrl + endpoint);
      return request;
    }
  }
}
