using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

[RequireComponent(typeof(CanvasGroup))]
public class StartScreenManager_Safe : MonoBehaviour
{
    [Header("Refs (auto find if unassigned)")]
    [SerializeField] Button startButton, optionsButton, quitButton;
    [SerializeField] Button backButton;
    [SerializeField] TextMeshProUGUI gameTitle;
    [SerializeField] GameObject optionsPanel;   // uses CanvasGroup inside
    [SerializeField] Slider volumeSlider;
    [SerializeField] TextMeshProUGUI volumeValueText;
    [SerializeField] AudioSource bgm;

    [Header("Scene")]
    [SerializeField] string sceneName = "GameScene"; // register name/index in Build Settings

    [Header("FX")]
    [Range(0.6f, 1.6f)] public float fadeInDuration = 1.0f;
    public float buttonStagger = 0.06f;
    [Header("UI SFX/FX")]
    [SerializeField] AudioSource uiAudio;
    [SerializeField] AudioClip hoverClip;
    [SerializeField] AudioClip clickClip;
    [SerializeField] bool autoAttachButtonFx = true;
    [Header("Button Theme")]
    [SerializeField] Color btnNormal = new Color(0.07f, 0.07f, 0.1f, 0.95f);
    [SerializeField] Color btnHighlight = new Color(0.14f, 0.84f, 1f, 0.9f);
    [SerializeField] Color btnPressed = new Color(0.05f, 0.45f, 0.65f, 0.9f);
    [SerializeField] Color btnDisabled = new Color(0.12f, 0.12f, 0.14f, 0.4f);
    [SerializeField] Color btnTextColor = new Color(0.85f, 0.9f, 0.95f, 1f);
    [SerializeField] float btnFadeDuration = 0.08f;
    [Header("Intro Overlay")]
    [SerializeField] CanvasGroup introOverlay; // 풀스크린 검정 + TMP 텍스트 컨테이너
    [SerializeField] TextMeshProUGUI introLine1;
    [SerializeField] TextMeshProUGUI introLine2;
    [SerializeField] TextMeshProUGUI introLine3;
    [SerializeField] float introHold = 2.5f;
    [SerializeField] float introFade = 0.7f;

    CanvasGroup rootCg, optionsCg;
    bool isOptionsOpen;
    bool isLoading;
    Coroutine optionsRoutine;

    void Awake()
    {
        // 0) ensure EventSystem (avoid duplicates across scene)
        EnsureEventSystem();

        // 1) auto wire (only when null)
        if (!startButton) startButton = GameObject.Find("StartButton")?.GetComponent<Button>();
        if (!optionsButton) optionsButton = GameObject.Find("OptionsButton")?.GetComponent<Button>();
        if (!quitButton) quitButton = GameObject.Find("QuitButton")?.GetComponent<Button>();
        if (!backButton) backButton = FindButtonByName(optionsPanel, "BackButton");
        if (!backButton) backButton = GameObject.Find("BackButton")?.GetComponent<Button>();
        if (!gameTitle) gameTitle = GetComponentInChildren<TextMeshProUGUI>(true);

        // 2) ensure CanvasGroup
        rootCg = GetComponent<CanvasGroup>();
        rootCg.alpha = 0f;

        EnsureIntroOverlay();

        if (optionsPanel)
        {
            optionsCg = optionsPanel.GetComponent<CanvasGroup>();
            if (!optionsCg) optionsCg = optionsPanel.AddComponent<CanvasGroup>();
            optionsCg.alpha = 0f; optionsCg.interactable = false; optionsCg.blocksRaycasts = false;
            optionsPanel.SetActive(true);
        }
    }

    void Start()
    {
        if (startButton) startButton.onClick.AddListener(StartGame);
        if (optionsButton) optionsButton.onClick.AddListener(() => ToggleOptions(true));
        if (backButton) backButton.onClick.AddListener(() => ToggleOptions(false));
        if (quitButton) quitButton.onClick.AddListener(QuitGame);
        ApplyButtonTheme(startButton);
        ApplyButtonTheme(optionsButton);
        ApplyButtonTheme(quitButton);
        if (autoAttachButtonFx)
        {
            AttachButtonFx(startButton);
            AttachButtonFx(optionsButton);
            AttachButtonFx(quitButton);
        }

        // volume display (saving handled by OptionsManager)
        float v = PlayerPrefs.GetFloat("MasterVolume", 0.8f);
        if (volumeSlider) volumeSlider.SetValueWithoutNotify(v);
        if (volumeValueText) volumeValueText.text = Mathf.RoundToInt(v * 100) + "%";
        if (bgm) bgm.volume = v;
        if (volumeSlider) volumeSlider.onValueChanged.AddListener(OnVolumeChanged);

        if (EventSystem.current && startButton)
            EventSystem.current.SetSelectedGameObject(startButton.gameObject);

        StartCoroutine(FadeIn());
        StartCoroutine(StaggerButtons());
    }

