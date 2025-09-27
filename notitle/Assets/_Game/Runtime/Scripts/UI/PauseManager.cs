using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem; // 새 입력 시스템
#endif

public class PauseManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] GameObject pauseRoot;          // UI_PauseRoot (inactive on start)
    [SerializeField] GameObject firstSelected;      // Btn_Pause_Resume
    [SerializeField] CanvasGroup fadeGroup;         // 선택: 페이드용(없어도 됨)

    [Header("Options")]
    [SerializeField] string titleSceneName = "StartScreen";
    [SerializeField] bool lockCursorOnResume = true;
    [SerializeField] bool useFade = true;
    [SerializeField, Range(0.05f, 0.6f)] float fadeDuration = 0.2f;

    public bool IsPaused { get; private set; }

    float baseFixedDelta;
    GameObject lastSelected;
    Coroutine fadeCo;

    void Awake()
    {
        baseFixedDelta = Time.fixedDeltaTime;
        if (pauseRoot) pauseRoot.SetActive(false);
        if (fadeGroup) { fadeGroup.alpha = 0f; fadeGroup.interactable = false; fadeGroup.blocksRaycasts = false; }
        // 안전장치
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }

    void Update()
    {
        // 키보드/패드로 토글
        bool pressed = false;
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) pressed = true;
        if (Gamepad.current != null && (Gamepad.current.startButton.wasPressedThisFrame || Gamepad.current.selectButton.wasPressedThisFrame)) pressed = true;
#else
        if (Input.GetKeyDown(KeyCode.Escape)) pressed = true;
#endif
        if (pressed) TogglePause();
    }

    public void TogglePause()
    {
        if (IsPaused) Resume();
        else Pause();
    }

    public void Pause()
    {
        if (IsPaused) return;
        IsPaused = true;

        // 시간/오디오 정지
        Time.timeScale = 0f;
        Time.fixedDeltaTime = baseFixedDelta * Time.timeScale; // 0
        AudioListener.pause = true;

        // 커서 노출
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // UI 표시
        if (pauseRoot) pauseRoot.SetActive(true);
        if (fadeGroup && useFade) StartFade(1f, true);
        else SetCanvasInteractable(true);

        // 포커스
        if (EventSystem.current)
        {
            lastSelected = EventSystem.current.currentSelectedGameObject;
            if (firstSelected) EventSystem.current.SetSelectedGameObject(firstSelected);
        }
    }

    public void Resume()
    {
        if (!IsPaused)
        {
            if (pauseRoot) pauseRoot.SetActive(false);
            return;
        }
        IsPaused = false;

        // 시간/오디오 재개
        Time.timeScale = 1f;
        Time.fixedDeltaTime = baseFixedDelta;
        AudioListener.pause = false;

        if (lockCursorOnResume)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (fadeGroup && useFade) StartFade(0f, false);
        else
        {
            SetCanvasInteractable(false);
            if (pauseRoot) pauseRoot.SetActive(false);
        }

        if (EventSystem.current && lastSelected)
            EventSystem.current.SetSelectedGameObject(lastSelected);
    }

    void OnDestroy()
    {
        // 씬 전환 시 꼬임 방지
        Time.timeScale = 1f;
        Time.fixedDeltaTime = baseFixedDelta;
        AudioListener.pause = false;
    }

    // UI 버튼용 메서드
    public void BtnResume() => Resume();
    public void BtnRestart() =>
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    public void BtnQuitToTitle()
    {
        if (!string.IsNullOrEmpty(titleSceneName))
            SceneManager.LoadScene(titleSceneName);
    }

    // --- 내부 유틸 ---
    void SetCanvasInteractable(bool on)
    {
        if (!fadeGroup) return;
        fadeGroup.interactable = on;
        fadeGroup.blocksRaycasts = on;
    }

    void StartFade(float target, bool enableAtStart)
    {
        if (fadeCo != null) StopCoroutine(fadeCo);
        fadeCo = StartCoroutine(FadeRoutine(target, enableAtStart));
    }

    System.Collections.IEnumerator FadeRoutine(float target, bool enableAtStart)
    {
        if (enableAtStart) SetCanvasInteractable(true);
        if (!fadeGroup) yield break;

        float start = fadeGroup.alpha;
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime; // 일시정지 동안에도 진행
            fadeGroup.alpha = Mathf.Lerp(start, target, t / fadeDuration);
            yield return null;
        }
        fadeGroup.alpha = target;

        if (Mathf.Approximately(target, 0f))
        {
            SetCanvasInteractable(false);
            if (pauseRoot) pauseRoot.SetActive(false);
        }
    }
}

