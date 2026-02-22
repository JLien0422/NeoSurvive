using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 넘버 시퀀스 미니게임
/// 1~9 숫자를 키패드 레이아웃에서 순서대로 클릭합니다.
/// </summary>
public class NumberSequenceMinigame : HackingMinigameBase
{
    [Header("UI 설정")]
    [SerializeField]
    private GameObject buttonPrefab; // 숫자 버튼 프리팹 (선택사항)

    [SerializeField]
    private Transform buttonParent; // 버튼들이 배치될 부모 (없으면 자동 생성)

    private List<GameObject> numberButtons = new List<GameObject>();
    private int currentTargetNumber = 1; // 현재 찾아야 할 숫자 (1~9)
    private const int MAX_NUMBER = 9;

    private float timeLimit = 15f; // 제한 시간 (초)
    private float remainingTime = 0f;
    private bool isActive = false;

    private Transform preMadeBtnGrid;   // 손으로 만든 BtnGrid
    private Slider timeGauge;           // 시간 게이지 (빨강)
    private bool usePreMadeUI = false;  // 손으로 만든 UI 사용 여부

    /// <summary>
    /// 버튼 부모 설정 (HackingSystem에서 호출)
    /// </summary>
    public void SetButtonParent(Transform parent)
    {
        buttonParent = parent;
        usePreMadeUI = false;
        Debug.Log($"[NumberSequenceMinigame] SetButtonParent 호출: {parent != null}");
    }

    /// <summary>
    /// 손으로 만든 UI 사용 (HackingSystem에서 호출)
    /// </summary>
    public void SetPreMadeUI(Transform btnGrid, Slider timeSlider)
    {
        preMadeBtnGrid = btnGrid;
        timeGauge = timeSlider;
        usePreMadeUI = true;
        buttonParent = btnGrid;
        Debug.Log("[NumberSequenceMinigame] 손으로 만든 UI 사용");
    }

    /// <summary>
    /// 버튼 부모 가져오기 (디버그용)
    /// </summary>
    public Transform GetButtonParent()
    {
        return buttonParent;
    }

    public override void Initialize(System.Action onSuccessCallback, System.Action onFailureCallback)
    {
        base.Initialize(onSuccessCallback, onFailureCallback);
        
        Debug.Log($"[NumberSequenceMinigame] Initialize 시작, usePreMadeUI: " + usePreMadeUI);
        
        if (usePreMadeUI && preMadeBtnGrid != null)
        {
            // 손으로 만든 버튼 9개 사용
            UsePreMadeButtons();
        }
        else
        {
            // UI 부모가 없으면 자동 생성
            if (buttonParent == null)
            {
                Debug.Log("[NumberSequenceMinigame] buttonParent가 null이므로 UI 부모 생성");
                CreateUIParent();
            }
            CreateNumberButtons();
        }
        ResetMinigame();
    }

    /// <summary>
    /// 손으로 만든 버튼 9개를 사용 (BtnGrid의 자식 Button들)
    /// 버튼 텍스트는 1~9로 이미 설정되어 있다고 가정. 위치만 랜덤으로 섞음.
    /// </summary>
    private void UsePreMadeButtons()
    {
        numberButtons.Clear();
        var buttons = preMadeBtnGrid.GetComponentsInChildren<Button>(true);
        if (buttons == null || buttons.Length < 9)
        {
            Debug.LogError("[NumberSequenceMinigame] BtnGrid에 버튼이 9개 이상 없습니다. 자동 생성으로 대체합니다.");
            CreateNumberButtons();
            return;
        }

        for (int i = 0; i < 9; i++)
        {
            Button btn = buttons[i];
            GameObject btnObj = btn.gameObject;
            int num = GetNumberFromButton(btnObj);
            if (num < 1 || num > 9) num = i + 1; // 파싱 실패 시 인덱스+1 사용

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnNumberClicked(num));
            btnObj.name = "NumberButton_" + num;
            numberButtons.Add(btnObj);
        }

