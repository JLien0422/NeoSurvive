using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeoSurvive.Utils
{
  /// <summary>
  /// ES3를 활용한 직렬화/역직렬화 헬퍼 클래스
  /// 네트워크 전송을 위한 ES3 기능을 간단하게 사용할 수 있도록 래핑합니다.
  /// </summary>
  public static class ES3SerializationHelper
  {
    // ES3 설정 (메모리 스트림 사용, JSON 포맷)
    private static ES3Settings GetNetworkSettings()
    {
      return new ES3Settings(ES3.Location.InternalMS)
      {
        format = ES3.Format.JSON,
        prettyPrint = false, // 네트워크 전송용이므로 압축된 형태
        encryptionType = ES3.EncryptionType.None // 서버에서 처리
      };
    }

    #region 직렬화 (Serialization)

    /// <summary>
    /// 객체를 JSON 문자열로 직렬화합니다.
    /// </summary>
    public static string SerializeToJson<T>(T obj)
    {
      try
      {
        var settings = GetNetworkSettings();
        byte[] bytes = ES3.Serialize(obj, settings);
        return System.Text.Encoding.UTF8.GetString(bytes);
      }
      catch (Exception e)
      {
        Debug.LogError($"[ES3Helper] JSON 직렬화 실패: {e.Message}");
        return null;
      }
    }

    /// <summary>
    /// 객체를 바이트 배열로 직렬화합니다.
    /// </summary>
    public static byte[] SerializeToBytes<T>(T obj)
    {
      try
      {
        var settings = GetNetworkSettings();
        return ES3.Serialize<T>(obj, settings);
      }
      catch (Exception e)
      {
        Debug.LogError($"[ES3Helper] 바이트 직렬화 실패: {e.Message}");
        return null;
      }
    }

    /// <summary>
    /// 객체를 압축된 바이트 배열로 직렬화합니다. (대역폭 절약)
    /// </summary>
    public static byte[] SerializeToCompressedBytes<T>(T obj)
    {
      try
      {
        byte[] bytes = SerializeToBytes(obj);
        if (bytes == null) return null;

        return ES3.CompressBytes(bytes);
      }
      catch (Exception e)
      {
        Debug.LogError($"[ES3Helper] 압축 직렬화 실패: {e.Message}");
        return null;
      }
    }

    #endregion

    #region 역직렬화 (Deserialization)

    /// <summary>
    /// JSON 문자열을 객체로 역직렬화합니다.
    /// </summary>
    public static T DeserializeFromJson<T>(string json)
    {
      try
      {
        if (string.IsNullOrEmpty(json))
        {
          Debug.LogWarning("[ES3Helper] 빈 JSON 문자열입니다.");
          return default(T);
        }

        var settings = GetNetworkSettings();
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(json);
        return ES3.Deserialize<T>(bytes, settings);
      }
      catch (Exception e)
      {
        Debug.LogError($"[ES3Helper] JSON 역직렬화 실패: {e.Message}\nJSON: {json}");
        return default(T);
      }
    }

    /// <summary>
    /// 바이트 배열을 객체로 역직렬화합니다.
    /// </summary>
    public static T DeserializeFromBytes<T>(byte[] bytes)
    {
      try
      {
        if (bytes == null || bytes.Length == 0)
        {
          Debug.LogWarning("[ES3Helper] 빈 바이트 배열입니다.");
          return default(T);
        }

        var settings = GetNetworkSettings();
        return ES3.Deserialize<T>(bytes, settings);
      }
      catch (Exception e)
      {
        Debug.LogError($"[ES3Helper] 바이트 역직렬화 실패: {e.Message}");
        return default(T);
      }
    }

    /// <summary>
    /// 압축된 바이트 배열을 객체로 역직렬화합니다.
    /// </summary>
    public static T DeserializeFromCompressedBytes<T>(byte[] compressedBytes)
    {
      try
      {
        if (compressedBytes == null || compressedBytes.Length == 0)
        {
          Debug.LogWarning("[ES3Helper] 빈 압축 바이트 배열입니다.");
          return default(T);
        }

        byte[] decompressedBytes = ES3.DecompressBytes(compressedBytes);
        return DeserializeFromBytes<T>(decompressedBytes);
      }
      catch (Exception e)
      {
        Debug.LogError($"[ES3Helper] 압축 해제 역직렬화 실패: {e.Message}");
        return default(T);
      }
    }

    #endregion

    #region 리스트 직렬화 (List Serialization)

    /// <summary>
    /// 리스트를 JSON 문자열로 직렬화합니다.
    /// </summary>
    public static string SerializeList<T>(List<T> list)
    {
      try
      {
        var settings = GetNetworkSettings();
        byte[] bytes = ES3.Serialize<List<T>>(list, settings);
        return System.Text.Encoding.UTF8.GetString(bytes);
      }
      catch (Exception e)
      {
        Debug.LogError($"[ES3Helper] 리스트 JSON 직렬화 실패: {e.Message}");
        return null;
      }
    }

    /// <summary>
    /// JSON 문자열을 리스트로 역직렬화합니다.
    /// </summary>
    public static List<T> DeserializeList<T>(string json)
    {
      try
      {
        if (string.IsNullOrEmpty(json))
        {
          Debug.LogWarning("[ES3Helper] 빈 JSON 문자열입니다.");
          return new List<T>();
        }

        var settings = GetNetworkSettings();
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(json);
        return ES3.Deserialize<List<T>>(bytes, settings) ?? new List<T>();
      }
      catch (Exception e)
      {
        Debug.LogError($"[ES3Helper] 리스트 JSON 역직렬화 실패: {e.Message}");
        return new List<T>();
      }
    }

    #endregion

    #region 유틸리티 (Utilities)

    /// <summary>
    /// 객체가 직렬화 가능한지 검증합니다.
    /// </summary>
    public static bool CanSerialize<T>(T obj)
    {
      if (obj == null)
        return false;

      try
      {
        SerializeToJson(obj);
        return true;
      }
      catch
      {
        return false;
      }
    }

    /// <summary>
    /// 직렬화된 데이터의 크기를 반환합니다. (바이트)
    /// </summary>
    public static int GetSerializedSize<T>(T obj)
    {
      byte[] bytes = SerializeToBytes(obj);
      return bytes?.Length ?? 0;
    }

    /// <summary>
    /// 압축률을 계산합니다. (0.0 ~ 1.0, 낮을수록 압축률이 높음)
    /// </summary>
    public static float CalculateCompressionRatio<T>(T obj)
    {
      byte[] original = SerializeToBytes(obj);
      byte[] compressed = SerializeToCompressedBytes(obj);

      if (original == null || compressed == null || original.Length == 0)
        return 1.0f;

      return (float)compressed.Length / original.Length;
    }

    #endregion
  }
}
