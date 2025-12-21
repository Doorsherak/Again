using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SceneTransitioner : MonoBehaviour
{
    public static SceneTransitioner Instance { get; private set; }

    [Header("Fade Defaults")]
    [SerializeField, Range(0f, 5f)] float defaultFadeOut = 0.9f;
    [SerializeField, Range(0f, 5f)] float defaultFadeIn = 0.9f;

    [Header("Policy")]
    [SerializeField] bool unpauseOnTransition = true;
    [SerializeField] bool blockInputDuringTransition = true;

    [Header("Cursor Policy")]
    [SerializeField] bool applyCursorPolicy = true;
    [SerializeField] string[] menuSceneNames = { "StartScreen", "Options", "Credits" };
    [SerializeField] CursorLockMode menuCursorLockState = CursorLockMode.None;
    [SerializeField] bool menuCursorVisible = true;
    [SerializeField] CursorLockMode gameplayCursorLockState = CursorLockMode.Locked;
    [SerializeField] bool gameplayCursorVisible = false;

    [Header("Debug")]
    [SerializeField] bool logVerbose = false;

    CanvasGroup _blockerGroup;
    GameObject _blockerCanvasGo;
    bool _isTransitioning;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AutoCreate()
    {
        Ensure();
    }

    public static SceneTransitioner Ensure()
    {
        if (Instance != null) return Instance;

        var existing = Object.FindFirstObjectByType<SceneTransitioner>(FindObjectsInactive.Include);
        if (existing != null)
        {
            Instance = existing;
            DontDestroyOnLoad(existing.gameObject);
            if (!existing.gameObject.activeSelf) existing.gameObject.SetActive(true);
            return existing;
        }

        var go = new GameObject("SceneTransitioner");
        var inst = go.AddComponent<SceneTransitioner>();
        Instance = inst;
        DontDestroyOnLoad(go);
        return inst;
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

        EnsureBlocker();
        SceneManager.sceneLoaded += OnSceneLoaded;
        var activeSceneName = SceneManager.GetActiveScene().name;
        if (applyCursorPolicy && !string.IsNullOrEmpty(activeSceneName))
            ApplyCursorPolicyForScene(activeSceneName);
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this) Instance = null;
    }

    void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        if (_isTransitioning) return;
        if (applyCursorPolicy) ApplyCursorPolicyForScene(s.name);
    }

    public bool IsTransitioning => _isTransitioning;
    public bool AppliesCursorPolicy => applyCursorPolicy;

    public void ApplyCursorPolicyForActiveScene()
    {
        if (!applyCursorPolicy) return;
        var sn = SceneManager.GetActiveScene().name;
        if (string.IsNullOrEmpty(sn)) return;
        ApplyCursorPolicyForScene(sn);
    }

    public static IEnumerator LoadScene(string sceneNameOrPath, float fadeOutDuration = -1f, float fadeInDuration = -1f)
    {
        var inst = Ensure();
        yield return inst.StartCoroutine(inst.CoLoadScene(sceneNameOrPath, fadeOutDuration, fadeInDuration));
    }

    public static IEnumerator LoadScene(int buildIndex, float fadeOutDuration = -1f, float fadeInDuration = -1f)
    {
        var inst = Ensure();
        yield return inst.StartCoroutine(inst.CoLoadScene(buildIndex, fadeOutDuration, fadeInDuration));
    }

    IEnumerator CoLoadScene(string sceneNameOrPath, float fadeOutDuration, float fadeInDuration)
    {
        if (_isTransitioning) yield break;
        _isTransitioning = true;

        if (unpauseOnTransition)
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }

        if (blockInputDuringTransition) SetBlockerActive(true);

        float outDur = fadeOutDuration >= 0f ? fadeOutDuration : defaultFadeOut;
        float inDur = fadeInDuration >= 0f ? fadeInDuration : defaultFadeIn;

        var fader = ScreenFader.Ensure();
        fader.SetAlpha(0f);
        if (outDur > 0f) yield return fader.FadeOut(outDur);

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        string identifier = ResolveSceneIdentifier(sceneNameOrPath);
        if (string.IsNullOrEmpty(identifier))
        {
            Debug.LogError($"[SceneTransitioner] Scene not found in Build Settings: '{sceneNameOrPath}'");
            if (inDur > 0f) yield return fader.FadeIn(inDur);
            if (blockInputDuringTransition) SetBlockerActive(false);
            _isTransitioning = false;
            yield break;
        }

        var op = SceneManager.LoadSceneAsync(identifier);
        yield return op;

        if (applyCursorPolicy) ApplyCursorPolicyForScene(SceneManager.GetActiveScene().name);

        if (inDur > 0f) yield return fader.FadeIn(inDur);

        if (blockInputDuringTransition) SetBlockerActive(false);
        _isTransitioning = false;
    }

    IEnumerator CoLoadScene(int buildIndex, float fadeOutDuration, float fadeInDuration)
    {
        if (_isTransitioning) yield break;
        _isTransitioning = true;

        if (unpauseOnTransition)
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }

        if (blockInputDuringTransition) SetBlockerActive(true);

        float outDur = fadeOutDuration >= 0f ? fadeOutDuration : defaultFadeOut;
        float inDur = fadeInDuration >= 0f ? fadeInDuration : defaultFadeIn;

        var fader = ScreenFader.Ensure();
        fader.SetAlpha(0f);
        if (outDur > 0f) yield return fader.FadeOut(outDur);

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        var op = SceneManager.LoadSceneAsync(buildIndex);
        yield return op;

        if (applyCursorPolicy) ApplyCursorPolicyForScene(SceneManager.GetActiveScene().name);

        if (inDur > 0f) yield return fader.FadeIn(inDur);

        if (blockInputDuringTransition) SetBlockerActive(false);
        _isTransitioning = false;
    }

    string ResolveSceneIdentifier(string sceneNameOrPath)
    {
        if (string.IsNullOrEmpty(sceneNameOrPath)) return null;

        if (sceneNameOrPath.Contains("/") || sceneNameOrPath.Contains("\\") || sceneNameOrPath.EndsWith(".unity"))
            return sceneNameOrPath;

        string firstMatch = null;
        int matches = 0;

        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            var path = SceneUtility.GetScenePathByBuildIndex(i);
            if (string.IsNullOrEmpty(path)) continue;

            var name = System.IO.Path.GetFileNameWithoutExtension(path);
            if (!string.Equals(name, sceneNameOrPath, System.StringComparison.OrdinalIgnoreCase)) continue;

            if (firstMatch == null) firstMatch = path;
            matches++;
        }

        if (matches > 1 && logVerbose)
            Debug.LogWarning($"[SceneTransitioner] Multiple scenes named '{sceneNameOrPath}'. Loading: {firstMatch}");

        return firstMatch;
    }

    bool IsMenuSceneName(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return false;
        if (menuSceneNames == null) return false;
        for (int i = 0; i < menuSceneNames.Length; i++)
        {
            var n = menuSceneNames[i];
            if (string.IsNullOrEmpty(n)) continue;
            if (string.Equals(sceneName, n, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    void ApplyCursorPolicyForScene(string sceneName)
    {
        bool isMenu = IsMenuSceneName(sceneName);
        Cursor.lockState = isMenu ? menuCursorLockState : gameplayCursorLockState;
        Cursor.visible = isMenu ? menuCursorVisible : gameplayCursorVisible;
    }

    void EnsureBlocker()
    {
        if (_blockerGroup != null) return;

        _blockerCanvasGo = new GameObject("TransitionBlockerCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        _blockerCanvasGo.transform.SetParent(transform, false);

        var canvas = _blockerCanvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = short.MaxValue - 1;

        var scaler = _blockerCanvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        _blockerGroup = _blockerCanvasGo.AddComponent<CanvasGroup>();
        _blockerGroup.alpha = 0f;
        _blockerGroup.interactable = false;
        _blockerGroup.blocksRaycasts = false;

        var imgGo = new GameObject("Blocker", typeof(RectTransform), typeof(Image));
        imgGo.transform.SetParent(_blockerCanvasGo.transform, false);
        var img = imgGo.GetComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0f);
        img.raycastTarget = true;

        var rt = (RectTransform)imgGo.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    void SetBlockerActive(bool active)
    {
        EnsureBlocker();
        if (_blockerGroup == null) return;
        _blockerGroup.blocksRaycasts = active;
        _blockerGroup.interactable = active;
        _blockerGroup.alpha = 0f;
    }
}