        // 버튼 위치만 랜덤으로 섞기 (자식 순서 섞기 → Grid Layout에서 위치 변경)
        ShuffleSiblingOrder(preMadeBtnGrid);

        Debug.Log("[NumberSequenceMinigame] 손으로 만든 버튼 9개 적용 완료 (텍스트 유지, 위치만 셔플)");
    }

    /// <summary>
    /// 버튼 텍스트에서 숫자 파싱 (1~9)
    /// </summary>
    private int GetNumberFromButton(GameObject btnObj)
    {
        var tmp = btnObj.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp != null && int.TryParse(tmp.text.Trim(), out int n)) return n;
        var legacyText = btnObj.GetComponentInChildren<UnityEngine.UI.Text>(true);
        if (legacyText != null && int.TryParse(legacyText.text.Trim(), out n)) return n;
        return -1;
    }

    /// <summary>
    /// 자식 순서를 랜덤으로 섞기 (Grid Layout에서 위치가 바뀜)
    /// </summary>
    private void ShuffleSiblingOrder(Transform parent)
    {
        var children = new List<Transform>();
        for (int i = 0; i < parent.childCount; i++)
            children.Add(parent.GetChild(i));

        for (int i = children.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Transform temp = children[i];
            children[i] = children[j];
            children[j] = temp;
        }

        foreach (var child in children)
            child.SetAsLastSibling();
    }

    /// <summary>
    /// UI 부모 오브젝트 자동 생성
    /// </summary>
    private void CreateUIParent()
    {
        // Canvas 찾기 또는 생성
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("HackingCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }

        // 해킹 UI 패널 생성 (정사각형 팝업창)
        GameObject panel = new GameObject("HackingPanel");
        panel.transform.SetParent(canvas.transform, false);
        
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(400, 400); // 정사각형
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero; // 화면 중앙

        UnityEngine.UI.Image panelImg = panel.AddComponent<UnityEngine.UI.Image>();
        panelImg.color = new Color(0.2f, 0.2f, 0.2f, 0.95f); // 조금 더 밝게, 더 불투명하게

        // Canvas의 Sort Order를 높여서 다른 UI 위에 표시
        if (canvas != null)
        {
            canvas.sortingOrder = 100; // 높은 값으로 설정
        }

        buttonParent = panel.transform;
        
        Debug.Log($"[NumberSequenceMinigame] UI 부모 생성 완료: {panel.name}");
    }

    /// <summary>
    /// 숫자 버튼들 생성 (키패드 레이아웃)
    /// </summary>
    private void CreateNumberButtons()
    {
        if (buttonParent == null)
        {
            Debug.LogError("[NumberSequenceMinigame] buttonParent가 null입니다!");
            return;
        }

        // 키패드 레이아웃: 3x3 그리드
        // 7 8 9
        // 4 5 6
        // 1 2 3

        int[] numberOrder = { 7, 8, 9, 4, 5, 6, 1, 2, 3 };
        float buttonSize = 80f;
        float spacing = 10f;
        
        // 패널 중앙 기준으로 버튼 배치
        float totalWidth = (buttonSize * 3) + (spacing * 2);
        float totalHeight = (buttonSize * 3) + (spacing * 2);
        float startX = -totalWidth / 2f + buttonSize / 2f;
        float startY = totalHeight / 2f - buttonSize / 2f;

        for (int i = 0; i < numberOrder.Length; i++)
        {
            int number = numberOrder[i];
            int row = i / 3;
            int col = i % 3;

            // 버튼 생성
            GameObject buttonObj = new GameObject($"NumberButton_{number}");
            buttonObj.transform.SetParent(buttonParent, false);

            RectTransform rect = buttonObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(buttonSize, buttonSize);
            rect.anchoredPosition = new Vector2(
                startX + col * (buttonSize + spacing),
                startY - row * (buttonSize + spacing)
            );

            // 버튼 컴포넌트
            Image img = buttonObj.AddComponent<Image>();
            img.color = new Color(0.3f, 0.3f, 0.3f, 1f);

            Button button = buttonObj.AddComponent<Button>();
            int num = number; // 클로저를 위한 로컬 변수
            button.onClick.AddListener(() => OnNumberClicked(num));

            // 텍스트 (Unity 기본 Text 사용 - TextMeshPro 폰트 문제 방지)
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;

            UnityEngine.UI.Text text = textObj.AddComponent<UnityEngine.UI.Text>();
            text.text = number.ToString();
            text.fontSize = 36;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            numberButtons.Add(buttonObj);
        }

        Debug.Log($"[NumberSequenceMinigame] {numberButtons.Count}개의 숫자 버튼 생성 완료");

        // 숫자 순서 섞기
        ShuffleButtonPositions();
    }

    /// <summary>
    /// 버튼 위치를 섞어서 랜덤 배치
    /// </summary>
    private void ShuffleButtonPositions()
    {
        List<Vector2> positions = new List<Vector2>();
        foreach (var btn in numberButtons)
        {
            positions.Add(btn.GetComponent<RectTransform>().anchoredPosition);
        }

        // Fisher-Yates 셔플
        for (int i = positions.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Vector2 temp = positions[i];
            positions[i] = positions[j];
            positions[j] = temp;
        }

        // 섞인 위치 적용
        for (int i = 0; i < numberButtons.Count; i++)
        {
            numberButtons[i].GetComponent<RectTransform>().anchoredPosition = positions[i];
        }
    }

    /// <summary>
    /// 미니게임 리셋
    /// </summary>
    private void ResetMinigame()
    {
        currentTargetNumber = 1;
        remainingTime = timeLimit;
        isActive = true;

        // 모든 버튼 활성화
        foreach (var btn in numberButtons)
        {
            btn.SetActive(true);
            UpdateButtonState(btn, false);
        }

        UpdateTimeGauge(); // 게이지 초기화
    }

    /// <summary>
    /// 숫자 클릭 처리
    /// </summary>
    private void OnNumberClicked(int number)
    {
        if (!isActive) return;

        // 올바른 순서인지 확인
        if (number == currentTargetNumber)
        {
            // 성공
            GameObject clickedButton = numberButtons.Find(b => b.name.Contains($"_{number}"));
            if (clickedButton != null)
            {
                UpdateButtonState(clickedButton, true);
                clickedButton.SetActive(false); // 클릭된 버튼 비활성화
            }

            currentTargetNumber++;

            // 모든 숫자를 다 찾았으면 성공
            if (currentTargetNumber > MAX_NUMBER)
            {
                OnSuccess();
                return;
            }
        }
        else
        {
            // 실패 (잘못된 순서)
            OnFailure();
        }
    }

    /// <summary>
    /// 버튼 상태 업데이트 (색상 변경 등)
    /// </summary>
    private void UpdateButtonState(GameObject button, bool isCorrect)
    {
        Image img = button.GetComponent<Image>();
        if (img != null)
        {
            img.color = isCorrect ? Color.green : new Color(0.3f, 0.3f, 0.3f, 1f);
        }
    }

    /// <summary>
    /// 시간 게이지 업데이트
    /// </summary>
    private void UpdateTimeGauge()
    {
        if (timeGauge != null)
        {
            timeGauge.value = remainingTime / timeLimit; // 1 → 0으로 감소
        }
    }

    private void Update()
    {
        if (!isActive) return;

        // 시간 제한 체크
        remainingTime -= Time.unscaledDeltaTime;
        UpdateTimeGauge(); // 게이지 업데이트
        
        if (remainingTime <= 0f)
        {
            OnFailure();
        }
    }

    public override void OnSuccess()
    {
        isActive = false;
        if (timeGauge != null)
        {
            timeGauge.value = 1f; // 성공 시 게이지 가득 참
        }
        base.OnSuccess();
    }

    public override void OnFailure()
    {
        isActive = false;
        if (timeGauge != null)
        {
            timeGauge.value = 0f; // 실패 시 게이지 비움
        }
        base.OnFailure();
    }
}
