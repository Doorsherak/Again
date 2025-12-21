// ScreenFader.cs ? Unity 6 / uGUI용 화면 페이드(오버레이 방식)
// 사용법 예) StartCoroutine(ScreenFader.FadeAndLoad("GameScene", 0.9f, 0.9f));

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance { get; private set; }

    [Header("Defaults")]
    [Tooltip("플레이 시작 시 검은 화면에서 서서히 등장시키려면 1, 아니라면 0")]
    [Range(0f, 1f)] public float startAlpha = 1f;
    public Color fadeColor = Color.black;
    public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Tooltip("OnSceneLoaded에서 자동 페이드 인할 기본 시간(초). startAlpha>0일 때만 사용")]
    public float defaultFadeInOnLoad = 0.9f;

    Canvas canvas;
    Image img;
    Coroutine current;

    // ====== Public API ======
    public static ScreenFader Ensure()
    {
        if (Instance == null) new GameObject("ScreenFader").AddComponent<ScreenFader>();
        return Instance;
    }

    // 전환 직전 시작 알파 강제 설정(컷-투-블랙 방지, 입력 차단 플래그 동기화)
    public void SetAlpha(float a)
    {
        if (img == null) return;
        var c = img.color;
        img.color = new Color(c.r, c.g, c.b, a);
        img.raycastTarget = a > 0.01f; // 투명하면 입력 허용
        startAlpha = a;                 // OnSceneLoaded 로직과 일치
    }

    public IEnumerator FadeOut(float dur) => FadeTo(dur, 1f);
    public IEnumerator FadeIn(float dur) => FadeTo(dur, 0f);

    public static IEnumerator FadeAndLoad(string sceneName, float outDur = 0.9f, float inDur = 0.9f)
    {
        Ensure();
        Instance.SetAlpha(0f);                     // 전환은 항상 투명에서 시작(컷-투-블랙 방지)
        yield return Instance.FadeOut(outDur);
        var op = SceneManager.LoadSceneAsync(sceneName);
        yield return op;                           // 로드 완료 대기
        if (inDur > 0f) yield return Instance.FadeIn(inDur);
    }

    public static IEnumerator FadeAndLoad(int buildIndex, float outDur = 0.9f, float inDur = 0.9f)
    {
        Ensure();
        Instance.SetAlpha(0f);
        yield return Instance.FadeOut(outDur);
        var op = SceneManager.LoadSceneAsync(buildIndex);
        yield return op;
        if (inDur > 0f) yield return Instance.FadeIn(inDur);
    }

    // ====== Lifecycle ======
    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildCanvasIfNeeded();

        // 시작 알파/입력 차단 상태 초기화
        img.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, startAlpha);
        img.raycastTarget = startAlpha > 0.01f;

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (Instance == this) { SceneManager.sceneLoaded -= OnSceneLoaded; Instance = null; }
    }

    void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        // 첫 씬 진입 등 startAlpha>0 이면 자동 페이드 인
        if (startAlpha > 0.01f) StartCoroutine(FadeIn(defaultFadeInOnLoad));
    }

    // ====== Internals ======
    void BuildCanvasIfNeeded()
    {
        if (canvas != null && img != null) return;

        // 최상위 오버레이 캔버스
        canvas = new GameObject("FaderCanvas").AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue; // 최상단
        var scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvas.gameObject.AddComponent<GraphicRaycaster>();

        // 풀스크린 이미지(입력 차단용)
        var go = new GameObject("Fader");
        go.transform.SetParent(canvas.transform, false);
        img = go.AddComponent<Image>();
        img.raycastTarget = true; // 기본값: 검은 화면일 때 입력 차단
        var rt = img.rectTransform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        img.color = fadeColor;
    }

    IEnumerator FadeTo(float dur, float targetA)
    {
        if (img == null) BuildCanvasIfNeeded();

        if (current != null) StopCoroutine(current);
        current = StartCoroutine(CoFade(dur, targetA));
        yield return current;
    }

    IEnumerator CoFade(float dur, float targetA)
    {
        float t = 0f;
        var c = img.color;
        float a0 = c.a;
        dur = Mathf.Max(0.001f, dur);

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / dur; // 타임스케일 0에서도 동작
            float a = Mathf.Lerp(a0, targetA, ease.Evaluate(Mathf.Clamp01(t)));
            img.color = new Color(c.r, c.g, c.b, a);
            yield return null;
        }

        img.color = new Color(c.r, c.g, c.b, targetA);
        img.raycastTarget = targetA > 0.01f; // 페이드 종료 후 입력 상태 정리
    }
}

