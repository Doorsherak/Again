using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class GameOptionsManager : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] Slider volumeSlider;
    [SerializeField] TextMeshProUGUI volumeText;

    [Header("Resolution")]
    [SerializeField] TMP_Dropdown resolutionDropdown;
    [SerializeField] Toggle fullscreenToggle;

    [Header("Quality")]
    [SerializeField] TMP_Dropdown qualityDropdown;

    [Header("Buttons")]
    [SerializeField] Button applyButton, resetButton, backButton;

    [Header("Panel")]
    [SerializeField] GameObject optionsPanel; // CanvasGroup 권장

    Resolution[] resolutions;
    int currentIdx;

    void Start()
    {
        InitResolutions();
        InitQuality();
        LoadSettings();
        HookEvents();
    }

    void InitResolutions()
    {
        if (!resolutionDropdown) { Debug.LogWarning("[Options] resolutionDropdown 미지정"); return; }

        var all = Screen.resolutions;
        var seen = new HashSet<string>();
        var labels = new List<string>();
        var filtered = new List<Resolution>();

        int wNow = Screen.width, hNow = Screen.height;
        currentIdx = 0;

        foreach (var r in all)
        {
            string key = $"{r.width}x{r.height}";
            if (seen.Add(key))
            {
                filtered.Add(r);
                labels.Add(key);
                if (r.width == wNow && r.height == hNow) currentIdx = filtered.Count - 1;
            }
        }
        resolutions = filtered.ToArray();

        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(labels);
        resolutionDropdown.value = Mathf.Clamp(currentIdx, 0, labels.Count > 0 ? labels.Count - 1 : 0);
        resolutionDropdown.RefreshShownValue();
    }

    void InitQuality()
    {
        if (!qualityDropdown) return;
        qualityDropdown.ClearOptions();
        var names = new List<string>(QualitySettings.names);
        qualityDropdown.AddOptions(names);
        qualityDropdown.value = QualitySettings.GetQualityLevel();
        qualityDropdown.RefreshShownValue();
    }

    void LoadSettings()
    {
        float vol = PlayerPrefs.GetFloat("MasterVolume", 0.8f);
        if (volumeSlider) volumeSlider.SetValueWithoutNotify(vol);
        if (volumeText) volumeText.text = $"{Mathf.RoundToInt(vol * 100)}%";
        AudioListener.volume = vol;

        if (resolutionDropdown && resolutions != null && resolutions.Length > 0)
        {
            int idx = Mathf.Clamp(PlayerPrefs.GetInt("ResolutionIndex", currentIdx), 0, resolutions.Length - 1);
            currentIdx = idx;
            resolutionDropdown.value = currentIdx;
            resolutionDropdown.RefreshShownValue();
        }

        if (fullscreenToggle)
        {
            bool fs = PlayerPrefs.GetInt("Fullscreen", Screen.fullScreen ? 1 : 0) == 1;
            fullscreenToggle.SetIsOnWithoutNotify(fs);
        }

        if (qualityDropdown)
        {
            int q = Mathf.Clamp(PlayerPrefs.GetInt("QualityLevel", QualitySettings.GetQualityLevel()), 0, QualitySettings.names.Length - 1);
            qualityDropdown.value = q;
            qualityDropdown.RefreshShownValue();
            QualitySettings.SetQualityLevel(q);
        }
    }

    void HookEvents()
    {
        if (volumeSlider) volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        if (resolutionDropdown) resolutionDropdown.onValueChanged.AddListener(i => currentIdx = i);
        if (fullscreenToggle) fullscreenToggle.onValueChanged.AddListener(_ => { });
        if (qualityDropdown) qualityDropdown.onValueChanged.AddListener(i => QualitySettings.SetQualityLevel(i));
        if (applyButton) applyButton.onClick.AddListener(ApplySettings);
        if (resetButton) resetButton.onClick.AddListener(ResetSettings);
        if (backButton) backButton.onClick.AddListener(ClosePanel);
    }

    void OnVolumeChanged(float v)
    {
        AudioListener.volume = v;
        if (volumeText) volumeText.text = $"{Mathf.RoundToInt(v * 100)}%";
    }

    void ApplySettings()
    {
        if (volumeSlider) PlayerPrefs.SetFloat("MasterVolume", volumeSlider.value);
        if (resolutionDropdown) PlayerPrefs.SetInt("ResolutionIndex", currentIdx);
        if (fullscreenToggle) PlayerPrefs.SetInt("Fullscreen", fullscreenToggle.isOn ? 1 : 0);
        if (qualityDropdown) PlayerPrefs.SetInt("QualityLevel", qualityDropdown.value);

        bool fs = fullscreenToggle ? fullscreenToggle.isOn : Screen.fullScreen;

        if (resolutions != null && currentIdx >= 0 && currentIdx < resolutions.Length)
        {
            var r = resolutions[currentIdx];
            Screen.SetResolution(r.width, r.height, fs);
        }
        else
        {
            Screen.fullScreen = fs; // 최소 반영
        }
        PlayerPrefs.Save();
    }

    void ResetSettings()
    {
        float defV = 0.8f;
        if (volumeSlider) { volumeSlider.SetValueWithoutNotify(defV); OnVolumeChanged(defV); }

        if (resolutionDropdown && resolutions != null && resolutions.Length > 0)
        {
            resolutionDropdown.value = currentIdx;
            resolutionDropdown.RefreshShownValue();
        }

        if (fullscreenToggle) fullscreenToggle.SetIsOnWithoutNotify(true);

        if (qualityDropdown)
        {
            int q = QualitySettings.GetQualityLevel();
            qualityDropdown.value = q;
            qualityDropdown.RefreshShownValue();
        }
    }

    void ClosePanel()
    {
        // 패널을 알파로 숨기고 입력 차단
        if (optionsPanel)
        {
            var cg = optionsPanel.GetComponent<CanvasGroup>();
            if (cg)
            {
                cg.interactable = false;
                cg.blocksRaycasts = false;
                cg.alpha = 0f;
            }
            optionsPanel.SetActive(true); // SetActive Off 사용 안 함(포커스 꼬임 방지)
        }

        // ✅ Unity 6 신 API: 씬 내 버튼 수집
        Button[] bs = optionsPanel
            ? optionsPanel.GetComponentsInChildren<Button>(true)
            : Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var b in bs)
        {
            if (!b) continue;
            b.interactable = true;
            if (EventSystem.current && EventSystem.current.currentSelectedGameObject == b.gameObject)
                EventSystem.current.SetSelectedGameObject(null);
        }
        if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
    }
}
