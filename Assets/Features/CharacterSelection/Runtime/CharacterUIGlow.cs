using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Image))]
public class CharacterUIGlow : MonoBehaviour
{
    #region Singleton

    public static CharacterUIGlow Instance { get; private set; }

    #endregion


    #region GlowPoint

    [System.Serializable]
    public class GlowPoint
    {
        [SerializeField] private string pointName = "Glow";
        [SerializeField] private Vector2 anchoredPosition;
        [SerializeField] private Vector2 size = new Vector2(50f, 50f);
        [SerializeField] private RectTransform anchorTransform;

        [Header("10 x 10 카드 그리드")]
        [Tooltip("활성화하면 Anchored Position 대신 카드 좌하단 (0,0), 우상단 (10,10) 그리드 좌표를 사용합니다.")]
        [SerializeField] private bool useGridPosition;

        [SerializeField] private Vector2 gridPosition = new Vector2(5f, 5f);

        [Header("개별 발광 설정")]
        [SerializeField] private bool enabled = true;

        [Tooltip("이 지점만 별도로 적용되는 발광 강도")]
        [Range(0f, 2f)]
        [SerializeField] private float intensityMultiplier = 1f;

        [Tooltip("강화 효과 발생 시 크기 증가량")]
        [Range(0f, 1f)]
        [SerializeField] private float boostScaleAmount = 0.15f;

        private readonly Image[] _images = new Image[3];
        private readonly RectTransform[] _rectTransforms = new RectTransform[3];
        private readonly Vector2[] _baseSizes = new Vector2[3];
        private Vector3 _layerSize = new Vector3(4.8f, 2.2f, 0.72f);


        public string PointName => pointName;
        public bool IsEnabled => enabled;
        public float IntensityMultiplier => intensityMultiplier;
        public RectTransform AnchorTransform => anchorTransform;


        public GlowPoint()
        {
        }

        public GlowPoint(
            string name,
            Vector2 position,
            Vector2 pointSize)
        {
            pointName = name;
            anchoredPosition = position;
            size = pointSize;
        }


        public void Initialize(int layerIndex, Image image, RectTransform rectTransform, Vector3 layerSize)
        {
            if (layerIndex < 0 || layerIndex >= _images.Length)
                return;

            _images[layerIndex] = image;
            _rectTransforms[layerIndex] = rectTransform;
            _layerSize = layerSize;
            _baseSizes[layerIndex] = size * GetLayerSize(layerIndex);
            rectTransform.sizeDelta = _baseSizes[layerIndex];
        }


        public void BindAnchor(RectTransform anchor)
        {
            anchorTransform = anchor;
            SyncFromAnchor();
        }


        public void SyncFromAnchor()
        {
            if (anchorTransform)
                anchoredPosition = anchorTransform.anchoredPosition;
        }


        public void SetEnabled(bool value)
        {
            enabled = value;

            for (int i = 0; i < _images.Length; i++)
                if (_images[i] != null)
                    _images[i].enabled = value;
        }


        public void SetIntensity(float value)
        {
            intensityMultiplier = Mathf.Clamp(value, 0f, 2f);
        }


        public void ApplyVisual(
            Color color,
            float alpha,
            float boostAmount,
            Vector3 layerAlpha,
            Vector3 whiteBalance)
        {
            for (int i = 0; i < _images.Length; i++)
            {
                Image image = _images[i];
                RectTransform rectTransform = _rectTransforms[i];
                if (image == null)
                    continue;

                if (!enabled)
                {
                    image.enabled = false;
                    continue;
                }

                image.enabled = true;
                float whiten = i == 0 ? whiteBalance.x : i == 1 ? whiteBalance.y : whiteBalance.z;
                float layerOpacity = i == 0 ? layerAlpha.x : i == 1 ? layerAlpha.y : layerAlpha.z;
                Color finalColor = Color.Lerp(color, Color.white, whiten);
                finalColor.a = Mathf.Clamp01(alpha * intensityMultiplier * layerOpacity);
                image.color = finalColor;

                if (rectTransform != null)
                {
                    float scale = 1f + (boostScaleAmount * boostAmount);
                    rectTransform.sizeDelta = _baseSizes[i] * scale;
                }
            }
        }


