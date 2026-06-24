using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 네트워크 브릿지 미니게임
/// 왼쪽 노드와 오른쪽 노드를 클릭해서 같은 색 단자끼리 연결합니다.
/// 
/// 변경점:
/// - 노드/라인을 코드로 생성하지 않음
/// - Hierarchy에 미리 만든 LeftNode_0~4 / RightNode_0~4 / Line_0~4 사용
/// - 성공 연결 시 Line 오브젝트를 활성화하고 두 노드 사이에 배치
/// </summary>
public class NetworkBridgeMinigame : HackingMinigameBase
{
    [Header("UI")]
    [SerializeField] private Transform gameParent;

    private Transform leftContainer;
    private Transform rightContainer;
    private Transform linesContainer;

    private Slider timeGauge;
    private Slider successGauge;

    private readonly List<ConnectionNode> leftNodes = new List<ConnectionNode>();
    private readonly List<ConnectionNode> rightNodes = new List<ConnectionNode>();
    private readonly List<ConnectionLine> connectionLines = new List<ConnectionLine>();

    private ConnectionNode selectedNode = null;

    [Header("게임 설정")]
    [SerializeField] private int totalConnections = 5;
    [SerializeField] private float timeLimit = 15f;

    [Header("노드 색상")]
    [SerializeField]
    private Color[] nodeColors =
    {
        Color.red,
        Color.blue,
        Color.green,
        Color.yellow,
        Color.magenta
    };

    [Header("라인 설정")]
    [SerializeField] private float lineThickness = 12f;

    private int completedConnections = 0;
    private float remainingTime = 0f;
    private bool isActive = false;

    /// <summary>
    /// HackingSystem에서 호출
    /// </summary>
    public void SetPreMadeUI(
        Transform left,
        Transform right,
        Transform lines,
        Slider timeSlider,
        Slider successSlider)
    {
        leftContainer = left;
        rightContainer = right;
        linesContainer = lines;
        timeGauge = timeSlider;
        successGauge = successSlider;

        if (gameParent == null && left != null)
            gameParent = left.parent;
    }

    public override void Initialize(
        System.Action onSuccessCallback,
        System.Action onFailureCallback)
    {
        base.Initialize(onSuccessCallback, onFailureCallback);
        ResetMinigame();
    }

    private void ResetMinigame()
    {
        completedConnections = 0;
        remainingTime = timeLimit;
        isActive = true;
        selectedNode = null;

        CachePreMadeNodes();
        CachePreMadeLines();
        SetupNodeColors();
        ResetNodes();
        ResetLines();

        UpdateTimeGauge();
        UpdateSuccessGauge();
    }

    /// <summary>
    /// Hierarchy의 LeftNode_0~4 / RightNode_0~4를 가져옴
    /// </summary>
    private void CachePreMadeNodes()
    {
        leftNodes.Clear();
        rightNodes.Clear();

        if (leftContainer == null)
        {
            Debug.LogWarning("[NetworkBridgeMinigame] LeftContainer가 없습니다.");
            return;
        }

        if (rightContainer == null)
        {
            Debug.LogWarning("[NetworkBridgeMinigame] RightContainer가 없습니다.");
            return;
        }

        for (int i = 0; i < totalConnections; i++)
        {
            Transform leftChild = leftContainer.Find($"LeftNode_{i}");
            Transform rightChild = rightContainer.Find($"RightNode_{i}");

            if (leftChild != null)
            {
                ConnectionNode node = GetOrAddNode(leftChild.gameObject);
                node.Initialize(Color.white, true, i);
                node.onNodeClicked -= OnNodeClicked;
                node.onNodeClicked += OnNodeClicked;
                leftNodes.Add(node);
            }
            else
            {
                Debug.LogWarning($"[NetworkBridgeMinigame] LeftNode_{i}를 찾을 수 없습니다.");
            }

            if (rightChild != null)
            {
                ConnectionNode node = GetOrAddNode(rightChild.gameObject);
                node.Initialize(Color.white, false, i);
                node.onNodeClicked -= OnNodeClicked;
                node.onNodeClicked += OnNodeClicked;
                rightNodes.Add(node);
            }
            else
            {
                Debug.LogWarning($"[NetworkBridgeMinigame] RightNode_{i}를 찾을 수 없습니다.");
            }
        }
    }

