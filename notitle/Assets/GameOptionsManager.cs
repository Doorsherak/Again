using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using System.Collections.Generic;

public class GameOptionsManager : MonoBehaviour
{
    [Header("Audio Settings")]
    public AudioMixer audioMixer;
    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;

    [Header("Graphics Settings")]
    public Dropdown qualityDropdown;
    public Dropdown resolutionDropdown;
    public Toggle fullscreenToggle;
    public Toggle vsyncToggle;
    public Slider fpsLimitSlider;
    public Text fpsLimitText;

    [Header("Gameplay Settings")]
    public Slider mouseSensitivitySlider;
    public Text mouseSensitivityText;
    public Toggle invertYAxisToggle;
    public Dropdown difficultyDropdown;
    public Toggle autoSaveToggle;

    [Header("UI Settings")]
    public Slider uiScaleSlider;
    public Text uiScaleText;
    public Toggle showFPSToggle;
    public Toggle showHUDToggle;

    [Header("Language Settings")]
    public Dropdown languageDropdown;

    [Header("Control Settings")]
    public Button rebindControlsButton;

    private Resolution[] resolutions;
    private string[] languages = { "�ѱ���", "English", "������", "����" };
    private string[] difficulties = { "����", "����", "�����", "�ſ� �����" };

    void Start()
    {
        InitializeOptions();
        LoadSettings();
    }