        public void ResetSize()
        {
            for (int i = 0; i < _rectTransforms.Length; i++)
                if (_rectTransforms[i] != null)
                    _rectTransforms[i].sizeDelta = _baseSizes[i];
        }


        internal void ApplyLayout(RectTransform rectTransform)
        {
            if (rectTransform == null)
                return;

            rectTransform.anchoredPosition = ResolvePosition(rectTransform);
            rectTransform.sizeDelta = size;
        }


        internal void RefreshRuntimeLayout(RectTransform parentRect)
        {
            Vector2 position = ResolvePosition(parentRect);
            for (int i = 0; i < _rectTransforms.Length; i++)
            {
                RectTransform rectTransform = _rectTransforms[i];
                if (rectTransform == null) continue;
                rectTransform.anchoredPosition = position;
                _baseSizes[i] = size * GetLayerSize(i);
                rectTransform.sizeDelta = _baseSizes[i];
            }
        }


        internal void BindAndRefreshLayer(
            RectTransform parentRect,
            int layerIndex,
            RectTransform layerRect,
            Vector3 layerSize)
        {
            if (layerIndex < 0 || layerIndex >= _rectTransforms.Length || layerRect == null)
                return;

            _rectTransforms[layerIndex] = layerRect;
            _layerSize = layerSize;
            _baseSizes[layerIndex] = size * GetLayerSize(layerIndex);
            layerRect.anchoredPosition = ResolvePosition(parentRect);
            layerRect.sizeDelta = _baseSizes[layerIndex];
        }

        private float GetLayerSize(int layerIndex)
        {
            return layerIndex == 0 ? _layerSize.x : layerIndex == 1 ? _layerSize.y : _layerSize.z;
        }


        private Vector2 ResolvePosition(RectTransform parentRect)
        {
            if (!useGridPosition)
                return anchoredPosition;

            Rect rect = parentRect.rect;
            return new Vector2(
                Mathf.Lerp(rect.xMin, rect.xMax, Mathf.Clamp01(gridPosition.x / 10f)),
                Mathf.Lerp(rect.yMin, rect.yMax, Mathf.Clamp01(gridPosition.y / 10f)));
        }


#if UNITY_EDITOR
        internal void DrawEditorGizmo(RectTransform parentRect, Color color)
        {
            if (parentRect == null || !enabled)
                return;

            Color previousColor = Gizmos.color;
            Gizmos.color = color;
            Vector3 center = parentRect.TransformPoint(ResolvePosition(parentRect));
            Vector3 worldSize = Vector3.Scale(new Vector3(size.x, size.y, 0f), parentRect.lossyScale);
            Gizmos.DrawWireCube(center, worldSize);
            Gizmos.color = previousColor;
        }
#endif
    }

    #endregion


    #region Inspector - Glow Points

    [Header("기본 발광 지점")]

    [SerializeField]
    private GlowPoint core = new GlowPoint(
        "CoreGlow",
        new Vector2(0f, 40f),
        new Vector2(55f, 55f));

    [SerializeField]
    private GlowPoint eyeLeft = new GlowPoint(
        "EyeGlowL",
        new Vector2(-14f, 195f),
        new Vector2(22f, 22f));

    [SerializeField]
    private GlowPoint eyeRight = new GlowPoint(
        "EyeGlowR",
        new Vector2(14f, 195f),
        new Vector2(22f, 22f));


    [Header("추가 발광 지점")]
    [Tooltip("무기, 가슴, 팔, 장식 등의 추가 발광 위치")]
    [SerializeField]
    private List<GlowPoint> extraGlowPoints = new();

    #endregion


    #region Inspector - Color

    [Header("발광 색상 / 기본 강도")]

    [SerializeField, ColorUsage(true, true)]
    private Color glowColor =
        new Color(1f, 0.15f, 0.1f, 1f);

    [Range(0f, 1f)]
    [SerializeField]
    private float maxAlpha = 1f;

