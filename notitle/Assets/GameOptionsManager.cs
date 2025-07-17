using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class GameOptionsManager : MonoBehaviour
{
    [Header("Audio Controls")]
    public Slider volumeSlider;
    public TextMeshProUGUI volumeText;

    [Header("Resolution Controls")]
    public TMP_Dropdown resolutionDropdown;
    public Toggle fullscreenToggle;

    [Header("Quality Controls")]
    public TMP_Dropdown qualityDropdown;

    [Header("Control Buttons")]
    public Button applyButton;
    public Button resetButton;
    public Button backButton;

    private Resolution[] resolutions;
    private int currentResolutionIndex = 0;

    void Start()
    {
        InitializeOptions();
        LoadSettings();
        SetupEventListeners();
    }

    void InitializeOptions()
    {
        InitializeResolutions();
        InitializeQuality();
    }

    void InitializeResolutions()
    {
        // �ػ� ���� - �ߺ� ���� �� ����
        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        // �ߺ� �ػ� ���Ÿ� ���� HashSet ���
        HashSet<string> uniqueResolutions = new HashSet<string>();
        List<string> resolutionOptions = new List<string>();
        List<Resolution> filteredResolutions = new List<Resolution>();

        currentResolutionIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = $"{resolutions[i].width} x {resolutions[i].height}";

            // �ߺ� �ػ� ����
            if (!uniqueResolutions.Contains(option))
            {
                uniqueResolutions.Add(option);
                resolutionOptions.Add(option);
                filteredResolutions.Add(resolutions[i]);

                // ���� �ػ󵵿� ��ġ�ϴ� �ε��� ã��
                if (resolutions[i].width == Screen.currentResolution.width &&
                    resolutions[i].height == Screen.currentResolution.height)
                {
                    currentResolutionIndex = filteredResolutions.Count - 1;
                }
            }
        }

        // ���͸��� �ػ� �迭�� ��ü
        resolutions = filteredResolutions.ToArray();

        if (resolutionOptions.Count > 0)
        {
            resolutionDropdown.AddOptions(resolutionOptions);
            resolutionDropdown.value = currentResolutionIndex;
            resolutionDropdown.RefreshShownValue();
            Debug.Log($"�ػ� �ʱ�ȭ �Ϸ�. ���� �ε���: {currentResolutionIndex}");
        }
        else
        {
            Debug.LogError("�ػ� ����� ��� �ֽ��ϴ�.");
        }
    }

    void InitializeQuality()
    {
        // ǰ�� ���� �ʱ�ȭ
        if (qualityDropdown != null)
        {
            qualityDropdown.ClearOptions();
            List<string> qualityOptions = new List<string>();
            string[] qualityNames = QualitySettings.names;

            if (qualityNames.Length > 0)
            {
                for (int i = 0; i < qualityNames.Length; i++)
                {
                    qualityOptions.Add(qualityNames[i]);
                }

                qualityDropdown.AddOptions(qualityOptions);
                qualityDropdown.value = QualitySettings.GetQualityLevel();
                qualityDropdown.RefreshShownValue();
                Debug.Log($"ǰ�� ���� �ʱ�ȭ �Ϸ�. ���� ����: {QualitySettings.GetQualityLevel()}");
            }
            else
            {
                Debug.LogError("ǰ�� ���� ����� ��� �ֽ��ϴ�.");
            }
        }
    }

    void LoadSettings()
    {
        // ���� ���� �ε�
        if (volumeSlider != null)
        {
            float volume = PlayerPrefs.GetFloat("MasterVolume", 0.8f);
            volumeSlider.value = volume;
            UpdateVolumeText(volume);
            AudioListener.volume = volume;
        }

        // �ػ� ���� �ε�
        if (resolutionDropdown != null)
        {
            int savedResolution = PlayerPrefs.GetInt("ResolutionIndex", currentResolutionIndex);
            // ����� �ε����� ��ȿ���� Ȯ��
            if (savedResolution >= 0 && savedResolution < resolutions.Length)
            {
                resolutionDropdown.value = savedResolution;
                currentResolutionIndex = savedResolution;
            }
            else
            {
                resolutionDropdown.value = currentResolutionIndex;
            }
            resolutionDropdown.RefreshShownValue();
        }

        // ��üȭ�� ���� �ε�
        if (fullscreenToggle != null)
        {
            bool isFullscreen = PlayerPrefs.GetInt("Fullscreen", Screen.fullScreen ? 1 : 0) == 1;
            fullscreenToggle.isOn = isFullscreen;
            Debug.Log($"��üȭ�� ���� �ε�: {isFullscreen}");
        }

        // ǰ�� ���� �ε�
        if (qualityDropdown != null)
        {
            int qualityIndex = PlayerPrefs.GetInt("QualityLevel", QualitySettings.GetQualityLevel());
            // ����� �ε����� ��ȿ���� Ȯ��
            if (qualityIndex >= 0 && qualityIndex < QualitySettings.names.Length)
            {
                qualityDropdown.value = qualityIndex;
            }
            else
            {
                qualityDropdown.value = QualitySettings.GetQualityLevel();
            }
            qualityDropdown.RefreshShownValue();
        }

        Debug.Log("LoadSettings �޼��尡 ȣ��Ǿ����ϴ�.");
    }

    void SetupEventListeners()
    {
        if (volumeSlider != null)
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        if (resolutionDropdown != null)
            resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenToggled);
        if (qualityDropdown != null)
            qualityDropdown.onValueChanged.AddListener(OnQualityChanged);

        if (applyButton != null)
            applyButton.onClick.AddListener(ApplySettings);
        if (resetButton != null)
            resetButton.onClick.AddListener(ResetSettings);
        if (backButton != null)
            backButton.onClick.AddListener(BackToMenu);

        Debug.Log("Event listeners have been set up.");
    }

    public void OnVolumeChanged(float value)
    {
        AudioListener.volume = value;
        UpdateVolumeText(value);
    }

    void UpdateVolumeText(float value)
    {
        if (volumeText != null)
        {
            volumeText.text = Mathf.Round(value * 100f) + "%";
        }
    }

    public void OnResolutionChanged(int resolutionIndex)
    {
        Debug.Log($"OnResolutionChanged ȣ���! �ε���: {resolutionIndex}, �迭 ����: {resolutions?.Length}");

        if (resolutions == null)
        {
            Debug.LogError("resolutions �迭�� null�Դϴ�!");
            return;
        }

        if (resolutionIndex >= 0 && resolutionIndex < resolutions.Length)
        {
            currentResolutionIndex = resolutionIndex;
            Debug.Log($"�ػ� ��Ӵٿ� �� ����: {resolutionIndex} - {resolutions[resolutionIndex].width}x{resolutions[resolutionIndex].height}");
        }
        else
        {
            Debug.LogError($"�߸��� �ػ� �ε���: {resolutionIndex}");
        }
    }

    // �׽�Ʈ�� �޼���
    public void TestMethod()
    {
        Debug.Log("�׽�Ʈ �޼��尡 ȣ��Ǿ����ϴ�!");
    }

    public void OnFullscreenToggled(bool isFullscreen)
    {
        Debug.Log($"��üȭ�� ��� ����: {isFullscreen}");
    }

    public void OnQualityChanged(int qualityIndex)
    {
        if (qualityIndex >= 0 && qualityIndex < QualitySettings.names.Length)
        {
            QualitySettings.SetQualityLevel(qualityIndex);
            Debug.Log($"ǰ�� ������ ��� ����Ǿ����ϴ�: {QualitySettings.names[qualityIndex]}");
        }
    }

    void ApplySettings()
    {
        // ���� ����
        if (volumeSlider != null)
            PlayerPrefs.SetFloat("MasterVolume", volumeSlider.value);

        if (resolutionDropdown != null)
            PlayerPrefs.SetInt("ResolutionIndex", resolutionDropdown.value);

        if (fullscreenToggle != null)
            PlayerPrefs.SetInt("Fullscreen", fullscreenToggle.isOn ? 1 : 0);

        if (qualityDropdown != null)
            PlayerPrefs.SetInt("QualityLevel", qualityDropdown.value);

        // �ػ� �� ��üȭ�� ����
        if (resolutionDropdown != null && fullscreenToggle != null)
        {
            int selectedIndex = resolutionDropdown.value;
            if (selectedIndex >= 0 && selectedIndex < resolutions.Length)
            {
                Resolution selectedResolution = resolutions[selectedIndex];
                Screen.SetResolution(selectedResolution.width, selectedResolution.height, fullscreenToggle.isOn);
                Debug.Log($"�ػ� ����: {selectedResolution.width} x {selectedResolution.height}, ��üȭ��: {fullscreenToggle.isOn}");
            }
        }

        PlayerPrefs.Save();

        Debug.Log("Settings Applied!");
        ShowAppliedMessage();
    }

    void ResetSettings()
    {
        // �⺻������ ����
        if (volumeSlider != null)
        {
            volumeSlider.value = 0.8f;
            AudioListener.volume = 0.8f;
            UpdateVolumeText(0.8f);
        }

        if (resolutionDropdown != null)
        {
            resolutionDropdown.value = currentResolutionIndex;
            resolutionDropdown.RefreshShownValue();
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = true;
        }

        if (qualityDropdown != null)
        {
            int defaultQuality = QualitySettings.GetQualityLevel();
            qualityDropdown.value = defaultQuality;
            qualityDropdown.RefreshShownValue();
        }

        Debug.Log("Settings Reset!");
    }

    void BackToMenu()
    {
        // ���� �޴��� ���ư���
        gameObject.SetActive(false);
    }

    void ShowAppliedMessage()
    {
        // ���� ���� �Ϸ� �޽��� ǥ��
        Debug.Log("Settings have been applied successfully!");
    }

    void OnDestroy()
    {
        // �̺�Ʈ ������ ����
        if (volumeSlider != null)
            volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
        if (resolutionDropdown != null)
            resolutionDropdown.onValueChanged.RemoveListener(OnResolutionChanged);
        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.RemoveListener(OnFullscreenToggled);
        if (qualityDropdown != null)
            qualityDropdown.onValueChanged.RemoveListener(OnQualityChanged);

        if (applyButton != null)
            applyButton.onClick.RemoveListener(ApplySettings);
        if (resetButton != null)
            resetButton.onClick.RemoveListener(ResetSettings);
        if (backButton != null)
            backButton.onClick.RemoveListener(BackToMenu);
    }
}