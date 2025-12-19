/* =========================  PauseManager.cs  =========================
 * Unity 6/2023+ 호환. 메뉴 씬 배열 지원, 커서 정책 일원화,
 * EventSystem 지연-단일 생성 가드, Fallback UI(폰트 분기/Dim 클릭 통과) 포함.
 */
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Serialization; // FormerlySerializedAs
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

public class PauseManager : MonoBehaviour
{
    static PauseManager s_instance;

    // ---------- References ----------
    [Header("References")]
    [SerializeField] public GameObject pauseRoot;       // UI_PauseRoot
    [SerializeField] public GameObject firstSelected;   // Btn_Pause_Resume
    [SerializeField] public CanvasGroup fadeGroup;

    // ---------- Options ----------
    [Header("Options")]
    // v3 단일 메뉴 씬 → 배열로 이관
    [FormerlySerializedAs("titleSceneName")]
    [SerializeField] string legacySingleMenuSceneName = "StartScreen";

    [SerializeField] bool useFade = true;
    [SerializeField, Range(0.05f, 0.6f)] float fadeDuration = 0.2f;

    // ---------- Resilience ----------
    [Header("Resilience")]
    [SerializeField] bool makePersistent = false;          // DontDestroyOnLoad
    [SerializeField] bool autoFindReferences = true;       // 이름으로 재결선
    [SerializeField] bool autoBuildFallbackUI = true;      // 없으면 즉석 생성
    [SerializeField] string pauseRootName = "UI_PauseRoot";
    [SerializeField] string firstSelectedName = "Btn_Pause_Resume";
    [SerializeField] bool createEventSystemIfMissing = true;
    [SerializeField] bool autoHookButtons = true;          // 버튼 이름 기반 자동 바인딩
    [SerializeField] string resumeButtonName = "Btn_Pause_Resume";
    [SerializeField] string restartButtonName = "Btn_Pause_Restart";
    [SerializeField] string quitButtonName = "Btn_Pause_MainMenu";
    [SerializeField] KeyCode legacyBackupKey = KeyCode.P;  // ESC 보조키
    [SerializeField] bool logVerbose = true;

    // ---------- Cursor & UI Safety ----------
    [Header("Cursor Policy")]
    [SerializeField] bool manageCursor = true;                 // 커서를 이 스크립트가 관리
    [SerializeField] bool lockCursorOnlyInGameplay = true;     // 게임 씬에서만 잠금
    [SerializeField] string[] menuSceneNames = new[] { "StartScreen" }; // 여러 메뉴 씬

    [Header("Input/UI Safety")]
    [SerializeField] bool preferLegacyStandaloneModule = true; // 빠른 복구용
    [SerializeField] int overlaySortingOrder = 500;            // Pause Canvas Sorting

    public bool IsPaused { get; private set; }
    float baseFixedDelta; GameObject lastSelected; Coroutine fadeCo;

    void Awake()
    {
        if (s_instance != null && s_instance != this)
        {
            if (logVerbose)
                Debug.Log($"[Pause][Awake] Duplicate PauseManager on '{gameObject.name}' ({gameObject.scene.path}). Keeping '{s_instance.gameObject.name}'.");
            Destroy(this);
            return;
        }
        s_instance = this;

        // v3 → v4 마이그레이션
        if ((menuSceneNames == null || menuSceneNames.Length == 0) &&
            !string.IsNullOrEmpty(legacySingleMenuSceneName))
            menuSceneNames = new[] { legacySingleMenuSceneName };

        if (makePersistent) DontDestroyOnLoad(gameObject);
        baseFixedDelta = Time.fixedDeltaTime;

        if (createEventSystemIfMissing) StartCoroutine(EnsureEventSystemDeferred());

        if (autoFindReferences) TryAutoWire("Awake");
        if (autoBuildFallbackUI && pauseRoot == null && IsGameplayScene()) BuildFallbackUI();
        if (autoHookButtons) HookButtons();

        if (pauseRoot) pauseRoot.SetActive(false);
        if (fadeGroup) { fadeGroup.alpha = 0; fadeGroup.interactable = false; fadeGroup.blocksRaycasts = false; }
        Time.timeScale = 1f; AudioListener.pause = false;

        SceneManager.sceneLoaded += OnSceneLoaded;
        if (logVerbose) Dump("[Awake]");
        ApplyCursorPolicy();
    }

    void OnDestroy()
    {
        var isPrimary = s_instance == this;
        if (isPrimary) s_instance = null;

        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (!isPrimary) return;

        Time.timeScale = 1f;
        Time.fixedDeltaTime = baseFixedDelta;
        AudioListener.pause = false;
    }