    [Tooltip("전체 발광 강도 배율")]
    [Range(0f, 2f)]
    [SerializeField]
    private float globalIntensity = 1f;

    [Header("현실적인 3단 광원")]
    [Tooltip("Outer / Inner / HotCore 레이어의 크기 배율입니다. Outer 값을 키우면 점처럼 보이지 않고 넓게 퍼집니다.")]
    [SerializeField] private Vector3 layerSize = new Vector3(4.8f, 2.2f, 0.72f);

    [Tooltip("Outer / Inner / HotCore 레이어의 알파 배율")]
    [SerializeField] private Vector3 layerAlpha = new Vector3(0.10f, 0.42f, 1f);

    [Tooltip("각 레이어가 백색 중심광에 가까워지는 정도")]
    [SerializeField] private Vector3 layerWhiteBalance = new Vector3(0.04f, 0.18f, 0.78f);

    #endregion


    #region Inspector - Fade

    [Header("Fade")]

    [Min(0.01f)]
    [SerializeField]
    private float fadeInDuration = 0.25f;

    [Min(0.01f)]
    [SerializeField]
    private float fadeOutDuration = 0.35f;

    #endregion


    #region Inspector - Pulse

    [Header("Pulse")]

    [SerializeField]
    private bool pulse = true;

    [Min(0f)]
    [SerializeField]
    private float pulseSpeed = 1.65f;

    [Range(0f, 1f)]
    [SerializeField]
    private float pulseMinAlpha = 0.82f;

    [Tooltip("맥동할 때 크기도 같이 움직일지 여부")]
    [SerializeField]
    private bool pulseScale = true;

    [Range(0f, 0.25f)]
    [SerializeField]
    private float pulseScaleAmount = 0.018f;

    #endregion


    #region Inspector - Boost

    [Header("강화 / 각성 효과")]

    [Tooltip("강화 효과 기본 지속시간")]
    [Min(0f)]
    [SerializeField]
    private float defaultBoostDuration = 0.5f;

    [Tooltip("강화 효과 발광 배율")]
    [Range(1f, 3f)]
    [SerializeField]
    private float defaultBoostIntensity = 1.5f;

    [Tooltip("강화 시 발광 크기 추가 증가")]
    [Range(0f, 1f)]
    [SerializeField]
    private float defaultBoostScale = 0.3f;

    #endregion


    #region Inspector - Character

    [Header("캐릭터 본체")]

    [SerializeField]
    private bool dimCharacterWhenInactive = true;

    [SerializeField]
    private Color activeBodyColor = Color.white;

    [SerializeField]
    private Color inactiveBodyColor =
        new Color(0.45f, 0.45f, 0.48f, 1f);

    #endregion


    #region Inspector - Flicker

    [Header("비활성화 Flicker")]

    [SerializeField]
    private bool flickerOnDeactivate = true;

    [Min(0)]
    [SerializeField]
    private int flickerCount = 3;

    [Min(0.01f)]
    [SerializeField]
    private float flickerInterval = 0.06f;

    #endregion


    #region Inspector - Texture

    [Header("Glow Texture")]

    [Range(32, 512)]
    [SerializeField]
    private int glowTextureResolution = 128;

    [Range(0.5f, 5f)]
    [SerializeField]
    private float glowFalloff = 2.2f;

    [Header("HDR Emission Material")]
    [Tooltip("Outer / Inner / HotCore 순서의 HDR Emission 강도입니다.")]
    [SerializeField] private Vector3 emissionIntensity = new Vector3(0.35f, 1.15f, 3.1f);

    #endregion


    #region Runtime

    private readonly List<GlowPoint> _runtimePoints = new();

    private Sprite _glowSprite;
    private Texture2D _glowTexture;
    private Material _emissionMaterialTemplate;
    private readonly Material[] _emissionMaterials = new Material[3];

    private Image _characterImage;

    private Coroutine _stateRoutine;
    private Coroutine _boostRoutine;

    private bool _selected;

    private float _currentAlpha;
    private float _temporaryIntensity = 1f;
    private float _temporaryScale;

    #endregion


    #region Public Properties

