using UnityEngine;

[DefaultExecutionOrder(10000)]
public class AudioVolumeAwakeProbe : MonoBehaviour
{
    private void Awake()
    {
        var sound = SoundManager.Instance;
        AudioStartupProbe.Check(sound != null && Mathf.Approximately(sound.GetComponent<AudioSource>().volume, ES3.Bgm), "BGM source volume applied before Start");
    }
}