    IEnumerator FadeIn()
    {
        float d = fadeInDuration * Random.Range(0.8f, 1.2f);
        for (float t = 0; t < d; t += Time.unscaledDeltaTime)
        {
            rootCg.alpha = Mathf.SmoothStep(0, 1, t / d); yield return null;
        }
        rootCg.alpha = 1f;
    }
    IEnumerator StaggerButtons()
    {
        var list = new Button[] { startButton, optionsButton, quitButton };
        foreach (var b in list)
        {
            if (!b) continue;
            var tr = b.transform; Vector3 baseS = tr.localScale, to = baseS * 1.02f;
            float dur = 0.12f; for (float t = 0; t < dur; t += Time.unscaledDeltaTime) { tr.localScale = Vector3.Lerp(baseS, to, t / dur); yield return null; }
            for (float t = 0; t < dur; t += Time.unscaledDeltaTime) { tr.localScale = Vector3.Lerp(to, baseS, t / dur); yield return null; }
            yield return new WaitForSecondsRealtime(buttonStagger);
        }
    }

    public void StartGame()
    {
        if (isLoading) return;
        isLoading = true;
        SetButtonsInteractable(false);
        StartCoroutine(CoStartGame());
    }
    IEnumerator CoStartGame()
    {
        if (introOverlay)
        {
            introOverlay.gameObject.SetActive(true);
            introOverlay.alpha = 1f;
            introOverlay.interactable = true;
            introOverlay.blocksRaycasts = true;
            if (introLine1) introLine1.text = "피실험체 번호: 482";
            if (introLine2) introLine2.text = "투약 완료. 환각 반응 테스트를 시작합니다.";
            if (introLine3) introLine3.text = "생존하십시오.";
            yield return new WaitForSecondsRealtime(introHold);
            for (float t = 0; t < introFade; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.Clamp01(t / introFade);
                introOverlay.alpha = 1f - k;
                yield return null;
            }
            introOverlay.alpha = 0f;
            introOverlay.interactable = false;
            introOverlay.blocksRaycasts = false;
        }

        var targetScene = ResolveSceneName();
        if (!string.IsNullOrEmpty(targetScene))
        {
            yield return SceneTransitioner.LoadScene(targetScene, 0.9f, 0.9f);
            yield break;
        }

        Debug.LogWarning($"[StartScreen] '{sceneName}' not in Build Settings. Falling back to index 1.");
        for (float t = 0; t < 0.9f; t += Time.unscaledDeltaTime)
        {
            rootCg.alpha = Mathf.Lerp(1, 0, t / 0.9f); yield return null;
        }
        rootCg.alpha = 0f;
        yield return SceneTransitioner.LoadScene(1, 0.9f, 0.9f);
    }

    string ResolveSceneName()
    {
        if (string.IsNullOrEmpty(sceneName)) return null;
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            var path = SceneUtility.GetScenePathByBuildIndex(i);
            if (path.EndsWith("/" + sceneName + ".unity", System.StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith("\\" + sceneName + ".unity", System.StringComparison.OrdinalIgnoreCase))
                return path;
        }
        return null;
    }

    public void ToggleOptions(bool open)
    {
        isOptionsOpen = open;
        if (optionsRoutine != null) StopCoroutine(optionsRoutine);
        optionsRoutine = StartCoroutine(CoOptions());
    }
    public void ToggleOptions() { ToggleOptions(!isOptionsOpen); }

