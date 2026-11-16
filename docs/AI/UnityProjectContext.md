# Unity 프로젝트 문맥

<!-- unity-onboarding:generated:start -->

2026-10-13, 기준 커밋 `108cc6394965fbfb78a8cb2e9110414fd21a3a62` 및 작업 중 변경사항을 확인했다. 이번 조사는 오디오와 설정 초기화에 한정한다.

- **확인됨:** Unity 2022.3.62f3. URP 14.0.12 및 실제 렌더 파이프라인 참조가 있다. 입력 설정은 Legacy Input Manager다.
- **확인됨:** 빌드 씬은 `Assets/Scenes/LobbyScene.unity`, `Assets/Scenes/MainScene.unity` 순서다.
- **확인됨:** 게임 스크립트는 `Assembly-CSharp`에 포함되며, `Assets/Scripts` 안에 별도 asmdef는 없다. 오디오는 `Assets/Scripts/Audio`, 설정 관리는 `Assets/Scripts/Managers`, 설정 UI는 `Assets/Scripts/UI`에 있다.
- **확인됨:** Easy Save 3가 설정 저장과 기본값 로드를 담당한다. `SettingsManager`는 마스터 음량을 `AudioListener.volume`에 적용한다. `SoundManager`는 BGM 소스 음량을 적용하고, 효과음 매니저들은 재생 시 `sfxVolume`을 읽는다.
- **확인됨:** 로비에는 시작 시 비활성인 `Canvas/SettingsTab`에 `SettingsUI`가 있으며, 별도 `SettingsManager`와 `ManagerInitializer`가 없다. MainScene에는 활성 `SettingsManager`가 있다. 설정 UI의 `Awake`에만 의존하면 로비 첫 오디오에 저장값이 적용되지 않는다.
- **수정한 흐름:** `SettingsManager.InitializeBeforeSceneLoad`가 첫 씬의 `Awake` 전에 지속 매니저를 생성하고 저장값을 적용한다. 기존 씬 매니저는 중복을 제거한다. `SoundManager`는 소스 준비 직후와 BGM 재생 전에 음량을 적용한다.
- **확인됨:** 현재 Enter Play Mode Options는 꺼져 있어 기본 도메인/씬 재로드를 사용한다. 재로드를 끈 조합은 별도 검증하지 않았다.
- **확인됨:** Unity Test Framework 1.1.33가 설치되어 있다. 조사 대상 게임 코드에 기존 테스트 어셈블리는 발견하지 못했다. `Tools/AudioStartupValidation`은 별도 Unity 프로젝트에서 실제 초기화/씬 전환을 검증하며, 저장 API와 무관한 게임 의존성을 대체한다.
- **확인됨:** CoplayDev Unity MCP 패키지는 설치되어 있다. 현재 채팅에 Unity 도구가 노출되어 있지 않아 기존 편집기의 상태 제어는 사용할 수 없다.
- **제약:** 현재 작업 폴더에는 사용자 수정이 많다. 이번 수정은 오디오/설정 스크립트와 검증 도구에 한정하며 씬, 프리팹, 저장 키, 패키지를 변경하지 않는다.
- **미확인:** 실제 로비/게임 씬의 청취 결과, 배포 빌드, 실제 저장 파일을 사용한 재실행. 독립 테스트는 이 항목을 대체하지 않는다.

근거: `ProjectSettings/ProjectVersion.txt`, `GraphicsSettings.asset`, `EditorSettings.asset`, `EditorBuildSettings.asset`, `ProjectSettings.asset`, `Packages/manifest.json`, 두 빌드 씬, `SettingsManager.cs`, `SettingsUI.cs`, `ManagerInitializer.cs`, `SoundManager.cs`, 각 효과음 매니저 및 스크립트 메타 파일.

<!-- unity-onboarding:generated:end -->