    void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        if (createEventSystemIfMissing) StartCoroutine(EnsureEventSystemDeferred());
        if (autoFindReferences) TryAutoWire("sceneLoaded");
        if (autoBuildFallbackUI && pauseRoot == null && !IsMenuSceneName(s.name)) BuildFallbackUI();
        if (autoHookButtons) HookButtons();
        if (logVerbose) Dump("[sceneLoaded]");
        ApplyCursorPolicy();
    }

    void Update()
    {
        // 메뉴 씬에서는 일시정지 입력을 무시하고 항상 해제 상태 유지
        if (!IsGameplayScene())
        {
            if (IsPaused) Resume();
            return;
        }
        if (IsPausePressed()) TogglePause();
    }

    bool IsPausePressed()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current; var pad = Gamepad.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame) return true;
        if (pad != null && (pad.startButton.wasPressedThisFrame || pad.selectButton.wasPressedThisFrame)) return true;
#endif
        if (Input.GetKeyDown(KeyCode.Escape)) return true;
        if (Input.GetKeyDown(legacyBackupKey)) return true;
        return false;
    }

    public void TogglePause() { if (IsPaused) Resume(); else Pause(); }

    public void Pause()
    {
        if (IsPaused) return;
        IsPaused = true;
        Time.timeScale = 0f; Time.fixedDeltaTime = baseFixedDelta * 0f; AudioListener.pause = true;

        if (!pauseRoot && autoBuildFallbackUI && IsGameplayScene()) BuildFallbackUI();
        if (pauseRoot)
        {
            pauseRoot.SetActive(true);
            if (fadeGroup && useFade) StartFade(1f, true); else SetCanvasInteractable(true);
        }
        else if (logVerbose) Debug.LogWarning("[Pause] pauseRoot 없음—시간은 정지하지만 UI는 없음.");

        if (EventSystem.current)
        {
            lastSelected = EventSystem.current.currentSelectedGameObject;
            if (firstSelected) EventSystem.current.SetSelectedGameObject(firstSelected);
        }
        ApplyCursorPolicy();
    }

    public void Resume()
    {
        if (!IsPaused) { if (pauseRoot) pauseRoot.SetActive(false); ApplyCursorPolicy(); return; }
        IsPaused = false;
        Time.timeScale = 1f; Time.fixedDeltaTime = baseFixedDelta; AudioListener.pause = false;

        if (pauseRoot)
        {
            if (fadeGroup && useFade) StartFade(0f, false);
            else { SetCanvasInteractable(false); pauseRoot.SetActive(false); }
        }
        if (EventSystem.current && lastSelected) EventSystem.current.SetSelectedGameObject(lastSelected);
        ApplyCursorPolicy();
    }

    void StartFade(float target, bool enableAtStart)
    { if (fadeCo != null) StopCoroutine(fadeCo); fadeCo = StartCoroutine(FadeRoutine(target, enableAtStart)); }

    System.Collections.IEnumerator FadeRoutine(float target, bool enableAtStart)
    {
        if (enableAtStart) SetCanvasInteractable(true); if (!fadeGroup) yield break;
        float start = fadeGroup.alpha, t = 0;
        while (t < fadeDuration) { t += Time.unscaledDeltaTime; fadeGroup.alpha = Mathf.Lerp(start, target, t / fadeDuration); yield return null; }
        fadeGroup.alpha = target;
        if (Mathf.Approximately(target, 0f)) { SetCanvasInteractable(false); if (pauseRoot) pauseRoot.SetActive(false); }
    }

    void SetCanvasInteractable(bool on)
    { if (!fadeGroup) return; fadeGroup.interactable = on; fadeGroup.blocksRaycasts = on; }

    // ---------- Cursor Policy ----------
    bool IsMenuSceneName(string sceneName)
    {
        if (menuSceneNames == null) return false;
        for (int i = 0; i < menuSceneNames.Length; i++)
        {
            var n = menuSceneNames[i];
            if (!string.IsNullOrEmpty(n) &&
                string.Equals(sceneName, n, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    bool IsGameplayScene()
        => !IsMenuSceneName(SceneManager.GetActiveScene().name);

    public void ApplyCursorPolicy()
    {
        if (!manageCursor) return;

        if (IsPaused) // 정지 중엔 항상 커서 보임
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        // 메뉴 = 보임, 게임 = 잠금
        if (lockCursorOnlyInGameplay && !IsGameplayScene())
        { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        else
        { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
    }

    // ---------- Utilities ----------
    void TryAutoWire(string tag)
    {
        if (!pauseRoot) pauseRoot = FindInLoadedScenesByName(pauseRootName);
        if (!firstSelected)
        {
            if (pauseRoot) firstSelected = FindChildByName(pauseRoot.transform, firstSelectedName);
            if (!firstSelected) firstSelected = FindInLoadedScenesByName(firstSelectedName);
        }
        if (!fadeGroup && pauseRoot) { fadeGroup = pauseRoot.GetComponent<CanvasGroup>(); }
        if (logVerbose) Debug.Log($"[Pause]{tag} Wire → UI={(pauseRoot ? pauseRoot.scene.path : "NULL")}, First={(firstSelected ? firstSelected.name : "NULL")}");
    }

    static GameObject FindInLoadedScenesByName(string objectName)
    {
        if (string.IsNullOrEmpty(objectName)) return null;

        // Fast path (활성 오브젝트)
        var active = GameObject.Find(objectName);
        if (active) return active;

        var activeScene = SceneManager.GetActiveScene();
        GameObject fallback = null;

#if UNITY_2023_1_OR_NEWER
        var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        var all = Resources.FindObjectsOfTypeAll<Transform>();
#endif
        foreach (var t in all)
        {
            if (!t || t.name != objectName) continue;
            var go = t.gameObject;
            var scene = go.scene;
            if (!scene.IsValid() || !scene.isLoaded) continue;

            if (scene == activeScene) return go;
            if (fallback == null) fallback = go;
        }

        return fallback;
    }

    static GameObject FindChildByName(Transform root, string objectName)
    {
        if (root == null || string.IsNullOrEmpty(objectName)) return null;

        var children = root.GetComponentsInChildren<Transform>(true);
        foreach (var t in children)
            if (t && t.name == objectName) return t.gameObject;

        return null;
    }

    void HookButtons()
    {
        if (!pauseRoot) return;
        var buttons = pauseRoot.GetComponentsInChildren<Button>(true);
        foreach (var b in buttons)
        {
            if (!b || string.IsNullOrEmpty(b.name)) continue;
            if (b.name == resumeButtonName)
            {
                b.onClick.RemoveListener(BtnResume); b.onClick.AddListener(BtnResume);
            }
            else if (b.name == restartButtonName)
            {
                b.onClick.RemoveListener(BtnRestart); b.onClick.AddListener(BtnRestart);
            }
            else if (b.name == quitButtonName)
            {
                b.onClick.RemoveListener(BtnQuitToTitle); b.onClick.AddListener(BtnQuitToTitle);
            }
        }
    }

    static bool _creatingES = false; // 동시 생성 가드

    System.Collections.IEnumerator EnsureEventSystemDeferred()
    {
        if (_creatingES) yield break;
        _creatingES = true;
        yield return null; // 한 프레임 대기: 씬 오브젝트 활성화 완료 후 검사

#if UNITY_2023_1_OR_NEWER
        var all = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        var all = GameObject.FindObjectsOfType<EventSystem>(true);
#endif
        if (all.Length == 0)
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            if (preferLegacyStandaloneModule) go.AddComponent<StandaloneInputModule>();
            else go.AddComponent<InputSystemUIInputModule>();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
            if (logVerbose) Debug.Log("[Pause] EventSystem 자동 생성(지연)");
        }
        else
        {
            for (int i = 1; i < all.Length; i++) Destroy(all[i].gameObject); // 1개만 유지
        }
        _creatingES = false;
    }

    void Dump(string tag)
    {
        var active = SceneManager.GetActiveScene().path;
        var ui = pauseRoot ? pauseRoot.scene.path : "NULL";
        Debug.Log($"[Pause]{tag} Active='{active}', UI='{ui}', IsPaused={IsPaused}");
    }

    [ContextMenu("Force Pause")] void ForcePauseCtx() => Pause();
    [ContextMenu("Force Resume")] void ForceResumeCtx() => Resume();
    void OnApplicationFocus(bool f) { if (f) ApplyCursorPolicy(); }

    // ---------- Buttons ----------
    public void BtnResume() => Resume();
    public void BtnRestart() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

    string GetFirstMenuScene()
    {
        if (menuSceneNames != null)
            for (int i = 0; i < menuSceneNames.Length; i++)
                if (!string.IsNullOrEmpty(menuSceneNames[i])) return menuSceneNames[i];
        return legacySingleMenuSceneName; // 최후의 보루
    }
    public void BtnQuitToTitle()
    {
        var menu = GetFirstMenuScene();
        if (!string.IsNullOrEmpty(menu)) SceneManager.LoadScene(menu);
    }

    // ---------- Fallback UI ----------
    void BuildFallbackUI()
    {
        // Canvas
        var canvasGO = new GameObject("Canvas_UI_Runtime", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = overlaySortingOrder;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // Root
        pauseRoot = new GameObject(pauseRootName, typeof(RectTransform), typeof(CanvasGroup));
        pauseRoot.transform.SetParent(canvasGO.transform, false);
        var rtRoot = (RectTransform)pauseRoot.transform; Stretch(rtRoot, 0, 0, 0, 0);
        fadeGroup = pauseRoot.GetComponent<CanvasGroup>();
        pauseRoot.SetActive(false);

        // Dim (보이되 클릭은 막지 않음)
        var dim = new GameObject("UI_Pause_Dim", typeof(RectTransform), typeof(Image));
        dim.transform.SetParent(pauseRoot.transform, false);
        var rtDim = (RectTransform)dim.transform; Stretch(rtDim, 0, 0, 0, 0);
        var imgDim = dim.GetComponent<Image>();
        imgDim.sprite = GetUISprite();
        imgDim.type = Image.Type.Simple;
        imgDim.color = new Color(0, 0, 0, 0.75f);
        imgDim.raycastTarget = false;
        dim.transform.SetAsFirstSibling();

        // Panel
        var panel = new GameObject("UI_Pause_Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        panel.transform.SetParent(pauseRoot.transform, false);
        var rtPanel = (RectTransform)panel.transform; rtPanel.sizeDelta = new Vector2(460, 0); rtPanel.anchoredPosition = Vector2.zero;
        var imgPanel = panel.GetComponent<Image>();
        imgPanel.sprite = GetUISprite();
        imgPanel.type = Image.Type.Sliced;
        imgPanel.color = new Color(0.08f, 0.09f, 0.1f, 0.92f);
        imgPanel.raycastTarget = false;

        var vlg = panel.GetComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.MiddleCenter; vlg.spacing = 14;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
        vlg.padding = new RectOffset(24, 24, 22, 22);
        var csf = panel.GetComponent<ContentSizeFitter>(); csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        CreateLabel(panel.transform, "Pause_Title", "일시정지", 32);
        firstSelected = CreateButton(panel.transform, firstSelectedName, "계속하기", BtnResume);
        CreateButton(panel.transform, "Btn_Pause_Restart", "다시 시작", BtnRestart);
        CreateButton(panel.transform, "Btn_Pause_MainMenu", "메인 메뉴", BtnQuitToTitle);

        if (logVerbose) Debug.Log("[Pause] Fallback UI 생성 완료");
    }

    GameObject CreateButton(Transform parent, string name, string label, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.sprite = GetUISprite();
        img.type = Image.Type.Sliced;
        img.color = new Color(0.16f, 0.17f, 0.2f, 0.95f);
        var btn = go.GetComponent<Button>(); btn.targetGraphic = img; btn.onClick.AddListener(onClick);
        var colors = btn.colors;
        colors.normalColor = new Color(0.16f, 0.17f, 0.2f, 0.95f);
        colors.highlightedColor = new Color(0.22f, 0.24f, 0.28f, 1f);
        colors.pressedColor = new Color(0.1f, 0.12f, 0.14f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.fadeDuration = 0.08f;
        btn.colors = colors;

        // Label(Text)
        var tgo = new GameObject("Text", typeof(RectTransform), typeof(Text));
        tgo.transform.SetParent(go.transform, false);
        var txt = tgo.GetComponent<Text>();
        txt.text = label; txt.alignment = TextAnchor.MiddleCenter; txt.fontSize = 24;
        txt.color = new Color(0.92f, 0.95f, 0.98f, 1f);
#if UNITY_6000_0_OR_NEWER
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");   // Unity 6 내장 폰트
#else
        txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
        var shadow = tgo.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.65f);
        shadow.effectDistance = new Vector2(1f, -1f);
        shadow.useGraphicAlpha = true;
        var rt = (RectTransform)tgo.transform; Stretch(rt, 0, 0, 0, 0);

        var le = go.AddComponent<LayoutElement>(); le.preferredHeight = 52;
        return go;
    }

    GameObject CreateLabel(Transform parent, string name, string label, int size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var txt = go.GetComponent<Text>();
        txt.text = label; txt.alignment = TextAnchor.MiddleCenter; txt.fontSize = size;
        txt.color = new Color(0.92f, 0.95f, 0.98f, 1f);
#if UNITY_6000_0_OR_NEWER
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");   // Unity 6 내장 폰트
#else
        txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
        var shadow = go.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.7f);
        shadow.effectDistance = new Vector2(1f, -1f);
        shadow.useGraphicAlpha = true;
        var le = go.AddComponent<LayoutElement>(); le.preferredHeight = 44;
        return go;
    }

    static Sprite GetUISprite()
    {
        // 일부 환경에서 내장 UGUI 스프라이트 경로가 누락되어 콘솔 에러가 발생할 수 있어,
        // 안전한 런타임 생성 스프라이트로 대체한다.
        return RuntimeUIFallback.GetSolidSprite();
    }

    static void Stretch(RectTransform rt, float l, float t, float r, float b)
    { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = new Vector2(l, b); rt.offsetMax = new Vector2(-r, -t); }
}
