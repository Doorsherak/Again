using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class GameOptionsManager : MonoBehaviour
{
    [Header("Audio Controls")]
    public Slider volumeSlider;
    public TextMeshProUGUI volumeText; // TextMeshPro�� ����

    [Header("Resolution Controls")]
    public TMP_Dropdown resolutionDropdown; // TMP_Dropdown���� ����
    public Toggle fullscreenToggle;

    [Header("Quality Controls")]
    public TMP_Dropdown qualityDropdown; // TMP_Dropdown���� ����

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
        // �ػ� ����
        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();
        List<string> resolutionOptions = new List<string>();
        currentResolutionIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = $"{resolutions[i].width} x {resolutions[i].height}";
            resolutionOptions.Add(option);

            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = i;
            }
        }

        if (resolutionOptions.Count > 0)
        {
            resolutionDropdown.AddOptions(resolutionOptions);
            resolutionDropdown.value = currentResolutionIndex;
            resolutionDropdown.RefreshShownValue();
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
            resolutionDropdown.value = savedResolution;
        }

        // ��üȭ�� ���� �ε�
        if (fullscreenToggle != null)
        {
            bool isFullscreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
            fullscreenToggle.isOn = isFullscreen;
        }

        // ǰ�� ���� �ε�
        if (qualityDropdown != null)
        {
            int qualityIndex = PlayerPrefs.GetInt("QualityLevel", QualitySettings.GetQualityLevel());
            qualityDropdown.value = qualityIndex;
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

    void OnVolumeChanged(float value)
    {
        AudioListener.volume = value;
        UpdateVolumeText(value);
    }

    void UpdateVolumeText(float value)
    {
        volumeText.text = Mathf.Round(value * 100f) + "%";
    }

    void OnResolutionChanged(int resolutionIndex)
    {
        currentResolutionIndex = resolutionIndex;
        Resolution selectedResolution = resolutions[resolutionIndex];
        Screen.SetResolution(selectedResolution.width, selectedResolution.height, fullscreenToggle.isOn);
        Debug.Log($"�ػ󵵰� ��� ����Ǿ����ϴ�: {selectedResolution.width} x {selectedResolution.height}");
    }

    void OnFullscreenToggled(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        Debug.Log($"��üȭ�� ������ ��� ����Ǿ����ϴ�: {isFullscreen}");
    }

    void OnQualityChanged(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
        Debug.Log($"ǰ�� ������ ��� ����Ǿ����ϴ�: {QualitySettings.names[qualityIndex]}");
    }

    void ApplySettings()
    {
        // ���� ����
        PlayerPrefs.SetFloat("MasterVolume", volumeSlider.value);
        PlayerPrefs.SetInt("ResolutionIndex", resolutionDropdown.value);
        PlayerPrefs.SetInt("Fullscreen", fullscreenToggle.isOn ? 1 : 0);
        PlayerPrefs.SetInt("QualityLevel", qualityDropdown.value);

        // �ػ� �� ��üȭ�� ����
        Resolution selectedResolution = resolutions[resolutionDropdown.value];
        Screen.SetResolution(selectedResolution.width, selectedResolution.height, fullscreenToggle.isOn);
        Screen.fullScreen = fullscreenToggle.isOn; // ��üȭ�� ���� ����

        PlayerPrefs.Save();

        Debug.Log("Settings Applied!");
        // ���� ���� �Ϸ� �޽��� ǥ�� (�ɼ�)
        ShowAppliedMessage();
    }

    void ResetSettings()
    {
        // �⺻������ ����
        volumeSlider.value = 0.8f;
        resolutionDropdown.value = currentResolutionIndex;
        fullscreenToggle.isOn = true;
        qualityDropdown.value = QualitySettings.GetQualityLevel();

        // ��� ����
        AudioListener.volume = 0.8f;
        UpdateVolumeText(0.8f);

        Debug.Log("Settings Reset!");
    }

    void BackToMenu()
    {
        // ���� �޴��� ���ư���
        // �� ��ȯ �ڵ� �Ǵ� �޴� �г� Ȱ��ȭ
        gameObject.SetActive(false);
    }

    void ShowAppliedMessage()
    {
        // ���� ���� �Ϸ� �޽��� ǥ�� (UI ���� �ʿ�)
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