    public bool IsSelected => _selected;

    public float CurrentIntensity =>
        globalIntensity * _temporaryIntensity;

    public Color CurrentGlowColor => glowColor;

    #endregion


    #region Unity

    private void Awake()
    {
        InitializeSingleton();

        _characterImage = GetComponent<Image>();

        CreateGlowSprite();
        BuildGlowPoints();

        ApplyGlow(0f);

        if (dimCharacterWhenInactive &&
            _characterImage != null)
        {
            _characterImage.color =
                inactiveBodyColor;
        }
    }


    private void OnValidate()
    {
        RefreshGlowLayout();
    }


    [ContextMenu("Refresh Glow Layout")]
    public void RefreshGlowLayout()
    {
        RectTransform parentRect = transform as RectTransform;
        if (parentRect == null) return;

        RefreshPointLayout(core, parentRect);
        RefreshPointLayout(eyeLeft, parentRect);
        RefreshPointLayout(eyeRight, parentRect);
        for (int i = 0; i < extraGlowPoints.Count; i++)
            RefreshPointLayout(extraGlowPoints[i], parentRect);

        if (Application.isPlaying)
            ApplyGlow(_currentAlpha);

        Canvas.ForceUpdateCanvases();
    }


    private void RefreshPointLayout(GlowPoint point, RectTransform parentRect)
    {
        if (point == null) return;

        RectTransform anchor = point.AnchorTransform;
        if (anchor)
            point.SyncFromAnchor();
        else
            point.RefreshRuntimeLayout(parentRect);

        string[] suffixes = { "_Outer", "_Inner", "_HotCore" };
        for (int layer = 0; layer < suffixes.Length; layer++)
        {
            RectTransform layerRect = anchor
                ? anchor.Find(point.PointName + suffixes[layer]) as RectTransform
                : transform.Find(point.PointName + suffixes[layer]) as RectTransform;
            if (layerRect)
            {
                point.BindAndRefreshLayer(parentRect, layer, layerRect, layerSize);
                layerRect.anchoredPosition = Vector2.zero;
            }
        }
    }


    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        if (_glowSprite != null)
            Destroy(_glowSprite);

        if (_glowTexture != null)
            Destroy(_glowTexture);

