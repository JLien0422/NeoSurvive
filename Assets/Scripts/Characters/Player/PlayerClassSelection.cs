using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerClassSelection : MonoBehaviour
{
    private const string KEY = "NeoSurvive_SelectedPlayerClass";

    public static void Save(PlayerClassType type)
    {
        PlayerPrefs.SetInt(KEY, (int)type);
        PlayerPrefs.Save();
        Debug.Log($"[Lobby] Saved PlayerClassType = {type}");
    }

    public static PlayerClassType Load()
    {
        return (PlayerClassType)PlayerPrefs.GetInt(KEY, (int)PlayerClassType.Cyborg);
    }
}
