using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-10000)]
public class AudioStartupProbe : MonoBehaviour
{
    public static void Check(bool passed, string message)
    {
        string line = $"{(passed ? "PASS" : "FAIL")} [{ES3.Scenario}/{SceneManager.GetActiveScene().name}] {message}";
        File.AppendAllText(Path.Combine(Application.dataPath, "../results.txt"), line + "\n");
        if (!passed) SessionState.SetInt("AudioValidationFailures", SessionState.GetInt("AudioValidationFailures", 0) + 1);
    }

    private void Awake()
    {
        Check(SettingsManager.Instance != null, "Settings exists before scene Awake callbacks");
        Check(Mathf.Approximately(AudioListener.volume, ES3.Master), "Master volume applied before scene Awake callbacks");
    }

    private IEnumerator Start()
    {
        yield return null;
        var sound = SoundManager.Instance;
        var source = sound.GetComponent<AudioSource>();
        Check(source.clip != null, "First BGM selected without opening settings");
        Check(Mathf.Approximately(source.volume, ES3.Bgm), "First BGM uses saved volume");
        Check(SettingsManager.Instance != null && Mathf.Approximately(SettingsManager.Instance.sfxVolume, ES3.Sfx), "Saved SFX volume available before settings UI");

        if (SceneManager.GetActiveScene().name.Contains("Lobby"))
        {
            var ui = GameObject.Find("Canvas").transform.Find("SettingsTab").gameObject;
            Check(!ui.activeInHierarchy, "Settings UI remains closed during startup checks");
            ui.SetActive(true);
            if (SettingsManager.Instance == null) new GameObject("SettingsManager").AddComponent<SettingsManager>();
            yield return null;
            Check(Mathf.Approximately(AudioListener.volume, ES3.Master) && Mathf.Approximately(source.volume, ES3.Bgm), "Opening settings preserves/applies saved volumes");
            ui.SetActive(false);
            SceneManager.LoadScene("MainAudioValidation");
            yield break;
        }

        var settings = SettingsManager.Instance;
        Check(Object.FindObjectsOfType<SettingsManager>().Length == 1, "One settings manager after scene transition");
        Check(Mathf.Approximately(AudioListener.volume, ES3.Master) && Mathf.Approximately(source.volume, ES3.Bgm), "Volumes preserved across scene transition");
        settings.SetBgmVolume(0.41f);
        Check(Mathf.Approximately(source.volume, 0.41f), "BGM slider updates playback immediately");
        settings.SetBgmVolume(ES3.Bgm);
        settings.SetMasterVolume(0.29f);
        Check(Mathf.Approximately(AudioListener.volume, 0.29f), "Master slider updates playback immediately");
        settings.SetMasterVolume(ES3.Master);

        settings.SetMuteOnFocusLost(true);
        settings.SendMessage("OnApplicationFocus", false);
        Check(Mathf.Approximately(AudioListener.volume, 0f), "Background mute still works");
        settings.SendMessage("OnApplicationFocus", true);
        Check(Mathf.Approximately(AudioListener.volume, ES3.Master), "Focus return restores saved master volume");
        settings.SetMuteOnFocusLost(false);

        Object.Destroy(settings.gameObject);
        yield return null;
        Check(SettingsManager.Instance == null, "Destroyed settings manager clears singleton");
        new GameObject("SettingsManager").AddComponent<SettingsManager>();
        Check(SettingsManager.Instance != null && Mathf.Approximately(AudioListener.volume, ES3.Master), "Recreated manager loads saved audio values");
        EditorApplication.isPlaying = false;
    }
}