        for (int i = 0; i < _emissionMaterials.Length; i++)
            if (_emissionMaterials[i] != null)
                Destroy(_emissionMaterials[i]);
    }

    #endregion


    #region Singleton

    private void InitializeSingleton()
    {
        // 카드 선택 화면은 캐릭터마다 독립된 발광 인스턴스가 필요하다.
        // Instance는 기존 외부 코드 호환용으로 첫 인스턴스만 가리키며,
        // 중복 컴포넌트의 GameObject를 제거하지 않는다.
        if (Instance == null)
            Instance = this;
    }

    #endregion


    #region Setup

    private void CreateGlowSprite()
    {
        _glowTexture =
            GenerateRadialGlowTexture(
                glowTextureResolution);

        _glowSprite = Sprite.Create(
            _glowTexture,
            new Rect(
                0f,
                0f,
                glowTextureResolution,
                glowTextureResolution),
            new Vector2(0.5f, 0.5f));
    }


    private void BuildGlowPoints()
    {
        _runtimePoints.Clear();

        RegisterPoint(core);
        RegisterPoint(eyeLeft);
        RegisterPoint(eyeRight);

        for (int i = 0;
             i < extraGlowPoints.Count;
             i++)
        {
            RegisterPoint(extraGlowPoints[i]);
        }
    }


    private void RegisterPoint(GlowPoint point)
    {
        if (point == null)
            return;

        SetupPoint(point);

        _runtimePoints.Add(point);
    }


    private void SetupPoint(GlowPoint point)
    {
        RectTransform anchor = point.AnchorTransform;
        if (!anchor)
        {
            GameObject anchorObject = new GameObject(point.PointName, typeof(RectTransform));
            anchorObject.transform.SetParent(transform, false);
            anchor = anchorObject.GetComponent<RectTransform>();
            anchor.anchorMin = anchor.anchorMax = anchor.pivot = new Vector2(0.5f, 0.5f);
            ApplyPointTransform(point, anchor);
            point.BindAnchor(anchor);
        }
        else
        {
            point.SyncFromAnchor();
        }

        string[] suffixes = { "_Outer", "_Inner", "_HotCore" };
        for (int layer = 0; layer < suffixes.Length; layer++)
        {
            GameObject glowObject = new GameObject(point.PointName + suffixes[layer],
                typeof(RectTransform), typeof(Canvas), typeof(Image));
            glowObject.transform.SetParent(anchor, false);

            RectTransform rect = glowObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;

            Image image = glowObject.GetComponent<Image>();
            image.sprite = _glowSprite;
            image.raycastTarget = false;
            Material emissionMaterial = GetEmissionMaterial(layer);
            if (emissionMaterial)
                image.material = emissionMaterial;

            // Each emission pass owns a nested canvas. Mosaic slices stay on the
            // base canvas while these layers render independently above them.
            Canvas glowCanvas = glowObject.GetComponent<Canvas>();
            glowCanvas.overrideSorting = false;

            point.Initialize(layer, image, rect, layerSize);
        }
    }


    private void ApplyPointTransform(
        GlowPoint point,
        RectTransform rect)
    {
        // Serialized private 데이터는
        // GlowPoint 내부에서 관리하기 위해
        // Reflection이나 직접 노출 없이
        // 초기 Transform 값은 아래 전용 메서드 사용.
        GlowPointLayoutUtility.Apply(point, rect);
    }


    private Material GetEmissionMaterial(int layer)
    {
        if (layer < 0 || layer >= _emissionMaterials.Length)
            return null;

        if (_emissionMaterials[layer] != null)
            return _emissionMaterials[layer];

        if (_emissionMaterialTemplate == null)
            _emissionMaterialTemplate = Resources.Load<Material>("UIEmissionGlow");

        if (_emissionMaterialTemplate == null)
        {
            Debug.LogWarning("[CharacterUIGlow] UIEmissionGlow material을 찾지 못했습니다.");
            return null;
        }

        Material material = new Material(_emissionMaterialTemplate)
        {
            name = $"UIEmissionGlow_{layer}"
        };
        float intensity = layer == 0 ? emissionIntensity.x : layer == 1 ? emissionIntensity.y : emissionIntensity.z;
        material.SetFloat("_EmissionIntensity", intensity);
        _emissionMaterials[layer] = material;
        return material;
    }

    #endregion


    #region Selection

    /// <summary>
    /// 캐릭터 발광 활성 / 비활성
    /// </summary>
    public void SetSelected(bool selected)
    {
        if (_selected == selected)
            return;

        _selected = selected;

        StopStateRoutine();

        _stateRoutine =
            StartCoroutine(
                selected
                    ? ActivateSequence()
                    : DeactivateSequence());
    }


    public void ToggleSelected()
    {
        SetSelected(!_selected);
    }

    #endregion


    #region Public Glow Control

    /// <summary>
    /// 전체 발광 색상을 변경한다.
    /// </summary>
    public void SetGlowColor(Color color)
    {
        glowColor = color;

        ApplyGlow(_currentAlpha);
    }


    /// <summary>
    /// 전체 발광 강도.
    /// 0 = 소등
    /// 1 = 기본
    /// 2 = 강한 발광
    /// </summary>
    public void SetIntensity(float intensity)
    {
        globalIntensity =
            Mathf.Clamp(intensity, 0f, 2f);

        ApplyGlow(_currentAlpha);
    }


    /// <summary>
    /// 특정 발광 지점 활성화.
    /// </summary>
    public void SetPointEnabled(
        string pointName,
        bool enabled)
    {
        GlowPoint point =
            FindPoint(pointName);

        if (point == null)
            return;

        point.SetEnabled(enabled);

        ApplyGlow(_currentAlpha);
    }


    /// <summary>
    /// 특정 발광 지점의 강도만 변경.
    /// </summary>
    public void SetPointIntensity(
        string pointName,
        float intensity)
    {
        GlowPoint point =
            FindPoint(pointName);

        if (point == null)
            return;

        point.SetIntensity(intensity);

        ApplyGlow(_currentAlpha);
    }


    /// <summary>
    /// 강화 / 각성 / 스킬 획득 등의 순간 강조 효과.
    /// </summary>
    public void PlayBoost()
    {
        PlayBoost(
            defaultBoostDuration,
            defaultBoostIntensity,
            defaultBoostScale);
    }


    public void PlayBoost(
        float duration,
        float intensity)
    {
        PlayBoost(
            duration,
            intensity,
            defaultBoostScale);
    }


    public void PlayBoost(
        float duration,
        float intensity,
        float scaleAmount)
    {
        if (_boostRoutine != null)
            StopCoroutine(_boostRoutine);

        _boostRoutine =
            StartCoroutine(
                BoostSequence(
                    duration,
                    intensity,
                    scaleAmount));
    }


    /// <summary>
    /// 모든 임시 강화 효과 제거.
    /// </summary>
    public void ResetBoost()
    {
        if (_boostRoutine != null)
        {
            StopCoroutine(_boostRoutine);
            _boostRoutine = null;
        }

        _temporaryIntensity = 1f;
        _temporaryScale = 0f;

        ApplyGlow(_currentAlpha);
    }


    public void ForceOff()
    {
        StopAllEffectCoroutines();

        _selected = false;
        _currentAlpha = 0f;

        _temporaryIntensity = 1f;
        _temporaryScale = 0f;

        ApplyGlow(0f);

        if (dimCharacterWhenInactive &&
            _characterImage != null)
        {
            _characterImage.color =
                inactiveBodyColor;
        }
    }


    public void ForceOn()
    {
        StopAllEffectCoroutines();

        _selected = true;
        _currentAlpha = maxAlpha;

        ApplyGlow(maxAlpha);

        if (dimCharacterWhenInactive &&
            _characterImage != null)
        {
            _characterImage.color =
                activeBodyColor;
        }

        if (pulse)
        {
            _stateRoutine =
                StartCoroutine(PulseSequence());
        }
    }

    #endregion


    #region Activate

    private IEnumerator ActivateSequence()
    {
        float timer = 0f;

        float startAlpha =
            _currentAlpha;

        Color bodyStart =
            _characterImage != null
                ? _characterImage.color
                : activeBodyColor;

        while (timer < fadeInDuration)
        {
            timer += Time.deltaTime;

            float normalized =
                Mathf.Clamp01(
                    timer / fadeInDuration);

            float eased =
                EaseOutCubic(normalized);

            _currentAlpha =
                Mathf.Lerp(
                    startAlpha,
                    maxAlpha,
                    eased);

            ApplyGlow(_currentAlpha);

            if (dimCharacterWhenInactive &&
                _characterImage != null)
            {
                _characterImage.color =
                    Color.Lerp(
                        bodyStart,
                        activeBodyColor,
                        eased);
            }

            yield return null;
        }

        _currentAlpha = maxAlpha;

        ApplyGlow(_currentAlpha);

        if (dimCharacterWhenInactive &&
            _characterImage != null)
        {
            _characterImage.color =
                activeBodyColor;
        }

        if (pulse)
            yield return PulseSequence();

        _stateRoutine = null;
    }

    #endregion


    #region Pulse

    private IEnumerator PulseSequence()
    {
        while (_selected)
        {
            float wave =
                Mathf.Sin(
                    Time.unscaledTime *
                    pulseSpeed);

            wave =
                wave * 0.5f + 0.5f;

            _currentAlpha =
                Mathf.Lerp(
                    pulseMinAlpha,
                    maxAlpha,
                    wave);

            float pulseScaleValue = 0f;

            if (pulseScale)
            {
                pulseScaleValue =
                    wave * pulseScaleAmount;
            }

            ApplyGlow(
                _currentAlpha,
                pulseScaleValue);

            yield return null;
        }
    }

    #endregion


    #region Deactivate

    private IEnumerator DeactivateSequence()
    {
        if (flickerOnDeactivate)
            yield return FlickerSequence();

        float timer = 0f;

        float startAlpha =
            _currentAlpha;

        Color bodyStart =
            _characterImage != null
                ? _characterImage.color
                : inactiveBodyColor;

        while (timer < fadeOutDuration)
        {
            timer += Time.deltaTime;

            float normalized =
                Mathf.Clamp01(
                    timer / fadeOutDuration);

            float eased =
                EaseInCubic(normalized);

            _currentAlpha =
                Mathf.Lerp(
                    startAlpha,
                    0f,
                    eased);

            ApplyGlow(_currentAlpha);

            if (dimCharacterWhenInactive &&
                _characterImage != null)
            {
                _characterImage.color =
                    Color.Lerp(
                        bodyStart,
                        inactiveBodyColor,
                        normalized);
            }

            yield return null;
        }

        _currentAlpha = 0f;

        ApplyGlow(0f);

        ResetPointSizes();

        if (dimCharacterWhenInactive &&
            _characterImage != null)
        {
            _characterImage.color =
                inactiveBodyColor;
        }

        _stateRoutine = null;
    }


    private IEnumerator FlickerSequence()
    {
        for (int i = 0;
             i < flickerCount;
             i++)
        {
            ApplyGlow(maxAlpha * 0.15f);

            yield return
                new WaitForSeconds(
                    flickerInterval);

            ApplyGlow(maxAlpha);

            yield return
                new WaitForSeconds(
                    flickerInterval);
        }
    }

    #endregion


    #region Boost

    private IEnumerator BoostSequence(
        float duration,
        float targetIntensity,
        float targetScale)
    {
        duration =
            Mathf.Max(0.01f, duration);

        targetIntensity =
            Mathf.Max(1f, targetIntensity);

        targetScale =
            Mathf.Max(0f, targetScale);

        float halfDuration =
            duration * 0.5f;

        float timer = 0f;

        // 상승
        while (timer < halfDuration)
        {
            timer += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    timer / halfDuration);

            float eased =
                EaseOutCubic(t);

            _temporaryIntensity =
                Mathf.Lerp(
                    1f,
                    targetIntensity,
                    eased);

            _temporaryScale =
                Mathf.Lerp(
                    0f,
                    targetScale,
                    eased);

            ApplyGlow(_currentAlpha);

            yield return null;
        }

        timer = 0f;

        // 감소
        while (timer < halfDuration)
        {
            timer += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    timer / halfDuration);

            float eased =
                EaseInCubic(t);

            _temporaryIntensity =
                Mathf.Lerp(
                    targetIntensity,
                    1f,
                    eased);

            _temporaryScale =
                Mathf.Lerp(
                    targetScale,
                    0f,
                    eased);

            ApplyGlow(_currentAlpha);

            yield return null;
        }

        _temporaryIntensity = 1f;
        _temporaryScale = 0f;

        ApplyGlow(_currentAlpha);

        _boostRoutine = null;
    }

    #endregion


    #region Rendering

    private void ApplyGlow(
        float alpha,
        float additionalScale = 0f)
    {
        UpdateEmissionMaterialStrengths();

        float intensity =
            globalIntensity *
            _temporaryIntensity;

        float finalAlpha =
            alpha * intensity;

        float finalScale =
            _temporaryScale +
            additionalScale;

        for (int i = 0;
             i < _runtimePoints.Count;
             i++)
        {
            _runtimePoints[i].ApplyVisual(
                glowColor,
                finalAlpha,
                finalScale,
                layerAlpha,
                layerWhiteBalance);
        }
    }


    private void UpdateEmissionMaterialStrengths()
    {
        for (int layer = 0; layer < _emissionMaterials.Length; layer++)
        {
            Material material = _emissionMaterials[layer];
            if (material == null) continue;
            float intensity = layer == 0 ? emissionIntensity.x : layer == 1 ? emissionIntensity.y : emissionIntensity.z;
            material.SetFloat("_EmissionIntensity", intensity);
        }
    }


    private void ResetPointSizes()
    {
        for (int i = 0;
             i < _runtimePoints.Count;
             i++)
        {
            _runtimePoints[i].ResetSize();
        }
    }

    #endregion


    #region Search

    private GlowPoint FindPoint(string pointName)
    {
        if (string.IsNullOrWhiteSpace(pointName))
            return null;

        for (int i = 0;
             i < _runtimePoints.Count;
             i++)
        {
            GlowPoint point =
                _runtimePoints[i];

            if (point.PointName == pointName)
                return point;
        }

        Debug.LogWarning(
            $"[CharacterUIGlow] GlowPoint를 찾지 못했습니다 : {pointName}");

        return null;
    }

    #endregion


    #region Texture

    private Texture2D GenerateRadialGlowTexture(
        int resolution)
    {
        Texture2D texture =
            new Texture2D(
                resolution,
                resolution,
                TextureFormat.RGBA32,
                false);

        texture.wrapMode =
            TextureWrapMode.Clamp;

        texture.filterMode =
            FilterMode.Bilinear;

        Color[] pixels =
            new Color[
                resolution *
                resolution];

        float center =
            (resolution - 1) * 0.5f;

        float maxDistance =
            resolution * 0.5f;

        int index = 0;

        for (int y = 0;
             y < resolution;
             y++)
        {
            for (int x = 0;
                 x < resolution;
                 x++)
            {
                float dx =
                    x - center;

                float dy =
                    y - center;

                float distance =
                    Mathf.Sqrt(
                        dx * dx +
                        dy * dy);

                float normalized =
                    Mathf.Clamp01(
                        distance /
                        maxDistance);

                float alpha =
                    Mathf.Pow(
                        1f - normalized,
                        glowFalloff);

                pixels[index++] =
                    new Color(
                        1f,
                        1f,
                        1f,
                        alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(false, false);

        return texture;
    }

    #endregion


    #region Coroutine

    private void StopStateRoutine()
    {
        if (_stateRoutine == null)
            return;

        StopCoroutine(_stateRoutine);
        _stateRoutine = null;
    }


    private void StopAllEffectCoroutines()
    {
        StopStateRoutine();

        if (_boostRoutine != null)
        {
            StopCoroutine(_boostRoutine);
            _boostRoutine = null;
        }
    }

    #endregion


    #region Ease

    private static float EaseOutCubic(float t)
    {
        return
            1f -
            Mathf.Pow(
                1f - t,
                3f);
    }


    private static float EaseInCubic(float t)
    {
        return t * t * t;
    }

    #endregion


#if UNITY_EDITOR

    #region Gizmo

    private void OnDrawGizmosSelected()
    {
        RectTransform rectTransform =
            transform as RectTransform;

        if (rectTransform == null)
            return;

        DrawPointGizmo(
            rectTransform,
            core,
            new Color(
                1f,
                0.2f,
                0.2f,
                0.9f));

        DrawPointGizmo(
            rectTransform,
            eyeLeft,
            new Color(
                0.2f,
                0.8f,
                1f,
                0.9f));

        DrawPointGizmo(
            rectTransform,
            eyeRight,
            new Color(
                0.2f,
                1f,
                0.4f,
                0.9f));
    }


    private void DrawPointGizmo(
        RectTransform parentRect,
        GlowPoint point,
        Color gizmoColor)
    {
        GlowPointLayoutUtility.DrawGizmo(
            parentRect,
            point,
            gizmoColor);
    }

    #endregion

#endif


    #region GlowPoint Layout Utility

    /*
     * GlowPoint의 내부 Serialized 값을
     * 외부 public 필드로 노출하지 않으면서
     * 부모 CharacterUIGlow에서 사용할 수 있도록 하는
     * 내부 전용 Utility.
     */
    private static class GlowPointLayoutUtility
    {
        public static void Apply(
            GlowPoint point,
            RectTransform rect)
        {
            point.ApplyLayout(rect);
        }


#if UNITY_EDITOR

        public static void DrawGizmo(
            RectTransform parentRect,
            GlowPoint point,
            Color color)
        {
            point.DrawEditorGizmo(
                parentRect,
                color);
        }

#endif
    }

    #endregion
}
