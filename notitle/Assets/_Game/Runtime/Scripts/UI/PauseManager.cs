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
    [SerializeField] bool debugRaycastOnClick = true;
    [SerializeField, Range(1, 20)] int debugRaycastMaxResults = 8;

    // ---------- Cursor & UI Safety ----------
    [Header("Cursor Policy")]
    [SerializeField] bool manageCursor = true;                 // 커서를 이 스크립트가 관리
    [SerializeField] bool preferSceneTransitionerCursorPolicy = true;
    [SerializeField] bool lockCursorOnlyInGameplay = true;     // 게임 씬에서만 잠금
    [SerializeField] string[] menuSceneNames = new[] { "StartScreen" }; // 여러 메뉴 씬

    [Header("Input/UI Safety")]
    [SerializeField] bool preferLegacyStandaloneModule = true; // 빠른 복구용
    [SerializeField] int overlaySortingOrder = 1200;           // Pause Canvas Sorting
#if ENABLE_INPUT_SYSTEM
    [SerializeField] bool forceDynamicInputUpdateWhilePaused = true;
    UnityEngine.InputSystem.InputSettings.UpdateMode _savedInputUpdateMode;
    bool _hasSavedInputUpdateMode;
#endif

    public bool IsPaused { get; private set; }
    float baseFixedDelta; GameObject lastSelected; Coroutine fadeCo;
    bool pauseInputHeld;

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
        EnsurePauseCanvasPriority();

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
        ApplyInputSystemUpdateModeForPause(paused: false);
    }

    void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        if (createEventSystemIfMissing) StartCoroutine(EnsureEventSystemDeferred());
        if (autoFindReferences) TryAutoWire("sceneLoaded");
        if (autoBuildFallbackUI && pauseRoot == null && !IsMenuSceneName(s.name)) BuildFallbackUI();
        if (autoHookButtons) HookButtons();
        EnsurePauseCanvasPriority();
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

        if (IsPausePressed())
        {
            if (logVerbose) Debug.Log("[Pause] Toggle key pressed");
            TogglePause();
        }

        if (IsPaused && debugRaycastOnClick && TryGetPointerDownThisFrame(out var reason, out var pos))
            DebugRaycastAtPosition(reason, pos);
    }

    bool IsPausePressed()
    {
        bool held = IsPauseInputHeld();
        if (held)
        {
            if (pauseInputHeld) return false; // prevent double-toggle while key is held
            pauseInputHeld = true;
            return true;
        }

        pauseInputHeld = false;
        return false;
    }

    bool IsPauseInputHeld()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current; var pad = Gamepad.current;
        if (kb != null && kb.escapeKey.isPressed) return true;
        if (pad != null && (pad.startButton.isPressed || pad.selectButton.isPressed)) return true;
