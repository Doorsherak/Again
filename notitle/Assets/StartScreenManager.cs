using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class StartScreenManager_Safe : MonoBehaviour
{
    [Header("Refs (auto find if unassigned)")]
    [SerializeField] Button startButton, optionsButton, quitButton;
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
        if (!gameTitle) gameTitle = GetComponentInChildren<TextMeshProUGUI>(true);

        // 2) ensure CanvasGroup
        rootCg = GetComponent<CanvasGroup>();
        rootCg.alpha = 0f;

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
        if (quitButton) quitButton.onClick.AddListener(QuitGame);

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
        var targetScene = ResolveSceneName();
        if (targetScene != null)
        {
            yield return ScreenFader.FadeAndLoad(targetScene, 0.9f, 0.9f);
            yield break;
        }

        Debug.LogWarning($"[StartScreen] '{sceneName}' not in Build Settings. Falling back to index 1.");
        for (float t = 0; t < 0.9f; t += Time.unscaledDeltaTime)
        {
            rootCg.alpha = Mathf.Lerp(1, 0, t / 0.9f); yield return null;
        }
        rootCg.alpha = 0f;
        SceneManager.LoadScene(1);
    }

    string ResolveSceneName()
    {
        if (string.IsNullOrEmpty(sceneName)) return null;
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            var path = SceneUtility.GetScenePathByBuildIndex(i);
            if (path.EndsWith("/" + sceneName + ".unity")) return sceneName;
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
            // Prefer one in the same scene as this manager
            EventSystem chosen = null;
            foreach (var es in existing)
            {
                if (es && es.gameObject.scene == gameObject.scene) { chosen = es; break; }
            }
            if (!chosen) chosen = existing[0];

            if (chosen && !chosen.gameObject.activeSelf) chosen.gameObject.SetActive(true);

            // Deactivate others to silence duplicate warnings
            foreach (var es in existing)
            {
                if (!es || es == chosen) continue;
                if (es.gameObject.activeSelf) es.gameObject.SetActive(false);
            }
            return;
        }

        var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        // keep scene-scoped to avoid duplicates when loading other scenes that already have one
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
