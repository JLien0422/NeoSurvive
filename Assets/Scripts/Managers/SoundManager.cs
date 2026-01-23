using UnityEngine;
using System.Collections; // Coroutines를 사용하기 위해 필요합니다.

// SoundManager 클래스는 게임의 사운드(배경음악, 효과음 등)를 관리합니다.
// 이 컴포넌트는 씬에 하나의 오브젝트만 존재해야 합니다.
[RequireComponent(typeof(AudioSource))]
public class SoundManager : MonoBehaviour
{
    // 싱글톤 인스턴스: 다른 스크립트에서 SoundManager에 쉽게 접근할 수 있도록 합니다.
    public static SoundManager Instance { get; private set; }

    // 인스펙터에서 할당할 배경음악(BGM) 클립들의 배열입니다.
    [SerializeField]
    private AudioClip[] bgmClips;

    // BGM을 재생할 AudioSource 컴포넌트에 대한 참조입니다.
    private AudioSource audioSource;
    // 현재 재생 중인 BGM 클립의 인덱스입니다.
    private int currentBgmIndex = 0;

    // 컴포넌트가 처음 활성화될 때 호출됩니다.
    private void Awake()
    {
        // 싱글톤 패턴 구현
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

        // AudioSource 컴포넌트를 가져옵니다.
        audioSource = GetComponent<AudioSource>();
        audioSource.spatialBlend = 0; // BGM은 2D 사운드입니다.
        audioSource.playOnAwake = false; // 스크립트에서 제어합니다.
        audioSource.loop = false; // 다음 곡으로 넘어가야 하므로 루프는 끕니다.
    }

    // 게임이 시작될 때 한 번 호출됩니다.
    private void Start()
    {
        // 시작할 때 SettingsManager로부터 현재 볼륨 값을 가져와 적용합니다.
        UpdateVolume();

        if (bgmClips != null && bgmClips.Length > 0)
        {
            PlayNextBgm();
        }
        else
        {
            Debug.LogWarning("SoundManager에 BGM 클립이 할당되지 않았습니다.");
        }
    }

    // 매 프레임마다 호출됩니다.
    private void Update()
    {
        if (!audioSource.isPlaying && bgmClips != null && bgmClips.Length > 0)
        {
            PlayNextBgm();
        }
    }

    // 다음 BGM을 재생하는 메서드입니다.
    private void PlayNextBgm()
    {
        if (currentBgmIndex >= bgmClips.Length)
        {
            currentBgmIndex = 0;
        }

        audioSource.clip = bgmClips[currentBgmIndex];
        audioSource.Play();

        Debug.Log($"BGM 재생 시작: {audioSource.clip.name} (인덱스: {currentBgmIndex})");

        currentBgmIndex++;
    }

    // SettingsManager에서 호출할 볼륨 업데이트 메서드
    public void UpdateVolume()
    {
        if (SettingsManager.Instance != null)
        {
            audioSource.volume = SettingsManager.Instance.bgmVolume;
            Debug.Log($"BGM 볼륨이 {audioSource.volume}으로 설정되었습니다.");
        }
    }
}