#endif
        if (Input.GetKey(KeyCode.Escape)) return true;
        if (Input.GetKey(legacyBackupKey)) return true;
        return false;
    }

    public void TogglePause() { if (IsPaused) Resume(); else Pause(); }

    public void Pause()
    {
        if (IsPaused) return;
        IsPaused = true;
        ApplyInputSystemUpdateModeForPause(paused: true);
        if (createEventSystemIfMissing) StartCoroutine(EnsureEventSystemDeferred());
        ConfigureEventSystemModules(EventSystem.current);
        Time.timeScale = 0f; AudioListener.pause = true;

        if (!pauseRoot && autoBuildFallbackUI && IsGameplayScene()) BuildFallbackUI();
        if (autoHookButtons) HookButtons();
        UnblockGlobalUIForPause();
        EnsurePauseCanvasPriority();
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
        if (logVerbose) Dump("[Pause]");
    }

    public void Resume()
    {
        if (!IsPaused) { if (pauseRoot) pauseRoot.SetActive(false); ApplyCursorPolicy(); return; }
        IsPaused = false;
        Time.timeScale = 1f; AudioListener.pause = false;
        ApplyInputSystemUpdateModeForPause(paused: false);

        if (pauseRoot)
        {
            if (fadeGroup && useFade) StartFade(0f, false);
            else { SetCanvasInteractable(false); pauseRoot.SetActive(false); }
        }
        if (EventSystem.current && lastSelected) EventSystem.current.SetSelectedGameObject(lastSelected);
        ApplyCursorPolicy();
        if (logVerbose) Dump("[Resume]");
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
    {
        int count = SceneManager.sceneCount;
        if (count <= 0) return true;

        for (int i = 0; i < count; i++)
        {
            var s = SceneManager.GetSceneAt(i);
            if (!s.IsValid() || !s.isLoaded) continue;
            if (!IsMenuSceneName(s.name)) return true;
        }
        return false;
    }

    public void ApplyCursorPolicy()
    {
        if (!manageCursor) return;

        if (IsPaused) // 정지 중엔 항상 커서 보임
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        if (preferSceneTransitionerCursorPolicy)
        {
            var transitioner = SceneTransitioner.Instance;
            if (transitioner != null && transitioner.AppliesCursorPolicy)
            {
                transitioner.ApplyCursorPolicyForActiveScene();
                return;
            }
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
        int hooked = 0;
        foreach (var b in buttons)
        {
            if (!b || string.IsNullOrEmpty(b.name)) continue;
            if (b.name == resumeButtonName)
            {
                b.onClick.RemoveListener(BtnResume); b.onClick.AddListener(BtnResume);
                hooked++;
            }
            else if (b.name == restartButtonName)
            {
                b.onClick.RemoveListener(BtnRestart); b.onClick.AddListener(BtnRestart);
                hooked++;
            }
            else if (b.name == quitButtonName)
            {
                b.onClick.RemoveListener(BtnQuitToTitle); b.onClick.AddListener(BtnQuitToTitle);
                hooked++;
            }
        }
        if (logVerbose && hooked > 0) Debug.Log($"[Pause] HookButtons → {hooked} bound");
    }

    static bool _creatingES = false; // 동시 생성 가드

    void ConfigureEventSystemModules(EventSystem es)
    {
        if (es == null) return;

#if ENABLE_LEGACY_INPUT_MANAGER
        if (preferLegacyStandaloneModule)
        {
            var legacy = es.GetComponent<StandaloneInputModule>();
            if (legacy == null) legacy = es.gameObject.AddComponent<StandaloneInputModule>();
            if (!legacy.enabled) legacy.enabled = true;

#if ENABLE_INPUT_SYSTEM
            var ui = es.GetComponent<InputSystemUIInputModule>();
            if (ui != null) ui.enabled = false; // legacy 모듈로만 입력 처리
#endif
            es.UpdateModules();
            return;
        }
#endif

#if ENABLE_INPUT_SYSTEM
        var inputSystem = es.GetComponent<InputSystemUIInputModule>();
        if (inputSystem == null) inputSystem = es.gameObject.AddComponent<InputSystemUIInputModule>();
        if (!inputSystem.enabled) inputSystem.enabled = true;
        if (inputSystem.actionsAsset == null)
            inputSystem.AssignDefaultActions();
        inputSystem.cursorLockBehavior = InputSystemUIInputModule.CursorLockBehavior.ScreenCenter;
#endif

        var standalone = es.GetComponent<StandaloneInputModule>();
        if (standalone != null) standalone.enabled = false;
        es.UpdateModules();
    }

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
#if ENABLE_LEGACY_INPUT_MANAGER
            if (preferLegacyStandaloneModule) go.AddComponent<StandaloneInputModule>();
            else go.AddComponent<InputSystemUIInputModule>();
#else
            go.AddComponent<InputSystemUIInputModule>();
#endif
#else
            go.AddComponent<StandaloneInputModule>();
#endif
            if (logVerbose) Debug.Log("[Pause] EventSystem 자동 생성(지연)");
            _creatingES = false;
            yield break;
        }

        var activeScene = SceneManager.GetActiveScene();
        EventSystem chosen = null;
        int bestScore = int.MinValue;
        for (int i = 0; i < all.Length; i++)
        {
            var es = all[i];
            if (!es) continue;

            int score = 0;
            if (es.isActiveAndEnabled) score += 100;
            if (es.gameObject.activeInHierarchy) score += 50;
            if (es.gameObject.scene == activeScene) score += 25;
#if ENABLE_INPUT_SYSTEM
            if (es.GetComponent<InputSystemUIInputModule>() != null) score += 10;
#endif
            if (es.GetComponent<StandaloneInputModule>() != null) score += 5;

            if (score > bestScore) { bestScore = score; chosen = es; }
        }
        if (chosen == null) chosen = all[0];

        if (chosen && !chosen.gameObject.activeSelf) chosen.gameObject.SetActive(true);
        if (chosen && !chosen.enabled) chosen.enabled = true;
        ConfigureEventSystemModules(chosen);
        if (EventSystem.current != null && EventSystem.current != chosen) EventSystem.current = chosen;

        for (int i = 0; i < all.Length; i++)
        {
            var es = all[i];
            if (!es || es == chosen) continue;
            es.enabled = false;
#if ENABLE_INPUT_SYSTEM
            var u = es.GetComponent<InputSystemUIInputModule>();
            if (u) u.enabled = false;
#endif
            var s = es.GetComponent<StandaloneInputModule>();
            if (s) s.enabled = false;
        }
        _creatingES = false;
    }

    void Dump(string tag)
    {
        var active = SceneManager.GetActiveScene().path;
        var ui = pauseRoot ? pauseRoot.scene.path : "NULL";
        string cursor = $"{Cursor.lockState}/{(Cursor.visible ? "Visible" : "Hidden")}";
        var mousePos = Input.mousePosition;
        string esPath = "NULL";
        string esModule = "NULL";
        var es = EventSystem.current;
        if (es != null)
        {
            esPath = es.gameObject.scene.path;
            esModule = "EventSystem";
#if ENABLE_INPUT_SYSTEM
            if (es.GetComponent<InputSystemUIInputModule>() != null) esModule += "+InputSystemUIInputModule";
#endif
            if (es.GetComponent<StandaloneInputModule>() != null) esModule += "+StandaloneInputModule";
        }

#if ENABLE_INPUT_SYSTEM
        string update = InputSystem.settings != null ? InputSystem.settings.updateMode.ToString() : "NULL";
        string currentModule = es != null && es.currentInputModule != null ? es.currentInputModule.GetType().Name : "NULL";
        Debug.Log($"[Pause]{tag} Active='{active}', UI='{ui}', IsPaused={IsPaused}, ES='{esPath}', Module='{esModule}', Current='{currentModule}', InputUpdate='{update}', Cursor='{cursor}', Mouse='{mousePos}'");
#else
        string currentModule = es != null && es.currentInputModule != null ? es.currentInputModule.GetType().Name : "NULL";
        Debug.Log($"[Pause]{tag} Active='{active}', UI='{ui}', IsPaused={IsPaused}, ES='{esPath}', Module='{esModule}', Current='{currentModule}', Cursor='{cursor}', Mouse='{mousePos}'");
#endif
    }

    [ContextMenu("Force Pause")] void ForcePauseCtx() => Pause();
    [ContextMenu("Force Resume")] void ForceResumeCtx() => Resume();
    void OnApplicationFocus(bool f) { if (f) ApplyCursorPolicy(); }

    // ---------- Buttons ----------
    public void BtnResume()
    {
        if (logVerbose) Debug.Log("[Pause] BtnResume");
        Resume();
    }

    public void BtnRestart()
    {
        if (logVerbose) Debug.Log("[Pause] BtnRestart");
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    string GetFirstMenuScene()
    {
        if (menuSceneNames != null)
            for (int i = 0; i < menuSceneNames.Length; i++)
                if (!string.IsNullOrEmpty(menuSceneNames[i])) return menuSceneNames[i];
        return legacySingleMenuSceneName; // 최후의 보루
    }
    public void BtnQuitToTitle()
    {
        if (logVerbose) Debug.Log("[Pause] BtnQuitToTitle");
        var menu = GetFirstMenuScene();
        if (string.IsNullOrEmpty(menu)) return;

        // Prefer unified transition path (handles name collisions via build path resolution + fade).
        if (SceneTransitioner.Instance != null)
        {
            StartCoroutine(SceneTransitioner.LoadScene(menu));
            return;
        }

        SceneManager.LoadScene(menu);
    }

#if ENABLE_INPUT_SYSTEM
    void ApplyInputSystemUpdateModeForPause(bool paused)
    {
        if (!forceDynamicInputUpdateWhilePaused) return;
        if (InputSystem.settings == null) return;

        if (paused)
        {
            if (!_hasSavedInputUpdateMode)
            {
                _savedInputUpdateMode = InputSystem.settings.updateMode;
                _hasSavedInputUpdateMode = true;
            }

            if (InputSystem.settings.updateMode != UnityEngine.InputSystem.InputSettings.UpdateMode.ProcessEventsInDynamicUpdate)
                InputSystem.settings.updateMode = UnityEngine.InputSystem.InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
        }
        else
        {
            if (!_hasSavedInputUpdateMode) return;
            InputSystem.settings.updateMode = _savedInputUpdateMode;
            _hasSavedInputUpdateMode = false;
        }
    }
#else
    void ApplyInputSystemUpdateModeForPause(bool paused) { }
#endif

    // ---------- Fallback UI ----------
    void BuildFallbackUI()
    {
        // Canvas
        var canvasGO = new GameObject("Canvas_UI_Runtime", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
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

    void EnsurePauseCanvasPriority()
    {
        if (pauseRoot == null) return;

        var canvas = pauseRoot.GetComponentInParent<Canvas>(true);
        if (canvas == null) return;

        if (!canvas.overrideSorting) canvas.overrideSorting = true;
        if (canvas.sortingOrder < overlaySortingOrder) canvas.sortingOrder = overlaySortingOrder;

        var t = canvas.transform;
        if (t != null && t.localScale.sqrMagnitude < 0.0001f)
            t.localScale = Vector3.one;

        // Ensure raycaster exists so the pause UI can be clicked.
        if (canvas.GetComponent<GraphicRaycaster>() == null)
            canvas.gameObject.AddComponent<GraphicRaycaster>();
    }

    void UnblockGlobalUIForPause()
    {
        var fader = ScreenFader.Instance;
        if (fader == null)
            fader = Object.FindFirstObjectByType<ScreenFader>(FindObjectsInactive.Include);
        if (fader != null) fader.SetAlpha(0f);

        var blockerCanvas = FindInLoadedScenesByName("TransitionBlockerCanvas");
        if (blockerCanvas == null)
        {
#if UNITY_2023_1_OR_NEWER
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            var canvases = GameObject.FindObjectsOfType<Canvas>(true);
#endif
            for (int i = 0; i < canvases.Length; i++)
            {
                var c = canvases[i];
                if (!c) continue;
                if (!c.overrideSorting) continue;
                if (c.sortingOrder != short.MaxValue - 1) continue;
                if (c.transform.Find("Blocker") == null) continue;
                blockerCanvas = c.gameObject;
                break;
            }
        }
        if (blockerCanvas == null) return;

        var cg = blockerCanvas.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.blocksRaycasts = false;
            cg.interactable = false;
        }

    }

    bool TryGetPointerDownThisFrame(out string reason, out Vector2 pos)
    {
#if ENABLE_INPUT_SYSTEM
        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            reason = "LMB(InputSystem)";
            pos = mouse.position.ReadValue();
            return true;
        }
#endif
        if (Input.GetMouseButtonDown(0))
        {
            reason = "LMB(Legacy)";
            pos = (Vector2)Input.mousePosition;
            return true;
        }

        reason = null;
        pos = default;
        return false;
    }

    void DebugRaycastAtPosition(string reason, Vector2 pos)
    {
        if (!logVerbose) return;

        var es = EventSystem.current;
        if (es == null)
        {
            Debug.LogWarning($"[Pause] Raycast({reason}) ES=NULL");
            return;
        }

        var ped = new PointerEventData(es) { position = pos };
        var results = new System.Collections.Generic.List<RaycastResult>(16);
        es.RaycastAll(ped, results);

        if (results.Count == 0)
        {
            Debug.Log($"[Pause] Raycast({reason}) hits=0 pos={pos}");
            return;
        }

        int n = Mathf.Min(debugRaycastMaxResults, results.Count);
        var msg = $"[Pause] Raycast({reason}) hits={results.Count} pos={pos}";
        for (int i = 0; i < n; i++)
        {
            var r = results[i];
            if (r.gameObject == null) continue;
            var canvas = r.gameObject.GetComponentInParent<Canvas>();
            int canvasOrder = canvas != null ? canvas.sortingOrder : int.MinValue;
            msg += $"\n  {i}: '{r.gameObject.name}' scene='{r.gameObject.scene.path}' sort={r.sortingOrder}/{canvasOrder} depth={r.depth} module='{r.module?.GetType().Name}'";
        }
        Debug.Log(msg);
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
