using UnityEngine;
using System.IO;

public static class JsonDataUtils
{
    public static void Save<T>(T data, string fileName)
    {
        string path = Path.Combine(Application.persistentDataPath, fileName);
        try
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(path, json);
            Debug.Log($"Successfully saved data to {path}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save data to {path}. Error: {e.Message}");
        }
    }

    public static T Load<T>(string fileName) where T : class
    {
        string path = Path.Combine(Application.persistentDataPath, fileName);
        if (File.Exists(path))
        {
            try
            {
                string json = File.ReadAllText(path);
                T data = JsonUtility.FromJson<T>(json);
                return data;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load data from {path}. Error: {e.Message}");
                return null;
            }
        }
        else
        {
            Debug.Log($"No save file found at {path}.");
            return null;
        }
    }
}
