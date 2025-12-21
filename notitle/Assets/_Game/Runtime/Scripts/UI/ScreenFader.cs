using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance { get; private set; }

    [Header("Defaults")]
    [Range(0f, 1f)] public float startAlpha = 0f;
    public Color fadeColor = Color.black;
    public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public float defaultFadeInOnLoad = 0.9f;

    Canvas _canvas;
    Image _img;
    Coroutine _current;

    public static ScreenFader Ensure()
    {
        if (Instance != null) return Instance;

        var existing = Object.FindFirstObjectByType<ScreenFader>(FindObjectsInactive.Include);
        if (existing != null)
        {
            Instance = existing;
            DontDestroyOnLoad(existing.gameObject);
            existing.BuildCanvasIfNeeded();
            return existing;
        }

        var go = new GameObject("ScreenFader");
        DontDestroyOnLoad(go);
        return go.AddComponent<ScreenFader>();
    }

    public void SetAlpha(float a)
    {
        BuildCanvasIfNeeded();
        if (_img == null) return;

        a = Mathf.Clamp01(a);
        var c = fadeColor;
        _img.color = new Color(c.r, c.g, c.b, a);
        _img.raycastTarget = a > 0.01f;
        startAlpha = a;
    }

    public IEnumerator FadeOut(float dur) => FadeTo(dur, 1f);
    public IEnumerator FadeIn(float dur) => FadeTo(dur, 0f);

    public static IEnumerator FadeAndLoad(string sceneNameOrPath, float outDur = 0.9f, float inDur = 0.9f)
    {
        var fader = Ensure();
        fader.SetAlpha(0f);
        if (outDur > 0f) yield return fader.FadeOut(outDur);
        yield return SceneManager.LoadSceneAsync(sceneNameOrPath);
        if (inDur > 0f) yield return fader.FadeIn(inDur);
    }

    public static IEnumerator FadeAndLoad(int buildIndex, float outDur = 0.9f, float inDur = 0.9f)
    {
        var fader = Ensure();
        fader.SetAlpha(0f);
        if (outDur > 0f) yield return fader.FadeOut(outDur);
        yield return SceneManager.LoadSceneAsync(buildIndex);
        if (inDur > 0f) yield return fader.FadeIn(inDur);
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildCanvasIfNeeded();
        SetAlpha(startAlpha);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this) Instance = null;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (startAlpha > 0.01f && defaultFadeInOnLoad > 0f)
            StartCoroutine(FadeIn(defaultFadeInOnLoad));
    }

    void BuildCanvasIfNeeded()
    {
        if (_canvas != null && _img != null) return;

        var canvasGo = new GameObject("FaderCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        _canvas = canvasGo.GetComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.overrideSorting = true;
        _canvas.sortingOrder = short.MaxValue;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var imgGo = new GameObject("Fader", typeof(RectTransform), typeof(Image));
        imgGo.transform.SetParent(canvasGo.transform, false);
        _img = imgGo.GetComponent<Image>();
        _img.raycastTarget = true;
        _img.color = fadeColor;

        var rt = (RectTransform)imgGo.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    IEnumerator FadeTo(float dur, float targetA)
    {
        BuildCanvasIfNeeded();
        if (_img == null) yield break;

        if (_current != null) StopCoroutine(_current);
        _current = StartCoroutine(CoFade(Mathf.Max(0.001f, dur), Mathf.Clamp01(targetA)));
        yield return _current;
    }

    IEnumerator CoFade(float dur, float targetA)
    {
        var c = fadeColor;
        float startA = _img.color.a;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / dur;
            float k = ease != null ? ease.Evaluate(Mathf.Clamp01(t)) : Mathf.Clamp01(t);
            float a = Mathf.Lerp(startA, targetA, k);
            _img.color = new Color(c.r, c.g, c.b, a);
            yield return null;
        }

        _img.color = new Color(c.r, c.g, c.b, targetA);
        _img.raycastTarget = targetA > 0.01f;
        startAlpha = targetA;
    }
}
