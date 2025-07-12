using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class StartScreenManager : MonoBehaviour
{
    [Header("UI 요소들")]
    public Button startButton;
    public Button optionsButton;
    public Button quitButton;
    public Text gameTitle;
    public GameObject optionsPanel;
    public Slider volumeSlider;
    public Text volumeValueText;
    public AudioSource backgroundMusic;

    [Header("애니메이션 설정")]
    public float fadeInDuration = 2f;
    public float buttonAnimationDelay = 0.5f;

    private CanvasGroup canvasGroup;
    private bool isOptionsOpen = false;

    void Start()
    {
        InitializeUI();
        StartCoroutine(FadeInAnimation());
    }

    void InitializeUI()
    {
        // Canvas Group 컴포넌트 가져오기 (페이드 효과용)
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // 초기 투명도 설정
        canvasGroup.alpha = 0f;

        // 버튼 이벤트 연결
        if (startButton != null)
            startButton.onClick.AddListener(StartGame);

        if (optionsButton != null)
            optionsButton.onClick.AddListener(ToggleOptions);

        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);

        // 볼륨 슬라이더 설정
        if (volumeSlider != null)
        {
            volumeSlider.value = PlayerPrefs.GetFloat("MasterVolume", 1f);
            volumeSlider.onValueChanged.AddListener(SetVolume);
            UpdateVolumeText(volumeSlider.value);
        }

        // 옵션 패널 비활성화
        if (optionsPanel != null)
            optionsPanel.SetActive(false);

        // 배경 음악 시작
        if (backgroundMusic != null)
        {
            backgroundMusic.volume = PlayerPrefs.GetFloat("MasterVolume", 1f);
            backgroundMusic.Play();
        }
    }

    IEnumerator FadeInAnimation()
    {
        float elapsedTime = 0f;

        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeInDuration);
            yield return null;
        }

        canvasGroup.alpha = 1f;

        // 버튼 애니메이션 시작
        yield return new WaitForSeconds(buttonAnimationDelay);
        StartCoroutine(AnimateButtons());
    }

    IEnumerator AnimateButtons()
    {
        Button[] buttons = { startButton, optionsButton, quitButton };

        foreach (Button button in buttons)
        {
            if (button != null)
            {
                // 버튼 크기 애니메이션
                StartCoroutine(ButtonScaleAnimation(button.transform));
                yield return new WaitForSeconds(0.1f);
            }
        }
    }

    IEnumerator ButtonScaleAnimation(Transform buttonTransform)
    {
        Vector3 originalScale = buttonTransform.localScale;
        Vector3 targetScale = originalScale * 1.1f;

        float duration = 0.3f;
        float elapsedTime = 0f;

        // 확대
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            buttonTransform.localScale = Vector3.Lerp(originalScale, targetScale, elapsedTime / duration);
            yield return null;
        }

        elapsedTime = 0f;

        // 축소
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            buttonTransform.localScale = Vector3.Lerp(targetScale, originalScale, elapsedTime / duration);
            yield return null;
        }

        buttonTransform.localScale = originalScale;
    }

    public void StartGame()
    {
        // 게임 시작 - 다음 씬으로 이동
        StartCoroutine(LoadGameScene());
    }

    IEnumerator LoadGameScene()
    {
        // 페이드 아웃 효과
        float elapsedTime = 0f;
        float fadeOutDuration = 1f;

        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeOutDuration);
            yield return null;
        }

        // 씬 로드 (게임 씬 이름을 실제 씬 이름으로 변경해주세요)
        SceneManager.LoadScene("GameScene");
    }

    public void ToggleOptions()
    {
        isOptionsOpen = !isOptionsOpen;

        if (optionsPanel != null)
        {
            optionsPanel.SetActive(isOptionsOpen);
        }
    }

    public void SetVolume(float volume)
    {
        // 볼륨 설정 저장
        PlayerPrefs.SetFloat("MasterVolume", volume);
        PlayerPrefs.Save();

        // 배경 음악 볼륨 적용
        if (backgroundMusic != null)
        {
            backgroundMusic.volume = volume;
        }

        // 전체 오디오 볼륨 설정
        AudioListener.volume = volume;

        // 볼륨 텍스트 업데이트
        UpdateVolumeText(volume);
    }

    void UpdateVolumeText(float volume)
    {
        if (volumeValueText != null)
        {
            volumeValueText.text = Mathf.RoundToInt(volume * 100) + "%";
        }
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }

    // 키보드 입력 처리
    void Update()
    {
        // ESC 키로 옵션 패널 토글
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isOptionsOpen)
            {
                ToggleOptions();
            }
        }

        // Enter 키로 게임 시작
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (!isOptionsOpen)
            {
                StartGame();
            }
        }
    }
}