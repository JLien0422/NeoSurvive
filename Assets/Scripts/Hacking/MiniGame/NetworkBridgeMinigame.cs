using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

/// <summary>
/// 네트워크 브릿지 미니게임
/// 왼쪽의 색상 단자와 오른쪽의 동일 색상 단자를 선으로 연결합니다.
/// </summary>
public class NetworkBridgeMinigame : HackingMinigameBase
{
    [Header("UI 설정")]
    [SerializeField]
    private Transform gameParent; // 게임이 표시될 부모

    private Transform leftContainer;   // 왼쪽 노드 부모
    private Transform rightContainer;  // 오른쪽 노드 부모
    private Transform linesContainer;  // 연결선 부모

    private Slider timeGauge;    // 시간 게이지 (빨강, 1→0)
    private Slider successGauge; // 성공 연결 게이지 (녹색, 0→1)

    private List<ConnectionNode> leftNodes = new List<ConnectionNode>();
    private List<ConnectionNode> rightNodes = new List<ConnectionNode>();
    private List<ConnectionLine> connectionLines = new List<ConnectionLine>();

    private ConnectionNode selectedNode = null;
    private int totalConnections = 5; // 총 연결 개수
    private int completedConnections = 0;

    private float timeLimit = 15f;
    private float remainingTime = 0f;
    private bool isActive = false;

    // 색상 목록
    private Color[] nodeColors = {
        Color.red,
        Color.blue,
        Color.green,
        Color.yellow,
        Color.magenta,
        Color.cyan
    };

    /// <summary>
    /// 손으로 만든 UI 전달 (HackingSystem에서 호출)
    /// </summary>
    public void SetPreMadeUI(Transform left, Transform right, Transform lines,
                              Slider timeSlider, Slider successSlider)
    {
        leftContainer = left;
        rightContainer = right;
        linesContainer = lines;
        timeGauge = timeSlider;
        successGauge = successSlider;

        if (gameParent == null && left != null)
            gameParent = left.parent;
    }

    public override void Initialize(System.Action onSuccessCallback, System.Action onFailureCallback)
    {
        base.Initialize(onSuccessCallback, onFailureCallback);

        if (gameParent == null)
            CreateUIParent();

        ResetMinigame();
    }

    private void CreateUIParent()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("HackingCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }

        GameObject panel = new GameObject("NetworkBridgePanel");
        panel.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(600, 400);
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;

        UnityEngine.UI.Image panelImg = panel.AddComponent<UnityEngine.UI.Image>();
        panelImg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

        gameParent = panel.transform;
    }

    private void ResetMinigame()
    {
        completedConnections = 0;
        remainingTime = timeLimit;
        isActive = true;

        ClearAll();
        CreateNodes();
        UpdateTimeGauge();
        UpdateSuccessGauge();
    }

    private void CreateNodes()
    {
        // 왼쪽 단자 생성
        float nodeSize = 50f;
        float nodeSpacing = 60f;
        float startY = (totalConnections - 1) * nodeSpacing / 2f;

        List<Color> colors = new List<Color>();
        for (int i = 0; i < totalConnections; i++)
        {
            colors.Add(nodeColors[i % nodeColors.Length]);
        }

        // 색상 섞기
        for (int i = colors.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Color temp = colors[i];
            colors[i] = colors[j];
            colors[j] = temp;
        }

        // 왼쪽 노드 생성 (LeftContainer 또는 gameParent 사용)
        Transform leftParent = leftContainer != null ? leftContainer : gameParent;
        for (int i = 0; i < totalConnections; i++)
        {
            GameObject nodeObj = new GameObject($"LeftNode_{i}");
            nodeObj.transform.SetParent(leftParent, false);

            RectTransform rect = nodeObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(nodeSize, nodeSize);
            rect.anchoredPosition = new Vector2(0, startY - i * nodeSpacing);

            Image img = nodeObj.AddComponent<Image>();
            img.color = colors[i];

            nodeObj.AddComponent<Button>();
            ConnectionNode node = nodeObj.AddComponent<ConnectionNode>();
            node.Initialize(colors[i], true, i);
            node.onNodeClicked += OnNodeClicked;

            leftNodes.Add(node);
        }

        // 오른쪽 노드 생성 (RightContainer 또는 gameParent 사용)
        List<Color> rightColors = new List<Color>(colors);
        for (int i = rightColors.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Color temp = rightColors[i];
            rightColors[i] = rightColors[j];
            rightColors[j] = temp;
        }

        Transform rightParent = rightContainer != null ? rightContainer : gameParent;
        for (int i = 0; i < totalConnections; i++)
        {
            GameObject nodeObj = new GameObject($"RightNode_{i}");
            nodeObj.transform.SetParent(rightParent, false);

            RectTransform rect = nodeObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(nodeSize, nodeSize);
            rect.anchoredPosition = new Vector2(0, startY - i * nodeSpacing);

            Image img = nodeObj.AddComponent<Image>();
            img.color = rightColors[i];

            nodeObj.AddComponent<Button>();
            ConnectionNode node = nodeObj.AddComponent<ConnectionNode>();
            node.Initialize(rightColors[i], false, i);
            node.onNodeClicked += OnNodeClicked;

            rightNodes.Add(node);
        }
    }