    private ConnectionNode GetOrAddNode(GameObject obj)
    {
        Button button = obj.GetComponent<Button>();
        if (button == null)
            obj.AddComponent<Button>();

        ConnectionNode node = obj.GetComponent<ConnectionNode>();
        if (node == null)
            node = obj.AddComponent<ConnectionNode>();

        return node;
    }

    /// <summary>
    /// Hierarchy의 Line_0~4를 가져옴
    /// </summary>
    private void CachePreMadeLines()
    {
        connectionLines.Clear();

        if (linesContainer == null)
        {
            Debug.LogWarning("[NetworkBridgeMinigame] LinesContainer가 없습니다.");
            return;
        }

        for (int i = 0; i < totalConnections; i++)
        {
            Transform lineChild = linesContainer.Find($"Line_{i}");

            if (lineChild == null)
            {
                Debug.LogWarning($"[NetworkBridgeMinigame] Line_{i}를 찾을 수 없습니다.");
                continue;
            }

            Image img = lineChild.GetComponent<Image>();
            if (img == null)
                img = lineChild.gameObject.AddComponent<Image>();

            ConnectionLine line = lineChild.GetComponent<ConnectionLine>();
            if (line == null)
                line = lineChild.gameObject.AddComponent<ConnectionLine>();

            connectionLines.Add(line);
        }
    }

    /// <summary>
    /// 왼쪽/오른쪽 노드 색상 세팅
    /// 왼쪽 색상 순서와 오른쪽 색상 순서를 다르게 섞음
    /// </summary>
    private void SetupNodeColors()
    {
        List<Color> colors = new List<Color>();

        for (int i = 0; i < totalConnections; i++)
        {
            colors.Add(nodeColors[i % nodeColors.Length]);
        }

        List<Color> leftColors = new List<Color>(colors);
        List<Color> rightColors = new List<Color>(colors);

        ShuffleColors(leftColors);
        ShuffleColors(rightColors);

        for (int i = 0; i < leftNodes.Count; i++)
        {
            leftNodes[i].Initialize(leftColors[i], true, i);
            leftNodes[i].onNodeClicked -= OnNodeClicked;
            leftNodes[i].onNodeClicked += OnNodeClicked;
        }

        for (int i = 0; i < rightNodes.Count; i++)
        {
            rightNodes[i].Initialize(rightColors[i], false, i);
            rightNodes[i].onNodeClicked -= OnNodeClicked;
            rightNodes[i].onNodeClicked += OnNodeClicked;
        }
    }

    private void ShuffleColors(List<Color> colors)
    {
        for (int i = colors.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Color temp = colors[i];
            colors[i] = colors[j];
            colors[j] = temp;
        }
    }

    private void ResetNodes()
    {
        foreach (ConnectionNode node in leftNodes)
        {
            if (node != null)
            {
                node.gameObject.SetActive(true);
                node.ResetVisual();
            }
        }

        foreach (ConnectionNode node in rightNodes)
        {
            if (node != null)
            {
                node.gameObject.SetActive(true);
                node.ResetVisual();
            }
        }
    }

    private void ResetLines()
    {
        foreach (ConnectionLine line in connectionLines)
        {
            if (line != null)
            {
                line.Clear();
                line.gameObject.SetActive(false);
            }
        }
    }

    private void OnNodeClicked(ConnectionNode node)
    {
        if (!isActive)
            return;

        if (node == null || node.isConnected)
            return;

        if (selectedNode == null)
        {
            selectedNode = node;
            selectedNode.SetSelected(true);
            return;
        }

        if (selectedNode == node)
        {
            selectedNode.SetSelected(false);
            selectedNode = null;
            return;
        }

        bool differentSide = selectedNode.isLeft != node.isLeft;
        bool sameColor = selectedNode.nodeColor == node.nodeColor;

        if (differentSide && sameColor)
        {
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
            selectedNode.SetSelected(false);
            selectedNode = null;
            OnFailure();
        }
    }

