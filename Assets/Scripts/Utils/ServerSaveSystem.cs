using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 기존 서버 저장 API 시그니처를 유지하면서 로컬 ES3 저장을 수행합니다.
/// </summary>
public static class ServerSaveSystem
{
  public static void Save<T>(string key, T value)
  {
    ES3.Save(key, value);
  }

  public static Task SaveAsync<T>(string key, T value)
  {
    ES3.Save(key, value);
    return Task.CompletedTask;
  }

  public static T Load<T>(string key, T defaultValue = default)
  {
    return ES3.Load(key, defaultValue);
  }

  public static Task<T> LoadAsync<T>(string key, T defaultValue = default)
  {
    T value = ES3.Load(key, defaultValue);
    return Task.FromResult(value);
  }

  public static bool KeyExists(string key)
  {
    return ES3.KeyExists(key);
  }

  public static Task<bool> KeyExistsAsync(string key)
  {
    return Task.FromResult(ES3.KeyExists(key));
  }

  public static void DeleteKey(string key)
  {
    if (ES3.KeyExists(key))
    {
      ES3.DeleteKey(key);
    }
  }

  public static Task DeleteKeyAsync(string key)
  {
    if (ES3.KeyExists(key))
    {
      ES3.DeleteKey(key);
    }

    return Task.CompletedTask;
  }
}