    private void OnNodeClicked(ConnectionNode node)
    {
        if (!isActive) return;

        if (selectedNode == null)
        {
            // 첫 번째 노드 선택
            selectedNode = node;
            node.SetSelected(true);
        }
        else
        {
            // 두 번째 노드 선택
            if (selectedNode.isLeft != node.isLeft && selectedNode.nodeColor == node.nodeColor)
            {
                // 올바른 연결
                CreateConnection(selectedNode, node);
                selectedNode.SetConnected(true);
                node.SetConnected(true);
                selectedNode = null;
                completedConnections++;
                UpdateSuccessGauge();

                if (completedConnections >= totalConnections)
                {
                    OnSuccess();
                }
            }
            else
            {
                // 잘못된 연결
                selectedNode.SetSelected(false);
                selectedNode = null;
                OnFailure();
            }
        }
    }

    private void CreateConnection(ConnectionNode from, ConnectionNode to)
    {
        GameObject lineObj = new GameObject($"Line_{from.index}_{to.index}");
        Transform lineParent = linesContainer != null ? linesContainer : gameParent;
        lineObj.transform.SetParent(lineParent, false);

        RectTransform rect = lineObj.AddComponent<RectTransform>();
        // 라인 기준점을 중앙으로 고정 (Middle Center)
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        Image img = lineObj.AddComponent<Image>();
        img.color = from.nodeColor;

        ConnectionLine line = lineObj.AddComponent<ConnectionLine>();
        line.Initialize(from, to);
        connectionLines.Add(line);
    }

    private void ClearAll()
    {
        foreach (var node in leftNodes)
        {
            if (node != null) Destroy(node.gameObject);
        }
        foreach (var node in rightNodes)
        {
            if (node != null) Destroy(node.gameObject);
        }
        foreach (var line in connectionLines)
        {
            if (line != null) Destroy(line.gameObject);
        }

        leftNodes.Clear();
        rightNodes.Clear();
        connectionLines.Clear();
        selectedNode = null;
    }

    private void UpdateTimeGauge()
    {
        if (timeGauge != null)
            timeGauge.value = remainingTime / timeLimit; // 1→0
    }

    private void UpdateSuccessGauge()
    {
        if (successGauge != null)
            successGauge.value = (float)completedConnections / totalConnections; // 0→1
    }

    private void Update()
    {
        if (!isActive) return;

        remainingTime -= Time.unscaledDeltaTime;
        UpdateTimeGauge();

        if (remainingTime <= 0f)
        {
            OnFailure();
        }
    }

    public override void OnSuccess()
    {
        isActive = false;
        if (timeGauge != null) timeGauge.value = 1f;
        if (successGauge != null) successGauge.value = 1f;
        base.OnSuccess();
    }

    public override void OnFailure()
    {
        isActive = false;
        if (timeGauge != null) timeGauge.value = 0f;
        base.OnFailure();
    }
}

/// <summary>
/// 연결 노드 컴포넌트
/// </summary>
public class ConnectionNode : MonoBehaviour, IPointerClickHandler
{
    public Color nodeColor;
    public bool isLeft;
    public int index;
    public bool isConnected = false;

    public System.Action<ConnectionNode> onNodeClicked;

    private Image image;
    private bool isSelected = false;

    public void Initialize(Color color, bool left, int idx)
    {
        nodeColor = color;
        isLeft = left;
        index = idx;
        image = GetComponent<Image>();
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        if (image != null)
        {
            image.color = selected ? Color.white : nodeColor;
        }
    }

    public void SetConnected(bool connected)
    {
        isConnected = connected;
        if (image != null)
        {
            image.color = nodeColor;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isConnected)
        {
            onNodeClicked?.Invoke(this);
        }
    }
}

/// <summary>
/// 연결선 컴포넌트
/// </summary>
public class ConnectionLine : MonoBehaviour
{
    private ConnectionNode fromNode;
    private ConnectionNode toNode;
    private RectTransform rectTransform;
    private Image image;

    public void Initialize(ConnectionNode from, ConnectionNode to)
    {
        fromNode = from;
        toNode = to;
        rectTransform = GetComponent<RectTransform>();
        image = GetComponent<Image>();

        StartCoroutine(UpdateLineNextFrame());
    }

    private System.Collections.IEnumerator UpdateLineNextFrame()
    {
        // Layout Group이 위치 계산을 마친 다음 프레임에 선을 그림
        yield return null;
        UpdateLine();
    }

    private void UpdateLine()
    {
        if (fromNode == null || toNode == null) return;

        // 각 노드의 월드 중심 좌표를 구한 뒤 LinesContainer 로컬 좌표로 변환
        RectTransform fromRect = fromNode.GetComponent<RectTransform>();
        RectTransform toRect   = toNode.GetComponent<RectTransform>();

        Vector3 fromWorld = fromRect.TransformPoint(Vector3.zero);
        Vector3 toWorld   = toRect.TransformPoint(Vector3.zero);

        RectTransform lineParent = rectTransform.parent as RectTransform;
        Vector2 fromLocal = lineParent.InverseTransformPoint(fromWorld);
        Vector2 toLocal   = lineParent.InverseTransformPoint(toWorld);

        Vector2 direction = toLocal - fromLocal;
        float distance = direction.magnitude;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        rectTransform.sizeDelta = new Vector2(distance, 5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.localPosition = (fromLocal + toLocal) / 2f;
        rectTransform.localRotation = Quaternion.Euler(0, 0, angle);
    }
}
