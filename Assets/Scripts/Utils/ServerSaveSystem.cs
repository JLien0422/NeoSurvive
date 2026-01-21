using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using NeoSurvive.Network;
using Newtonsoft.Json;

/// <summary>
/// ES3 API와 동일한 인터페이스를 제공하면서 내부적으로 HTTP 서버에 저장/로드하는 시스템
/// ES3.Save/Load와 동일한 매개변수를 사용하되, 서버 기반 저장소를 활용
/// </summary>
public static class ServerSaveSystem
{
  private const string SAVE_ENDPOINT = "/save";
  private const string LOAD_ENDPOINT = "/load";

  /// <summary>
  /// ES3.Save와 동일한 시그니처 - 서버에 데이터 저장
  /// </summary>
  /// <typeparam name="T">저장할 데이터 타입</typeparam>
  /// <param name="key">저장 키 (ES3와 동일)</param>
  /// <param name="value">저장할 값</param>
  public static void Save<T>(string key, T value)
  {
    SaveAsync(key, value).ConfigureAwait(false);
  }

  /// <summary>
  /// ES3.Save 비동기 버전 - await 가능
  /// </summary>
  public static async Task SaveAsync<T>(string key, T value)
  {
    try
    {
      // GameServerAPI Instance null 체크
      if (NeoSurvive.Network.GameServerAPI.Instance == null)
      {
        Debug.LogError($"[ServerSaveSystem] GameServerAPI.Instance is null. Cannot save key '{key}'");
        return;
      }

      // ES3 직렬화를 사용하여 JSON으로 변환
      string jsonData = ES3SerializationHelper.SerializeToJson(value);

      // 서버에 저장할 데이터 패키징
      var saveData = new ServerSaveData
      {
        deviceUID = NeoSurvive.Network.GameServerAPI.Instance.DeviceUID,
        key = key,
        data = jsonData,
        dataType = typeof(T).FullName,
        timestamp = DateTime.UtcNow.ToString("o")
      };

      // HTTP POST로 서버에 저장
      string requestJson = JsonConvert.SerializeObject(saveData);
      Debug.Log($"[ServerSaveSystem] Saving to server - DeviceUID: {saveData.deviceUID}, Key: {key}, URL: {SAVE_ENDPOINT}");
      Debug.Log($"[ServerSaveSystem] Request JSON: {requestJson}");
      UnityWebRequest request = NeoSurvive.Network.GameServerAPI.Instance.CreatePostRequest(SAVE_ENDPOINT, requestJson);
      request.timeout = 10; // 10초 타임아웃 설정

      var operation = request.SendWebRequest();
      float startTime = Time.realtimeSinceStartup;
      float timeout = 10f;

      while (!operation.isDone)
      {
        if (Time.realtimeSinceStartup - startTime > timeout)
        {
          Debug.LogError($"[ServerSaveSystem] Save timeout for key '{key}'");
          request.Abort();
          break;
        }
        await Task.Yield();
      }

      if (request.result == UnityWebRequest.Result.Success)
      {
        Debug.Log($"[ServerSaveSystem] Saved '{key}' to server successfully");
        Debug.Log($"[ServerSaveSystem] Response: {request.downloadHandler.text}");
      }
      else
      {
        Debug.LogError($"[ServerSaveSystem] Save failed for key '{key}': {request.error}");
        Debug.LogError($"[ServerSaveSystem] Response Code: {request.responseCode}");
        Debug.LogError($"[ServerSaveSystem] Response: {request.downloadHandler?.text}");
      }

      request.Dispose();
    }
    catch (Exception ex)
    {
      Debug.LogError($"[ServerSaveSystem] Save exception for key '{key}': {ex.Message}");
    }
  }

  /// <summary>
  /// ES3.Load와 동일한 시그니처 - 서버에서 데이터 로드
  /// </summary>
  /// <typeparam name="T">로드할 데이터 타입</typeparam>
  /// <param name="key">로드 키 (ES3와 동일)</param>
  /// <param name="defaultValue">기본값 (서버에 데이터가 없을 때 반환)</param>
  /// <returns>로드된 값 또는 기본값</returns>
  public static T Load<T>(string key, T defaultValue = default)
  {
    // 동기 호출을 위해 Task를 블로킹
    // 주의: 메인 스레드 블로킹을 피하려면 LoadAsync 사용 권장
    try
    {
      return LoadAsync(key, defaultValue).GetAwaiter().GetResult();
    }
    catch (Exception ex)
    {
      Debug.LogError($"[ServerSaveSystem] Load failed for key '{key}': {ex.Message}");
      return defaultValue;
    }
  }