    void InitializeOptions()
    {
        // �ػ� ����
        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();
        List<string> resolutionOptions = new List<string>();
        int currentResolutionIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " x " + resolutions[i].height;
            resolutionOptions.Add(option);

            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = i;
            }
        }

        resolutionDropdown.AddOptions(resolutionOptions);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();

        // ǰ�� ����
        qualityDropdown.ClearOptions();
        qualityDropdown.AddOptions(new List<string>(QualitySettings.names));
        qualityDropdown.value = QualitySettings.GetQualityLevel();

        // ��� ����
        languageDropdown.ClearOptions();
        languageDropdown.AddOptions(new List<string>(languages));

        // ���̵� ����
        difficultyDropdown.ClearOptions();
        difficultyDropdown.AddOptions(new List<string>(difficulties));

        // �̺�Ʈ ������ �߰�
        SetupEventListeners();

        // �ʱ� �ؽ�Ʈ ������Ʈ
        UpdateFPSLimitText();
        UpdateMouseSensitivityText();
        UpdateUIScaleText();
    }

    void SetupEventListeners()
    {
        // ����� ����
        masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
        musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
        sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);

        // �׷��� ����
        qualityDropdown.onValueChanged.AddListener(SetQuality);
        resolutionDropdown.onValueChanged.AddListener(SetResolution);
        fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        vsyncToggle.onValueChanged.AddListener(SetVSync);
        fpsLimitSlider.onValueChanged.AddListener(SetFPSLimit);

        // �����÷��� ����
        mouseSensitivitySlider.onValueChanged.AddListener(SetMouseSensitivity);
        invertYAxisToggle.onValueChanged.AddListener(SetInvertYAxis);
        difficultyDropdown.onValueChanged.AddListener(SetDifficulty);
        autoSaveToggle.onValueChanged.AddListener(SetAutoSave);

        // UI ����
        uiScaleSlider.onValueChanged.AddListener(SetUIScale);
        showFPSToggle.onValueChanged.AddListener(SetShowFPS);
        showHUDToggle.onValueChanged.AddListener(SetShowHUD);

        // ��� ����
        languageDropdown.onValueChanged.AddListener(SetLanguage);

        // ��Ʈ�� ����
        rebindControlsButton.onClick.AddListener(OpenControlRebinding);
    }

    #region Audio Settings
    public void SetMasterVolume(float volume)
    {
        audioMixer.SetFloat("MasterVolume", Mathf.Log10(volume) * 20);
        PlayerPrefs.SetFloat("MasterVolume", volume);
    }

    public void SetMusicVolume(float volume)
    {
        audioMixer.SetFloat("MusicVolume", Mathf.Log10(volume) * 20);
        PlayerPrefs.SetFloat("MusicVolume", volume);
    }

    public void SetSFXVolume(float volume)
    {
        audioMixer.SetFloat("SFXVolume", Mathf.Log10(volume) * 20);
        PlayerPrefs.SetFloat("SFXVolume", volume);
    }
    #endregion

    #region Graphics Settings
    public void SetQuality(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
        PlayerPrefs.SetInt("QualityLevel", qualityIndex);
    }

    public void SetResolution(int resolutionIndex)
    {
        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
        PlayerPrefs.SetInt("ResolutionIndex", resolutionIndex);
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
    }

    public void SetVSync(bool isVSync)
    {
        QualitySettings.vSyncCount = isVSync ? 1 : 0;
        PlayerPrefs.SetInt("VSync", isVSync ? 1 : 0);
    }

    public void SetFPSLimit(float fpsLimit)
    {
        Application.targetFrameRate = (int)fpsLimit;
        PlayerPrefs.SetFloat("FPSLimit", fpsLimit);
        UpdateFPSLimitText();
    }

    void UpdateFPSLimitText()
    {
        if (fpsLimitText != null)
        {
            int fps = (int)fpsLimitSlider.value;
            fpsLimitText.text = fps == 0 ? "������" : fps.ToString();
        }
    }
    #endregion

    #region Gameplay Settings
    public void SetMouseSensitivity(float sensitivity)
    {
        PlayerPrefs.SetFloat("MouseSensitivity", sensitivity);
        UpdateMouseSensitivityText();
    }

    void UpdateMouseSensitivityText()
    {
        if (mouseSensitivityText != null)
        {
            mouseSensitivityText.text = mouseSensitivitySlider.value.ToString("F1");
        }
    }

    public void SetInvertYAxis(bool invert)
    {
        PlayerPrefs.SetInt("InvertYAxis", invert ? 1 : 0);
    }

    public void SetDifficulty(int difficultyIndex)
    {
        PlayerPrefs.SetInt("Difficulty", difficultyIndex);
    }

    public void SetAutoSave(bool autoSave)
    {
        PlayerPrefs.SetInt("AutoSave", autoSave ? 1 : 0);
    }
    #endregion

    #region UI Settings
    public void SetUIScale(float scale)
    {
        Canvas.ForceUpdateCanvases();
        PlayerPrefs.SetFloat("UIScale", scale);
        UpdateUIScaleText();
    }

    void UpdateUIScaleText()
    {
        if (uiScaleText != null)
        {
            uiScaleText.text = (uiScaleSlider.value * 100).ToString("F0") + "%";
        }
    }

    public void SetShowFPS(bool show)
    {
        PlayerPrefs.SetInt("ShowFPS", show ? 1 : 0);
    }

    public void SetShowHUD(bool show)
    {
        PlayerPrefs.SetInt("ShowHUD", show ? 1 : 0);
    }
    #endregion

    #region Language Settings
    public void SetLanguage(int languageIndex)
    {
        PlayerPrefs.SetInt("Language", languageIndex);
        // ���⼭ ���� ��� ���� ������ �����ϼ���
    }
    #endregion

    #region Control Settings
    public void OpenControlRebinding()
    {
        // Ű ���ε� UI�� ���� ����
        Debug.Log("Ű ���ε� �޴��� ���ϴ�.");
    }
    #endregion

    #region Save/Load Settings
    void LoadSettings()
    {
        // ����� ���� �ε�
        masterVolumeSlider.value = PlayerPrefs.GetFloat("MasterVolume", 0.75f);
        musicVolumeSlider.value = PlayerPrefs.GetFloat("MusicVolume", 0.75f);
        sfxVolumeSlider.value = PlayerPrefs.GetFloat("SFXVolume", 0.75f);

        // �׷��� ���� �ε�
        qualityDropdown.value = PlayerPrefs.GetInt("QualityLevel", QualitySettings.GetQualityLevel());
        resolutionDropdown.value = PlayerPrefs.GetInt("ResolutionIndex", 0);
        fullscreenToggle.isOn = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
        vsyncToggle.isOn = PlayerPrefs.GetInt("VSync", 1) == 1;
        fpsLimitSlider.value = PlayerPrefs.GetFloat("FPSLimit", 60);

        // �����÷��� ���� �ε�
        mouseSensitivitySlider.value = PlayerPrefs.GetFloat("MouseSensitivity", 1.0f);
        invertYAxisToggle.isOn = PlayerPrefs.GetInt("InvertYAxis", 0) == 1;
        difficultyDropdown.value = PlayerPrefs.GetInt("Difficulty", 1);
        autoSaveToggle.isOn = PlayerPrefs.GetInt("AutoSave", 1) == 1;

        // UI ���� �ε�
        uiScaleSlider.value = PlayerPrefs.GetFloat("UIScale", 1.0f);
        showFPSToggle.isOn = PlayerPrefs.GetInt("ShowFPS", 0) == 1;
        showHUDToggle.isOn = PlayerPrefs.GetInt("ShowHUD", 1) == 1;

        // ��� ���� �ε�
        languageDropdown.value = PlayerPrefs.GetInt("Language", 0);

        // ��� ���� ����
        ApplyAllSettings();
    }

    void ApplyAllSettings()
    {
        SetMasterVolume(masterVolumeSlider.value);
        SetMusicVolume(musicVolumeSlider.value);
        SetSFXVolume(sfxVolumeSlider.value);
        SetQuality(qualityDropdown.value);
        SetFullscreen(fullscreenToggle.isOn);
        SetVSync(vsyncToggle.isOn);
        SetFPSLimit(fpsLimitSlider.value);
        SetMouseSensitivity(mouseSensitivitySlider.value);
        SetInvertYAxis(invertYAxisToggle.isOn);
        SetDifficulty(difficultyDropdown.value);
        SetAutoSave(autoSaveToggle.isOn);
        SetUIScale(uiScaleSlider.value);
        SetShowFPS(showFPSToggle.isOn);
        SetShowHUD(showHUDToggle.isOn);
        SetLanguage(languageDropdown.value);
    }

    public void ResetToDefaults()
    {
        PlayerPrefs.DeleteAll();
        LoadSettings();
    }
    #endregion
}