    IEnumerator CoOptions()
    {
        if (!optionsCg) yield break;
        float from = optionsCg.alpha, to = isOptionsOpen ? 1f : 0f;
        optionsCg.interactable = isOptionsOpen; optionsCg.blocksRaycasts = isOptionsOpen;
        for (float t = 0; t < 0.15f; t += Time.unscaledDeltaTime)
        {
            optionsCg.alpha = Mathf.Lerp(from, to, t / 0.15f); yield return null;
        }
        optionsCg.alpha = to;
        if (!isOptionsOpen && EventSystem.current && startButton)
            EventSystem.current.SetSelectedGameObject(startButton.gameObject);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && isOptionsOpen) ToggleOptions(false);
        if ((Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) && !isOptionsOpen)
            StartGame();
    }

    void OnVolumeChanged(float v)
    {
        if (volumeValueText) volumeValueText.text = Mathf.RoundToInt(v * 100) + "%";
        if (bgm) bgm.volume = v;
    }

    void AttachButtonFx(Button btn)
    {
        if (!btn) return;
        var fx = btn.GetComponent<HorrorButtonFx>();
        if (!fx) fx = btn.gameObject.AddComponent<HorrorButtonFx>();
        fx.audioSource = uiAudio;
        fx.hoverClip = hoverClip;
        fx.clickClip = clickClip;
    }

    void ApplyButtonTheme(Button btn)
    {
        if (!btn) return;
        var colors = btn.colors;
        colors.normalColor = btnNormal;
        colors.highlightedColor = btnHighlight;
        colors.pressedColor = btnPressed;
        colors.disabledColor = btnDisabled;
        colors.selectedColor = btnHighlight;
        colors.colorMultiplier = 1f;
        colors.fadeDuration = btnFadeDuration;
        btn.colors = colors;

        var tmp = btn.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp) tmp.color = btnTextColor;
    }

    void SetButtonsInteractable(bool value)
    {
        if (startButton) startButton.interactable = value;
        if (optionsButton) optionsButton.interactable = value;
        if (quitButton) quitButton.interactable = value;
    }

    void EnsureEventSystem()
    {
        // Find even inactive ones to avoid creating duplicates
#if UNITY_2023_1_OR_NEWER
        var existing = FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        var existing = FindObjectsOfType<EventSystem>(true);
#endif
        if (existing != null && existing.Length > 0)
        {
            EventSystem chosen = ChooseEventSystem(existing);
            if (chosen && !chosen.gameObject.activeSelf) chosen.gameObject.SetActive(true);
            ConfigureEventSystemModules(chosen);

            // Deactivate others to silence duplicate warnings
            foreach (var es in existing)
            {
                if (!es || es == chosen) continue;
                if (es.gameObject.activeSelf) es.gameObject.SetActive(false);
            }
            return;
        }

#if ENABLE_INPUT_SYSTEM
        var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
#else
        var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
#endif
        // keep scene-scoped to avoid duplicates when loading other scenes that already have one
    }

    static Button FindButtonByName(GameObject root, string name)
    {
        if (root == null || string.IsNullOrEmpty(name)) return null;
        var buttons = root.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            var b = buttons[i];
            if (b && b.name == name) return b;
        }
        return null;
    }

    EventSystem ChooseEventSystem(EventSystem[] existing)
    {
        if (existing == null || existing.Length == 0) return null;

        EventSystem chosen = null;
        int bestScore = int.MinValue;
        for (int i = 0; i < existing.Length; i++)
        {
            var es = existing[i];
            if (!es) continue;
            int score = 0;
            if (es.gameObject.scene == gameObject.scene) score += 5;
#if ENABLE_INPUT_SYSTEM
            if (es.GetComponent<InputSystemUIInputModule>() != null) score += 10;
#else
            if (es.GetComponent<StandaloneInputModule>() != null) score += 10;
#endif
            if (es.isActiveAndEnabled) score += 2;
            if (score > bestScore)
            {
                bestScore = score;
                chosen = es;
            }
        }

        return chosen ? chosen : existing[0];
    }

    void ConfigureEventSystemModules(EventSystem es)
    {
        if (es == null) return;

#if ENABLE_INPUT_SYSTEM
        var inputSystem = es.GetComponent<InputSystemUIInputModule>();
        if (inputSystem == null) inputSystem = es.gameObject.AddComponent<InputSystemUIInputModule>();
        if (!inputSystem.enabled) inputSystem.enabled = true;
        if (inputSystem.actionsAsset == null) inputSystem.AssignDefaultActions();

        var legacy = es.GetComponent<StandaloneInputModule>();
        if (legacy != null) legacy.enabled = false;
#else
        var legacy = es.GetComponent<StandaloneInputModule>();
        if (legacy == null) legacy = es.gameObject.AddComponent<StandaloneInputModule>();
        if (!legacy.enabled) legacy.enabled = true;
#endif
        es.UpdateModules();
    }

    void EnsureIntroOverlay()
    {
        if (introOverlay)
        {
            introOverlay.alpha = 0f;
            introOverlay.interactable = false;
            introOverlay.blocksRaycasts = false;
            return;
        }

        // Minimal, scene-local intro overlay (no prefab needed)
        var canvasGo = new GameObject("IntroOverlayCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 999;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        introOverlay = canvasGo.AddComponent<CanvasGroup>();
        introOverlay.alpha = 0f;
        introOverlay.interactable = false;
        introOverlay.blocksRaycasts = false;

        var bgGo = new GameObject("BG", typeof(RectTransform), typeof(Image));
        bgGo.transform.SetParent(canvasGo.transform, false);
        var bgRt = (RectTransform)bgGo.transform;
        Stretch(bgRt, 0, 0, 0, 0);
        var bgImg = bgGo.GetComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 1f);
        bgImg.raycastTarget = false;

        introLine1 = CreateIntroText(canvasGo.transform, "IntroLine1", new Vector2(80, -120), 44);
        introLine2 = CreateIntroText(canvasGo.transform, "IntroLine2", new Vector2(80, -176), 28);
        introLine3 = CreateIntroText(canvasGo.transform, "IntroLine3", new Vector2(80, -232), 34);
    }

    TextMeshProUGUI CreateIntroText(Transform parent, string name, Vector2 anchoredPos, float size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(1600, 80);

        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = string.Empty;
        tmp.fontSize = size;
        tmp.color = new Color(0.9f, 0.94f, 0.98f, 1f);
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.raycastTarget = false;
        return tmp;
    }

    static void Stretch(RectTransform rt, float l, float t, float r, float b)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(l, b);
        rt.offsetMax = new Vector2(-r, -t);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
