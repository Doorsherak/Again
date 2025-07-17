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
        // 해상도 설정 - 중복 제거 및 정렬
        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        // 중복 해상도 제거를 위한 HashSet 사용
        HashSet<string> uniqueResolutions = new HashSet<string>();
        List<string> resolutionOptions = new List<string>();
        List<Resolution> filteredResolutions = new List<Resolution>();

        currentResolutionIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = $"{resolutions[i].width} x {resolutions[i].height}";

            // 중복 해상도 제거
            if (!uniqueResolutions.Contains(option))
            {
                uniqueResolutions.Add(option);
                resolutionOptions.Add(option);
                filteredResolutions.Add(resolutions[i]);

                // 현재 해상도와 일치하는 인덱스 찾기
                if (resolutions[i].width == Screen.currentResolution.width &&
                    resolutions[i].height == Screen.currentResolution.height)
                {
                    currentResolutionIndex = filteredResolutions.Count - 1;
                }
            }
        }

        // 필터링된 해상도 배열로 교체
        resolutions = filteredResolutions.ToArray();

        if (resolutionOptions.Count > 0)
        {
            resolutionDropdown.AddOptions(resolutionOptions);
            resolutionDropdown.value = currentResolutionIndex;
            resolutionDropdown.RefreshShownValue();
            Debug.Log($"해상도 초기화 완료. 현재 인덱스: {currentResolutionIndex}");
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
                Debug.Log($"품질 설정 초기화 완료. 현재 레벨: {QualitySettings.GetQualityLevel()}");
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
            // 저장된 인덱스가 유효한지 확인
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

        // 전체화면 설정 로드
        if (fullscreenToggle != null)
        {
            bool isFullscreen = PlayerPrefs.GetInt("Fullscreen", Screen.fullScreen ? 1 : 0) == 1;
            fullscreenToggle.isOn = isFullscreen;
            Debug.Log($"전체화면 설정 로드: {isFullscreen}");
        }

        // 품질 설정 로드
        if (qualityDropdown != null)
        {
            int qualityIndex = PlayerPrefs.GetInt("QualityLevel", QualitySettings.GetQualityLevel());
            // 저장된 인덱스가 유효한지 확인
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
        Debug.Log($"OnResolutionChanged 호출됨! 인덱스: {resolutionIndex}, 배열 길이: {resolutions?.Length}");

        if (resolutions == null)
        {
            Debug.LogError("resolutions 배열이 null입니다!");
            return;
        }

        if (resolutionIndex >= 0 && resolutionIndex < resolutions.Length)
        {
            currentResolutionIndex = resolutionIndex;
            Debug.Log($"해상도 드롭다운 값 변경: {resolutionIndex} - {resolutions[resolutionIndex].width}x{resolutions[resolutionIndex].height}");
        }
        else
        {
            Debug.LogError($"잘못된 해상도 인덱스: {resolutionIndex}");
        }
    }

    // 테스트용 메서드
    public void TestMethod()
    {
        Debug.Log("테스트 메서드가 호출되었습니다!");
    }

    public void OnFullscreenToggled(bool isFullscreen)
    {
        Debug.Log($"전체화면 토글 변경: {isFullscreen}");
    }

    public void OnQualityChanged(int qualityIndex)
    {
        if (qualityIndex >= 0 && qualityIndex < QualitySettings.names.Length)
        {
            QualitySettings.SetQualityLevel(qualityIndex);
            Debug.Log($"품질 설정이 즉시 변경되었습니다: {QualitySettings.names[qualityIndex]}");
        }
    }

    void ApplySettings()
    {
        // 설정 저장
        if (volumeSlider != null)
            PlayerPrefs.SetFloat("MasterVolume", volumeSlider.value);

        if (resolutionDropdown != null)
            PlayerPrefs.SetInt("ResolutionIndex", resolutionDropdown.value);

        if (fullscreenToggle != null)
            PlayerPrefs.SetInt("Fullscreen", fullscreenToggle.isOn ? 1 : 0);

        if (qualityDropdown != null)
            PlayerPrefs.SetInt("QualityLevel", qualityDropdown.value);

        // 해상도 및 전체화면 적용
        if (resolutionDropdown != null && fullscreenToggle != null)
        {
            int selectedIndex = resolutionDropdown.value;
            if (selectedIndex >= 0 && selectedIndex < resolutions.Length)
            {
                Resolution selectedResolution = resolutions[selectedIndex];
                Screen.SetResolution(selectedResolution.width, selectedResolution.height, fullscreenToggle.isOn);
                Debug.Log($"해상도 적용: {selectedResolution.width} x {selectedResolution.height}, 전체화면: {fullscreenToggle.isOn}");
            }
        }

        PlayerPrefs.Save();

        Debug.Log("Settings Applied!");
        ShowAppliedMessage();
    }

    void ResetSettings()
    {
        // 기본값으로 리셋
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
        // 메인 메뉴로 돌아가기
        gameObject.SetActive(false);
    }

    void ShowAppliedMessage()
    {
        // 설정 적용 완료 메시지 표시
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