  /// <summary>
  /// ES3.Load 비동기 버전 - await 가능
  /// </summary>
  public static async Task<T> LoadAsync<T>(string key, T defaultValue = default)
  {
    try
    {
      // GameServerAPI Instance null 체크
      if (NeoSurvive.Network.GameServerAPI.Instance == null)
      {
        Debug.LogError($"[ServerSaveSystem] GameServerAPI.Instance is null. Cannot load key '{key}'");
        return defaultValue;
      }

      // 서버에서 데이터 요청
      var loadRequest = new ServerLoadRequest
      {
        deviceUID = NeoSurvive.Network.GameServerAPI.Instance.DeviceUID,
        key = key,
        dataType = typeof(T).FullName
      };

      string requestJson = JsonConvert.SerializeObject(loadRequest);
      UnityWebRequest request = NeoSurvive.Network.GameServerAPI.Instance.CreatePostRequest(LOAD_ENDPOINT, requestJson);
      request.timeout = 10; // 10초 타임아웃 설정

      var operation = request.SendWebRequest();
      float startTime = Time.realtimeSinceStartup;
      float timeout = 10f;

      while (!operation.isDone)
      {
        if (Time.realtimeSinceStartup - startTime > timeout)
        {
          Debug.LogError($"[ServerSaveSystem] Load timeout for key '{key}'");
          request.Abort();
          break;
        }
        await Task.Yield();
      }

      if (request.result == UnityWebRequest.Result.Success)
      {
        string responseText = request.downloadHandler.text;
        var saveData = JsonConvert.DeserializeObject<ServerSaveData>(responseText);

        if (saveData != null && !string.IsNullOrEmpty(saveData.data))
        {
          T loadedValue = ES3SerializationHelper.DeserializeFromJson<T>(saveData.data);
          Debug.Log($"[ServerSaveSystem] Loaded '{key}' from server successfully");
          request.Dispose();
          return loadedValue;
        }
      }

      Debug.LogWarning($"[ServerSaveSystem] No data found on server for key '{key}', using default or local value");
      request.Dispose();

      return defaultValue;
    }
    catch (Exception ex)
    {
      Debug.LogError($"[ServerSaveSystem] LoadAsync exception for key '{key}': {ex.Message}");
      return defaultValue;
    }
  }

  /// <summary>
  /// ES3.KeyExists와 동일 - 서버에 키가 존재하는지 확인
  /// </summary>
  public static bool KeyExists(string key)
  {
    return KeyExistsAsync(key).GetAwaiter().GetResult();
  }

  /// <summary>
  /// 서버에 키가 존재하는지 비동기 확인
  /// </summary>
  public static async Task<bool> KeyExistsAsync(string key)
  {
    try
    {
      // GameServerAPI Instance null 체크
      if (NeoSurvive.Network.GameServerAPI.Instance == null)
      {
        Debug.LogError($"[ServerSaveSystem] GameServerAPI.Instance is null. Cannot check key '{key}'");
        return false;
      }

      var checkRequest = new ServerLoadRequest
      {
        deviceUID = NeoSurvive.Network.GameServerAPI.Instance.DeviceUID,
        key = key
      };
      string requestJson = JsonConvert.SerializeObject(checkRequest);
      UnityWebRequest request = NeoSurvive.Network.GameServerAPI.Instance.CreatePostRequest(LOAD_ENDPOINT, requestJson);
      request.timeout = 10; // 10초 타임아웃 설정

      var operation = request.SendWebRequest();
      float startTime = Time.realtimeSinceStartup;
      float timeout = 10f;

      while (!operation.isDone)
      {
        if (Time.realtimeSinceStartup - startTime > timeout)
        {
          Debug.LogError($"[ServerSaveSystem] KeyExists timeout for key '{key}'");
          request.Abort();
          break;
        }
        await Task.Yield();
      }

      bool exists = request.result == UnityWebRequest.Result.Success && !string.IsNullOrEmpty(request.downloadHandler.text);
      request.Dispose();
      return exists;
    }
    catch (Exception ex)
    {
      Debug.LogError($"[ServerSaveSystem] KeyExistsAsync exception for key '{key}': {ex.Message}");
      return false;
    }
  }

  /// <summary>
  /// ES3.DeleteKey와 동일 - 서버에서 키 삭제
  /// </summary>
  public static void DeleteKey(string key)
  {
    DeleteKeyAsync(key).ConfigureAwait(false);
  }

  /// <summary>
  /// 서버에서 키를 비동기 삭제
  /// </summary>
  public static async Task DeleteKeyAsync(string key)
  {
    try
    {
      // GameServerAPI Instance null 체크
      if (NeoSurvive.Network.GameServerAPI.Instance == null)
      {
        Debug.LogError($"[ServerSaveSystem] GameServerAPI.Instance is null. Cannot delete key '{key}'");
        return;
      }

      var deleteRequest = new ServerDeleteRequest
      {
        deviceUID = NeoSurvive.Network.GameServerAPI.Instance.DeviceUID,
        key = key
      };
      string requestJson = JsonConvert.SerializeObject(deleteRequest);
      UnityWebRequest request = NeoSurvive.Network.GameServerAPI.Instance.CreatePostRequest("/delete", requestJson);
      request.timeout = 10; // 10초 타임아웃 설정

      var operation = request.SendWebRequest();
      float startTime = Time.realtimeSinceStartup;
      float timeout = 10f;

      while (!operation.isDone)
      {
        if (Time.realtimeSinceStartup - startTime > timeout)
        {
          Debug.LogError($"[ServerSaveSystem] Delete timeout for key '{key}'");
          request.Abort();
          break;
        }
        await Task.Yield();
      }

      request.Dispose();
    }
    catch (Exception ex)
    {
      Debug.LogError($"[ServerSaveSystem] DeleteKeyAsync exception for key '{key}': {ex.Message}");
    }
  }
}

/// <summary>
/// 서버 저장 데이터 구조
/// </summary>
[Serializable]
public class ServerSaveData
{
  public string deviceUID;    // 디바이스 고유 ID
  public string key;          // 저장 키
  public string data;         // ES3로 직렬화된 JSON 데이터
  public string dataType;     // 데이터 타입 (역직렬화용)
  public string timestamp;    // 저장 시간
}

/// <summary>
/// 서버 로드 요청 구조
/// </summary>
[Serializable]
public class ServerLoadRequest
{
  public string deviceUID;    // 디바이스 고유 ID
  public string key;          // 로드할 키
  public string dataType;     // 기대하는 데이터 타입
}

/// <summary>
/// 서버 삭제 요청 구조
/// </summary>
[Serializable]
public class ServerDeleteRequest
{
  public string deviceUID;    // 디바이스 고유 ID
  public string key;          // 삭제할 키
}
