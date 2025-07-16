using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class GameOptionsManager : MonoBehaviour
{
    [Header("Audio Controls")]
    public Slider volumeSlider;
    public TextMeshProUGUI volumeText; // TextMeshPro로 변경

    [Header("Resolution Controls")]
    public TMP_Dropdown resolutionDropdown; // TMP_Dropdown으로 변경
    public Toggle fullscreenToggle;

    [Header("Quality Controls")]
    public TMP_Dropdown qualityDropdown; // TMP_Dropdown으로 변경

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
        // 해상도 설정
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
            Debug.LogError("해상도 목록이 비어 있습니다.");
        }
    }

    void InitializeQuality()
    {
        // 품질 설정 초기화
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
                Debug.LogError("품질 설정 목록이 비어 있습니다.");
            }
        }
    }

    void LoadSettings()
    {
        // 볼륨 설정 로드
        if (volumeSlider != null)
        {
            float volume = PlayerPrefs.GetFloat("MasterVolume", 0.8f);
            volumeSlider.value = volume;
            UpdateVolumeText(volume);
            AudioListener.volume = volume;
        }

        // 해상도 설정 로드
        if (resolutionDropdown != null)
        {
            int savedResolution = PlayerPrefs.GetInt("ResolutionIndex", currentResolutionIndex);
            resolutionDropdown.value = savedResolution;
        }

        // 전체화면 설정 로드
        if (fullscreenToggle != null)
        {
            bool isFullscreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
            fullscreenToggle.isOn = isFullscreen;
        }

        // 품질 설정 로드
        if (qualityDropdown != null)
        {
            int qualityIndex = PlayerPrefs.GetInt("QualityLevel", QualitySettings.GetQualityLevel());
            qualityDropdown.value = qualityIndex;
        }

        Debug.Log("LoadSettings 메서드가 호출되었습니다.");
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
        Debug.Log($"해상도가 즉시 변경되었습니다: {selectedResolution.width} x {selectedResolution.height}");
    }

    void OnFullscreenToggled(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        Debug.Log($"전체화면 설정이 즉시 변경되었습니다: {isFullscreen}");
    }

    void OnQualityChanged(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
        Debug.Log($"품질 설정이 즉시 변경되었습니다: {QualitySettings.names[qualityIndex]}");
    }

    void ApplySettings()
    {
        // 설정 저장
        PlayerPrefs.SetFloat("MasterVolume", volumeSlider.value);
        PlayerPrefs.SetInt("ResolutionIndex", resolutionDropdown.value);
        PlayerPrefs.SetInt("Fullscreen", fullscreenToggle.isOn ? 1 : 0);
        PlayerPrefs.SetInt("QualityLevel", qualityDropdown.value);

        // 해상도 및 전체화면 적용
        Resolution selectedResolution = resolutions[resolutionDropdown.value];
        Screen.SetResolution(selectedResolution.width, selectedResolution.height, fullscreenToggle.isOn);
        Screen.fullScreen = fullscreenToggle.isOn; // 전체화면 설정 적용

        PlayerPrefs.Save();

        Debug.Log("Settings Applied!");
        // 설정 적용 완료 메시지 표시 (옵션)
        ShowAppliedMessage();
    }

    void ResetSettings()
    {
        // 기본값으로 리셋
        volumeSlider.value = 0.8f;
        resolutionDropdown.value = currentResolutionIndex;
        fullscreenToggle.isOn = true;
        qualityDropdown.value = QualitySettings.GetQualityLevel();

        // 즉시 적용
        AudioListener.volume = 0.8f;
        UpdateVolumeText(0.8f);

        Debug.Log("Settings Reset!");
    }

    void BackToMenu()
    {
        // 메인 메뉴로 돌아가기
        // 씬 전환 코드 또는 메뉴 패널 활성화
        gameObject.SetActive(false);
    }

    void ShowAppliedMessage()
    {
        // 설정 적용 완료 메시지 표시 (UI 구현 필요)
        Debug.Log("Settings have been applied successfully!");
    }

    void OnDestroy()
    {
        // 이벤트 리스너 제거
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