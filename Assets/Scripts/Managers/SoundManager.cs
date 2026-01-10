using UnityEngine;
using System.Collections; // Coroutines를 사용하기 위해 필요합니다.

// SoundManager 클래스는 게임의 사운드(배경음악, 효과음 등)를 관리합니다.
// 이 컴포넌트는 씬에 하나의 오브젝트만 존재해야 합니다 (싱글톤 패턴 고려 가능).
[RequireComponent(typeof(AudioSource))] // 이 스크립트가 붙으려면 AudioSource 컴포넌트가 필요합니다.
public class SoundManager : MonoBehaviour
{
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
        // AudioSource 컴포넌트를 가져옵니다.
        audioSource = GetComponent<AudioSource>();
        // BGM은 보통 2D 사운드입니다. (공간감 없음)
        audioSource.spatialBlend = 0;
        // 게임이 시작될 때 자동으로 재생되지 않도록 합니다. (스크립트에서 제어)
        audioSource.playOnAwake = false;
        // BGM은 루프되지 않아야 다음 곡으로 넘어갈 수 있습니다.
        audioSource.loop = false;
    }

    // 게임이 시작될 때 한 번 호출됩니다.
    private void Start()
    {
        // BGM 클립이 하나 이상 할당되었는지 확인합니다.
        if (bgmClips != null && bgmClips.Length > 0)
        {
            // 첫 번째 BGM을 재생합니다.
            PlayNextBgm();
        }
        else
        {
            Debug.LogWarning("SoundManager에 BGM 클립이 할당되지 않았습니다. 인스펙터에서 BGM 클립을 할당해주세요.");
        }
    }

    // 매 프레임마다 호출됩니다.
    private void Update()
    {
        // AudioSource가 현재 아무것도 재생하고 있지 않고, 클립이 할당되어 있다면
        if (!audioSource.isPlaying && bgmClips != null && bgmClips.Length > 0)
        {
            // 다음 BGM을 재생합니다.
            PlayNextBgm();
        }
    }

    // 다음 BGM을 재생하는 메서드입니다.
    private void PlayNextBgm()
    {
        // 만약 인덱스가 배열의 길이를 벗어나면, 처음(0)으로 돌아갑니다.
        if (currentBgmIndex >= bgmClips.Length)
        {
            currentBgmIndex = 0;
        }

        // 현재 인덱스의 클립을 audioSource에 할당합니다.
        audioSource.clip = bgmClips[currentBgmIndex];
        // 클립을 재생합니다.
        audioSource.Play();

        Debug.Log($"BGM 재생 시작: {audioSource.clip.name} (인덱스: {currentBgmIndex})");

        // 다음 재생을 위해 인덱스를 증가시킵니다.
        currentBgmIndex++;
    }
}