    /// <summary>
    /// 미리 만들어둔 Line을 하나 꺼내서 연결선으로 사용
    /// </summary>
    private void CreateConnection(ConnectionNode from, ConnectionNode to)
    {
        if (completedConnections >= connectionLines.Count)
        {
            Debug.LogWarning("[NetworkBridgeMinigame] 사용할 수 있는 Line이 부족합니다.");
            return;
        }

        ConnectionLine line = connectionLines[completedConnections];

        if (line == null)
            return;

        line.gameObject.SetActive(true);
        line.Initialize(from, to, lineThickness);
    }

    private void UpdateTimeGauge()
    {
        if (timeGauge != null)
            timeGauge.value = remainingTime / timeLimit;
    }

    private void UpdateSuccessGauge()
    {
        if (successGauge != null)
            successGauge.value = (float)completedConnections / totalConnections;
    }

    private void Update()
    {
        if (!isActive)
            return;

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

        if (timeGauge != null)
            timeGauge.value = 1f;

        if (successGauge != null)
            successGauge.value = 1f;

        base.OnSuccess();
    }

    public override void OnFailure()
    {
        isActive = false;

        if (timeGauge != null)
            timeGauge.value = 0f;

        base.OnFailure();
    }
}

/// <summary>
/// 연결 노드
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
        isConnected = false;
        isSelected = false;

        image = GetComponent<Image>();

        if (image != null)
            image.color = nodeColor;
    }

    public void ResetVisual()
    {
        isConnected = false;
        isSelected = false;

        if (image == null)
            image = GetComponent<Image>();

        if (image != null)
            image.color = nodeColor;
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;

        if (image == null)
            image = GetComponent<Image>();

        if (image != null)
        {
            image.color = selected ? Color.white : nodeColor;
        }
    }

    public void SetConnected(bool connected)
    {
        isConnected = connected;
        isSelected = false;

        if (image == null)
            image = GetComponent<Image>();

        if (image != null)
            image.color = nodeColor;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isConnected)
            onNodeClicked?.Invoke(this);
    }
}

/// <summary>
/// 연결선
/// 미리 만들어둔 Line 오브젝트의 위치/길이/회전만 조정
/// </summary>
public class ConnectionLine : MonoBehaviour
{
    private ConnectionNode fromNode;
    private ConnectionNode toNode;
    private RectTransform rectTransform;
    private Image image;
    private float thickness = 12f;

    public void Initialize(ConnectionNode from, ConnectionNode to, float lineThickness)
    {
        fromNode = from;
        toNode = to;
        thickness = lineThickness;

        rectTransform = GetComponent<RectTransform>();
        image = GetComponent<Image>();

        if (image != null && fromNode != null)
            image.color = fromNode.nodeColor;

        UpdateLine();
    }

    public void Clear()
    {
        fromNode = null;
        toNode = null;

        rectTransform = GetComponent<RectTransform>();
    }

    private void UpdateLine()
    {
        if (fromNode == null || toNode == null)
            return;

        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        RectTransform fromRect = fromNode.GetComponent<RectTransform>();
        RectTransform toRect = toNode.GetComponent<RectTransform>();

        if (fromRect == null || toRect == null || rectTransform == null)
            return;

        RectTransform lineParent = rectTransform.parent as RectTransform;

        if (lineParent == null)
            return;

        Vector3 fromWorld = fromRect.TransformPoint(Vector3.zero);
        Vector3 toWorld = toRect.TransformPoint(Vector3.zero);

        Vector2 fromLocal = lineParent.InverseTransformPoint(fromWorld);
        Vector2 toLocal = lineParent.InverseTransformPoint(toWorld);

        Vector2 direction = toLocal - fromLocal;
        float distance = direction.magnitude;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);

        rectTransform.sizeDelta = new Vector2(distance, thickness);
        rectTransform.localPosition = (fromLocal + toLocal) / 2f;
        rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
    }
}