// Only copied into the isolated validation project. No real save files are read or written.
using System.IO;
using UnityEngine;

public static class ES3
{
    public static string Scenario => File.ReadAllText(Path.Combine(Application.dataPath, "../scenario.txt")).Trim();
    public static float Master => Scenario == "zero" ? 0f : Scenario == "defaults" ? 0.8f : 0.37f;
    public static float Bgm => Scenario == "zero" ? 0f : Scenario == "defaults" ? 1f : 0.23f;
    public static float Sfx => Scenario == "zero" ? 0f : Scenario == "defaults" ? 1f : 0.17f;

    public static T Load<T>(string key, T fallback)
    {
        // Skip resolution changes in the batch Editor; exercise the original audio path.
        if (key == "Settings_ResolutionIndex") return (T)(object)int.MaxValue;
        if (Scenario == "defaults") return fallback;
        if (key == "Settings_MasterVolume") return (T)(object)Master;
        if (key == "Settings_BgmVolume") return (T)(object)Bgm;
        if (key == "Settings_SfxVolume") return (T)(object)Sfx;
        return fallback;
    }
    public static void Save<T>(string key, T value) => throw new System.InvalidOperationException("Validation must not write saves.");
}

public class GameManager : MonoBehaviour { public static GameManager Instance; }
namespace NeoSurvive.Utils
{
    public class EffectOpacityController : MonoBehaviour { public void ApplyOpacity() {} }
}
namespace NeoSurvive.Weapon
{
    public class WeaponBase : MonoBehaviour { public int weaponId; }
}
