using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ExperimentHud : MonoBehaviour
{
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

    CanvasGroup _cg;
    Text _status;
    Text _objective;
    Text _message;
    Coroutine _messageCo;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        Build();
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

        var panel = new GameObject("HUD_Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        panel.transform.SetParent(root.transform, false);
        var panelRt = (RectTransform)panel.transform;
        panelRt.anchorMin = new Vector2(0f, 1f);
        panelRt.anchorMax = new Vector2(0f, 1f);
        panelRt.pivot = new Vector2(0f, 1f);
        panelRt.anchoredPosition = Vector2.zero;
        panelRt.sizeDelta = new Vector2(maxWidth, 0f);

        var panelImage = panel.GetComponent<Image>();
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

        _status = CreateText(panel.transform, "Status", 34);
        _objective = CreateText(panel.transform, "Objective", 28);
        _message = CreateText(panel.transform, "Message", 30);
        SetAlpha(_message, 0f);
    }

    public void SetVisible(bool visible)
    {
        if (_cg) _cg.alpha = visible ? 1f : 0f;
    }

    Text CreateText(Transform parent, string name, int size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(ContentSizeFitter));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(maxWidth, 0f);

        var txt = go.GetComponent<Text>();
        txt.text = string.Empty;
        txt.fontSize = size;
        txt.color = new Color(0.9f, 0.94f, 0.98f, 1f);
        txt.alignment = TextAnchor.UpperLeft;
        txt.horizontalOverflow = HorizontalWrapMode.Wrap;
        txt.verticalOverflow = VerticalWrapMode.Overflow;
        txt.raycastTarget = false;
#if UNITY_6000_0_OR_NEWER
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
        txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
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
}
