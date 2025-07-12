using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class StartScreenManager : MonoBehaviour
{
    [Header("UI ��ҵ�")]
    public Button startButton;
    public Button optionsButton;
    public Button quitButton;
    public Text gameTitle;
    public GameObject optionsPanel;
    public Slider volumeSlider;
    public Text volumeValueText;
    public AudioSource backgroundMusic;

    [Header("�ִϸ��̼� ����")]
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
        // Canvas Group ������Ʈ �������� (���̵� ȿ����)
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // �ʱ� ���� ����
        canvasGroup.alpha = 0f;

        // ��ư �̺�Ʈ ����
        if (startButton != null)
            startButton.onClick.AddListener(StartGame);

        if (optionsButton != null)
            optionsButton.onClick.AddListener(ToggleOptions);

        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);

        // ���� �����̴� ����
        if (volumeSlider != null)
        {
            volumeSlider.value = PlayerPrefs.GetFloat("MasterVolume", 1f);
            volumeSlider.onValueChanged.AddListener(SetVolume);
            UpdateVolumeText(volumeSlider.value);
        }

        // �ɼ� �г� ��Ȱ��ȭ
        if (optionsPanel != null)
            optionsPanel.SetActive(false);

        // ��� ���� ����
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

        // ��ư �ִϸ��̼� ����
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
                // ��ư ũ�� �ִϸ��̼�
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

        // Ȯ��
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            buttonTransform.localScale = Vector3.Lerp(originalScale, targetScale, elapsedTime / duration);
            yield return null;
        }

        elapsedTime = 0f;

        // ���
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
        // ���� ���� - ���� ������ �̵�
        StartCoroutine(LoadGameScene());
    }

    IEnumerator LoadGameScene()
    {
        // ���̵� �ƿ� ȿ��
        float elapsedTime = 0f;
        float fadeOutDuration = 1f;

        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeOutDuration);
            yield return null;
        }

        // �� �ε� (���� �� �̸��� ���� �� �̸����� �������ּ���)
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
        // ���� ���� ����
        PlayerPrefs.SetFloat("MasterVolume", volume);
        PlayerPrefs.Save();

        // ��� ���� ���� ����
        if (backgroundMusic != null)
        {
            backgroundMusic.volume = volume;
        }

        // ��ü ����� ���� ����
        AudioListener.volume = volume;

        // ���� �ؽ�Ʈ ������Ʈ
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

    // Ű���� �Է� ó��
    void Update()
    {
        // ESC Ű�� �ɼ� �г� ���
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isOptionsOpen)
            {
                ToggleOptions();
            }
        }

        // Enter Ű�� ���� ����
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (!isOptionsOpen)
            {
                StartGame();
            }
        }
    }
}