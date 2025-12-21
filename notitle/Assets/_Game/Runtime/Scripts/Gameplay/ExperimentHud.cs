using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ExperimentHud : MonoBehaviour
{
    public static ExperimentHud Instance { get; private set; }

    [Header("Layout")]
    [SerializeField] Vector2 padding = new Vector2(48f, 48f);
    [SerializeField] float lineSpacing = 6f;
    [SerializeField] float maxWidth = 900f;

    [Header("Style")]
    [SerializeField] bool useBackdrop = true;
    [SerializeField] Color backdropColor = new Color(0f, 0f, 0f, 0.35f);
    [SerializeField] Vector2 backdropPadding = new Vector2(18f, 12f);
    [SerializeField] bool useShadow = true;
    [SerializeField] Color shadowColor = new Color(0f, 0f, 0f, 0.85f);
    [SerializeField] Vector2 shadowDistance = new Vector2(2f, -2f);

    [Header("Message")]
    [SerializeField] float messageFadeIn = 0.08f;
    [SerializeField] float messageFadeOut = 0.25f;

    [Header("Hint")]
    [SerializeField] bool showHint = true;
    [SerializeField] float hintUpdateInterval = 0.12f;
    [SerializeField] Color hintColor = new Color(0.72f, 0.84f, 0.96f, 0.95f);

    [Header("Status")]
    [SerializeField] bool statusAtTop = false;
    [SerializeField] float statusOffset = 40f;
    [SerializeField] float statusMaxWidth = 1200f;
    [SerializeField] int statusFontSize = 46;
    [SerializeField] Color statusNormalColor = new Color(0.9f, 0.94f, 0.98f, 1f);
    [SerializeField] Color statusWarningColor = new Color(0.95f, 0.12f, 0.12f, 1f);
    [SerializeField] bool statusBlink = true;
    [SerializeField] float statusBlinkSpeed = 6f;
    [SerializeField] Vector2 statusBlinkAlphaRange = new Vector2(0.35f, 1f);
    [SerializeField] Font statusFontOverride;
    [SerializeField] string statusFontFamily = "Gungsuh";
    [SerializeField] string statusFontResource = "Fonts/WarningFont";
    [SerializeField] bool autoWarningFromObservation = true;
    [SerializeField] bool useStatusBackdrop = false;
    [SerializeField] Color statusBackdropColor = new Color(0.15f, 0f, 0f, 0.4f);
    [SerializeField] float statusBackdropHeight = 90f;

    CanvasGroup _cg;
    Text _status;
    Text _objective;
    Text _hint;
    Text _message;
    Coroutine _messageCo;
    ExperimentBootstrap _bootstrap;
    bool _statusWarning;
    Color _statusBaseColor = Color.white;
    float _hintTimer;
    Camera _hintCamera;

    static readonly string[] HintArrows = { "↑", "↗", "→", "↘", "↓", "↙", "←", "↖" };

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Build();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Build()
    {
        var canvasGo = new GameObject("Canvas_ExperimentHUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 990;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        _cg = canvasGo.AddComponent<CanvasGroup>();
        _cg.alpha = 1f;
        _cg.interactable = false;
        _cg.blocksRaycasts = false;

        var root = new GameObject("HUD_Root", typeof(RectTransform));
        root.transform.SetParent(canvasGo.transform, false);
        var rootRt = (RectTransform)root.transform;
        rootRt.anchorMin = new Vector2(0f, 1f);
        rootRt.anchorMax = new Vector2(0f, 1f);
        rootRt.pivot = new Vector2(0f, 1f);
        rootRt.anchoredPosition = new Vector2(padding.x, -padding.y);

        var statusRoot = new GameObject("HUD_StatusRoot", typeof(RectTransform));
        statusRoot.transform.SetParent(canvasGo.transform, false);
        var statusRt = (RectTransform)statusRoot.transform;
        if (statusAtTop)
        {
            statusRt.anchorMin = new Vector2(0.5f, 1f);
            statusRt.anchorMax = new Vector2(0.5f, 1f);
            statusRt.pivot = new Vector2(0.5f, 1f);
            statusRt.anchoredPosition = new Vector2(0f, -statusOffset);
        }
        else
        {
            statusRt.anchorMin = new Vector2(0.5f, 0f);
            statusRt.anchorMax = new Vector2(0.5f, 0f);
            statusRt.pivot = new Vector2(0.5f, 0f);
            statusRt.anchoredPosition = new Vector2(0f, statusOffset);
        }

        Image statusBackdrop = null;
        if (useStatusBackdrop)
        {
            var bgGo = new GameObject("Status_Backdrop", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(canvasGo.transform, false);
            var bgRt = (RectTransform)bgGo.transform;
            if (statusAtTop)
            {
                bgRt.anchorMin = new Vector2(0f, 1f);
                bgRt.anchorMax = new Vector2(1f, 1f);
                bgRt.pivot = new Vector2(0.5f, 1f);
                bgRt.anchoredPosition = new Vector2(0f, -statusOffset);
            }
            else
            {
                bgRt.anchorMin = new Vector2(0f, 0f);
                bgRt.anchorMax = new Vector2(1f, 0f);
                bgRt.pivot = new Vector2(0.5f, 0f);
                bgRt.anchoredPosition = new Vector2(0f, statusOffset);
            }
            bgRt.sizeDelta = new Vector2(0f, Mathf.Max(10f, statusBackdropHeight));

            statusBackdrop = bgGo.GetComponent<Image>();
            statusBackdrop.sprite = RuntimeUIFallback.GetSolidSprite();
            statusBackdrop.type = Image.Type.Simple;
            statusBackdrop.color = statusBackdropColor;
            statusBackdrop.raycastTarget = false;
        }

        var panel = new GameObject("HUD_Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        panel.transform.SetParent(root.transform, false);
        var panelRt = (RectTransform)panel.transform;
        panelRt.anchorMin = new Vector2(0f, 1f);
        panelRt.anchorMax = new Vector2(0f, 1f);
        panelRt.pivot = new Vector2(0f, 1f);
        panelRt.anchoredPosition = Vector2.zero;
        panelRt.sizeDelta = new Vector2(maxWidth, 0f);

        var panelImage = panel.GetComponent<Image>();
        panelImage.sprite = RuntimeUIFallback.GetSolidSprite();
        panelImage.type = Image.Type.Simple;
        panelImage.color = backdropColor;
        panelImage.raycastTarget = false;
        panelImage.enabled = useBackdrop;

        var layout = panel.GetComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.spacing = lineSpacing;
        layout.padding = new RectOffset(
            Mathf.RoundToInt(backdropPadding.x),
            Mathf.RoundToInt(backdropPadding.x),
            Mathf.RoundToInt(backdropPadding.y),
            Mathf.RoundToInt(backdropPadding.y));

        var fitter = panel.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        _statusBaseColor = statusNormalColor;
        _status = CreateText(
            statusRoot.transform,
            "Status",
            statusFontSize,
            TextAnchor.MiddleCenter,
            statusMaxWidth,
            ResolveStatusFont(),
            statusNormalColor);
        if (_status)
        {
            var statusTextRt = _status.rectTransform;
            statusTextRt.anchorMin = new Vector2(0.5f, 0.5f);
            statusTextRt.anchorMax = new Vector2(0.5f, 0.5f);
            statusTextRt.pivot = new Vector2(0.5f, 0.5f);
            statusTextRt.anchoredPosition = Vector2.zero;
        }

        _objective = CreateText(panel.transform, "Objective", 28);
        _hint = CreateText(panel.transform, "Hint", 26);
        if (_hint) _hint.color = hintColor;
        _message = CreateText(panel.transform, "Message", 30);
        SetAlpha(_message, 0f);
    }

    void Update()
    {
        if (!Application.isPlaying) return;
        if (autoWarningFromObservation)
        {
            if (_bootstrap == null)
                _bootstrap = FindFirstObjectByType<ExperimentBootstrap>(FindObjectsInactive.Include);
            if (_bootstrap != null)
                SetStatusWarning(_bootstrap.IsWatching);
        }

        UpdateStatusBlink();
        UpdateHint();
    }

    void UpdateHint()
    {
        if (_hint == null) return;
        if (!showHint) { _hint.text = string.Empty; return; }

        _hintTimer += Time.unscaledDeltaTime;
        float interval = Mathf.Max(0.02f, hintUpdateInterval);
        if (_hintTimer < interval) return;
        _hintTimer = 0f;

        if (_bootstrap == null)
            _bootstrap = FindFirstObjectByType<ExperimentBootstrap>(FindObjectsInactive.Include);
        if (_bootstrap == null || _bootstrap.IsEnding)
        {
            _hint.text = string.Empty;
            return;
        }

        if (_hintCamera == null) _hintCamera = Camera.main;
        if (_hintCamera == null)
        {
            _hint.text = string.Empty;
            return;
        }

        if (!_bootstrap.TryGetHintTarget(
                _hintCamera.transform.position,
                out var targetPos,
                out ExperimentBootstrap.HintTargetKind kind))
        {
            _hint.text = string.Empty;
            return;
        }

        Vector3 to = targetPos - _hintCamera.transform.position;
        to.y = 0f;
        float dist = to.magnitude;
        if (dist < 0.05f)
        {
            _hint.text = string.Empty;
            return;
        }

        Vector3 fwd = _hintCamera.transform.forward;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.0001f) fwd = Vector3.forward;
        fwd.Normalize();

        Vector3 dir = to / dist;
        float angle = Vector3.SignedAngle(fwd, dir, Vector3.up);
        float normalized = (angle + 360f) % 360f;
        int idx = Mathf.RoundToInt(normalized / 45f) % 8;
        string arrow = HintArrows[idx];

        string label = kind == ExperimentBootstrap.HintTargetKind.Exit
            ? "EXIT"
            : (kind == ExperimentBootstrap.HintTargetKind.Analyzer ? "ANALYZER" : "SAMPLE");

        string extra = kind switch
        {
            ExperimentBootstrap.HintTargetKind.Exit => string.Empty,
            ExperimentBootstrap.HintTargetKind.Analyzer => $" ({_bootstrap.RawSamples} to process)",
            _ => $" ({_bootstrap.RemainingSamples} left)"
        };

        _hint.text = $"{label} {arrow} {dist:0}m{extra}";
    }

    public void SetVisible(bool visible)
    {
        if (_cg) _cg.alpha = visible ? 1f : 0f;
    }

    public void SetStatusWarning(bool warning)
    {
        if (_statusWarning == warning) return;
        _statusWarning = warning;
        _statusBaseColor = warning ? statusWarningColor : statusNormalColor;
        UpdateStatusColor(1f);
    }

    Text CreateText(Transform parent, string name, int size)
    {
        return CreateText(
            parent,
            name,
            size,
            TextAnchor.UpperLeft,
            maxWidth,
            null,
            new Color(0.9f, 0.94f, 0.98f, 1f));
    }

    Text CreateText(Transform parent, string name, int size, TextAnchor alignment, float width, Font font, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(ContentSizeFitter));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(width, 0f);

        var txt = go.GetComponent<Text>();
        txt.text = string.Empty;
        txt.fontSize = size;
        txt.color = color;
        txt.alignment = alignment;
        txt.horizontalOverflow = HorizontalWrapMode.Wrap;
        txt.verticalOverflow = VerticalWrapMode.Overflow;
        txt.raycastTarget = false;
        txt.font = font != null ? font : GetFallbackFont();
        var fitter = go.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        if (useShadow)
        {
            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = shadowColor;
            shadow.effectDistance = shadowDistance;
            shadow.useGraphicAlpha = true;
        }
        return txt;
    }

    public void SetStatus(string text)
    {
        if (_status) _status.text = text;
    }

    public void SetObjective(string text)
    {
        if (_objective) _objective.text = text;
    }

    public void ShowMessage(string text, float seconds)
    {
        if (!_message) return;
        if (_messageCo != null) StopCoroutine(_messageCo);
        _messageCo = StartCoroutine(CoMessage(text, seconds));
    }

    IEnumerator CoMessage(string text, float seconds)
    {
        _message.text = text;
        float fadeIn = Mathf.Max(0.01f, messageFadeIn);
        float fadeOut = Mathf.Max(0.01f, messageFadeOut);
        float startAlpha = _message.color.a;
        yield return FadeText(_message, startAlpha, 1f, fadeIn);
        if (seconds > 0f) yield return new WaitForSecondsRealtime(seconds);
        yield return FadeText(_message, 1f, 0f, fadeOut);
        _messageCo = null;
    }

    static IEnumerator FadeText(Text txt, float from, float to, float duration)
    {
        if (!txt) yield break;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);
            SetAlpha(txt, Mathf.Lerp(from, to, k));
            yield return null;
        }
        SetAlpha(txt, to);
    }

    static void SetAlpha(Text txt, float a)
    {
        if (!txt) return;
        var c = txt.color;
        c.a = a;
        txt.color = c;
    }

    Font ResolveStatusFont()
    {
        if (statusFontOverride != null) return statusFontOverride;
        if (!string.IsNullOrEmpty(statusFontResource))
        {
            var resourceFont = Resources.Load<Font>(statusFontResource);
            if (resourceFont != null) return resourceFont;
        }

        if (!string.IsNullOrEmpty(statusFontFamily))
        {
            var osFont = Font.CreateDynamicFontFromOSFont(statusFontFamily, statusFontSize);
            if (osFont != null) return osFont;
        }

        return GetFallbackFont();
    }

    static Font GetFallbackFont()
    {
#if UNITY_6000_0_OR_NEWER
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
        return Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
    }

    void UpdateStatusBlink()
    {
        if (_status == null) return;
        if (!statusBlink || !_statusWarning)
        {
            UpdateStatusColor(1f);
            return;
        }

        float pulse = (Mathf.Sin(Time.unscaledTime * statusBlinkSpeed) + 1f) * 0.5f;
        float min = Mathf.Min(statusBlinkAlphaRange.x, statusBlinkAlphaRange.y);
        float max = Mathf.Max(statusBlinkAlphaRange.x, statusBlinkAlphaRange.y);
        float alpha = Mathf.Lerp(min, max, pulse);
        UpdateStatusColor(alpha);
    }

    void UpdateStatusColor(float alpha)
    {
        if (_status == null) return;
        var c = _statusBaseColor;
        c.a = alpha;
        _status.color = c;
    }
}
