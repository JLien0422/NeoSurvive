using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class AudioStartupValidation
{
    private static readonly string[] Scenarios = { "saved", "zero", "defaults" };
    static AudioStartupValidation()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        EditorApplication.update += Watchdog;
    }

    public static void Run()
    {
        SessionState.SetBool("AudioValidationActive", true);
        SessionState.SetInt("AudioValidationFailures", 0);
        SessionState.SetInt("AudioValidationScenario", 0);
        File.WriteAllText(Path.Combine(Application.dataPath, "../results.txt"), "");
        AssetDatabase.Refresh();
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/tone.wav");
        CreateScene("LobbyAudioValidation", clip, false);
        CreateScene("MainAudioValidation", clip, true);
        EditorBuildSettings.scenes = new[] {
            new EditorBuildSettingsScene("Assets/LobbyAudioValidation.unity", true),
            new EditorBuildSettingsScene("Assets/MainAudioValidation.unity", true)
        };
        BeginScenario();
    }

    private static void CreateScene(string name, AudioClip clip, bool hasSettings)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("Early probe").AddComponent<AudioStartupProbe>();
        new GameObject("Late Awake probe").AddComponent<AudioVolumeAwakeProbe>();
        if (hasSettings) new GameObject("SettingsManager").AddComponent<SettingsManager>();
        else
        {
            var canvas = new GameObject("Canvas");
            var ui = new GameObject("SettingsTab");
            ui.transform.SetParent(canvas.transform);
            ui.SetActive(false);
            var sound = new GameObject("Sound Manager").AddComponent<SoundManager>();
            var serialized = new SerializedObject(sound);
            serialized.FindProperty("autoLoadEventDefinitionsFromAssets").boolValue = false;
            serialized.FindProperty("autoLoadWeaponProfilesFromAssets").boolValue = false;
            foreach (var field in new[] { "lobbyBgmClips", "inGameBgmClips" })
            {
                var clips = serialized.FindProperty(field);
                clips.arraySize = 1;
                clips.GetArrayElementAtIndex(0).objectReferenceValue = clip;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        EditorSceneManager.SaveScene(scene, "Assets/" + name + ".unity");
    }

    private static void BeginScenario()
    {
        int index = SessionState.GetInt("AudioValidationScenario", 0);
        File.WriteAllText(Path.Combine(Application.dataPath, "../scenario.txt"), Scenarios[index]);
        EditorSceneManager.OpenScene("Assets/LobbyAudioValidation.unity");
        SessionState.SetFloat("AudioValidationStarted", (float)EditorApplication.timeSinceStartup);
        EditorApplication.isPlaying = true;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool("AudioValidationActive", false)) return;
        int next = SessionState.GetInt("AudioValidationScenario", 0) + 1;
        SessionState.SetInt("AudioValidationScenario", next);
        if (next < Scenarios.Length) EditorApplication.delayCall += BeginScenario;
        else
        {
            int failures = SessionState.GetInt("AudioValidationFailures", 0);
            File.AppendAllText(Path.Combine(Application.dataPath, "../results.txt"), $"TOTAL FAILURES: {failures}\n");
            SessionState.SetBool("AudioValidationActive", false);
            EditorApplication.Exit(failures == 0 ? 0 : 1);
        }
    }

    private static void Watchdog()
    {
        if (SessionState.GetBool("AudioValidationActive", false) && EditorApplication.timeSinceStartup - SessionState.GetFloat("AudioValidationStarted", (float)EditorApplication.timeSinceStartup) > 60)
        {
            File.AppendAllText(Path.Combine(Application.dataPath, "../results.txt"), "TIMEOUT\n");
            EditorApplication.Exit(2);
        }
    }
}
