using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections;

public class StartScreenManager_Safe : MonoBehaviour
{
    [Header("Refs (미할당 시 자동 탐색)")]
    [SerializeField] Button startButton, optionsButton, quitButton;
    [SerializeField] TextMeshProUGUI gameTitle;
    [SerializeField] GameObject optionsPanel;   // 내부에 CanvasGroup 사용
    [SerializeField] Slider volumeSlider;
    [SerializeField] TextMeshProUGUI volumeValueText;
    [SerializeField] AudioSource bgm;

    [Header("Scene")]
    [SerializeField] string sceneName = "GameScene"; // 빌드 세팅에 이름/인덱스 등록

    [Header("FX")]
    [Range(0.6f, 1.6f)] public float fadeInDuration = 1.0f;
    public float buttonStagger = 0.06f;

    CanvasGroup rootCg, optionsCg;
    bool isOptionsOpen;

    void Awake()
    {
        // 0) EventSystem 보장
        if (!EventSystem.current)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            DontDestroyOnLoad(es);
        }

        // 1) 자동 와이어(미할당 시만)
        if (!startButton) startButton = GameObject.Find("StartButton")?.GetComponent<Button>();
        if (!optionsButton) optionsButton = GameObject.Find("OptionsButton")?.GetComponent<Button>();
        if (!quitButton) quitButton = GameObject.Find("QuitButton")?.GetComponent<Button>();
        if (!gameTitle) gameTitle = GetComponentInChildren<TextMeshProUGUI>(true);

        // 2) CanvasGroup 보장
        rootCg = GetComponent<CanvasGroup>(); if (!rootCg) rootCg = gameObject.AddComponent<CanvasGroup>();
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

        // 볼륨 표시(저장은 OptionsManager에서)
        float v = PlayerPrefs.GetFloat("MasterVolume", 0.8f);
        if (volumeSlider) volumeSlider.SetValueWithoutNotify(v);
        if (volumeValueText) volumeValueText.text = Mathf.RoundToInt(v * 100) + "%";
        if (bgm) bgm.volume = v;

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
        StartCoroutine(CoLoadScene());
    }
    IEnumerator CoLoadScene()
    {
        // 씬 존재 검사
        bool canLoadByName = !string.IsNullOrEmpty(sceneName);
        if (canLoadByName)
        {
            bool found = false;
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                var p = SceneUtility.GetScenePathByBuildIndex(i);
                if (p.EndsWith("/" + sceneName + ".unity")) { found = true; break; }
            }
            if (!found) { Debug.LogWarning($"[StartScreen] Build Settings에 '{sceneName}'가 없습니다. 인덱스 1로 시도합니다."); canLoadByName = false; }
        }
        // 짧은 페이드 아웃
        for (float t = 0; t < 0.9f; t += Time.unscaledDeltaTime)
        {
            rootCg.alpha = Mathf.Lerp(1, 0, t / 0.9f); yield return null;
        }
        if (canLoadByName) SceneManager.LoadScene(sceneName);
        else SceneManager.LoadScene(1); // 첫 씬 다음 인덱스를 기본값으로
    }

    public void ToggleOptions(bool open)
    {
        isOptionsOpen = open; StopAllCoroutines();
        StartCoroutine(CoOptions());
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

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
