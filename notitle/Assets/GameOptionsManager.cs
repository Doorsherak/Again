using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class GameOptionsManager : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] Slider volumeSlider;
    [SerializeField] TextMeshProUGUI volumeText; // "80%" 같은 표시(없으면 비워둬도 됨)

    [Header("Resolution / Fullscreen")]
    [SerializeField] TMP_Dropdown resolutionDropdown;
    [SerializeField] Toggle fullscreenToggle;

    [Header("Quality")]
    [SerializeField] TMP_Dropdown qualityDropdown;

    [Header("Buttons")]
    [SerializeField] Button applyButton;
    [SerializeField] Button resetButton;
    [SerializeField] Button backButton;

    [Header("Panel (선택)")]
    [SerializeField] GameObject optionsPanel; // CanvasGroup 사용 권장

    // 내부 상태
    List<Resolution> resList = new List<Resolution>(); // 드롭다운과 1:1 매핑
    int currentIdx = 0;

    // 테스트 시 전환이 눈에 보이도록(완성 단계에서는 false 권장)
    [SerializeField] bool useExclusiveFullScreenForTesting = true;

    // 연속 적용 방지용
    Coroutine applyRoutine;

    void Start()
    {
        InitResolutions();
        InitQuality();
        LoadSettings();
        HookEvents();
    }

    // --- 해상도 목록 구성(중복 제거) ---
    void InitResolutions()
    {
        if (!resolutionDropdown) return;

        resList.Clear();
        var labels = new List<string>();
        var seen = new HashSet<string>();

        int wNow = Screen.width, hNow = Screen.height;

        foreach (var r in Screen.resolutions)
        {
#if UNITY_6000_0_OR_NEWER || UNITY_2022_2_OR_NEWER
            var rr = r.refreshRateRatio; // RefreshRate(분수)
            int hz = Mathf.RoundToInt((float)rr.numerator / rr.denominator);
#else
            int hz = r.refreshRate;
#endif
            string key = $"{r.width}x{r.height}@{hz}";
            if (seen.Add(key))
            {
                resList.Add(r);
                labels.Add($"{r.width}×{r.height}  {hz}Hz");
                if (r.width == wNow && r.height == hNow) currentIdx = resList.Count - 1;
            }
        }

        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(labels);
        resolutionDropdown.SetValueWithoutNotify(Mathf.Clamp(currentIdx, 0, Mathf.Max(0, resList.Count - 1)));
        resolutionDropdown.RefreshShownValue();

        // 드롭다운 선택 시 인덱스 갱신
        resolutionDropdown.onValueChanged.AddListener(i => currentIdx = i);
    }

    void InitQuality()
    {
        if (!qualityDropdown) return;
        qualityDropdown.ClearOptions();
        qualityDropdown.AddOptions(new List<string>(QualitySettings.names));
        qualityDropdown.value = QualitySettings.GetQualityLevel();
        qualityDropdown.RefreshShownValue();
    }

    void LoadSettings()
    {
        // 볼륨
        float vol = PlayerPrefs.GetFloat("MasterVolume", 0.8f);
        if (volumeSlider) volumeSlider.SetValueWithoutNotify(vol);
        if (volumeText) volumeText.text = $"{Mathf.RoundToInt(vol * 100)}%";
        AudioListener.volume = vol;

        // 해상도 인덱스
        if (resolutionDropdown && resList.Count > 0)
        {
            int savedIdx = Mathf.Clamp(PlayerPrefs.GetInt("ResolutionIndex", currentIdx), 0, resList.Count - 1);
            currentIdx = savedIdx;
            resolutionDropdown.SetValueWithoutNotify(currentIdx);
            resolutionDropdown.RefreshShownValue();
        }

        // 전체화면
        if (fullscreenToggle)
        {
            bool fs = PlayerPrefs.GetInt("Fullscreen", Screen.fullScreen ? 1 : 0) == 1;
            fullscreenToggle.SetIsOnWithoutNotify(fs);
        }

        // 품질
        if (qualityDropdown)
        {
            int q = Mathf.Clamp(PlayerPrefs.GetInt("QualityLevel", QualitySettings.GetQualityLevel()),
                                0, QualitySettings.names.Length - 1);
            qualityDropdown.value = q;
            qualityDropdown.RefreshShownValue();
            QualitySettings.SetQualityLevel(q);
        }
    }

    void HookEvents()
    {
        if (volumeSlider)
        {
            volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }
        if (qualityDropdown)
        {
            // Start에서 한 번만 등록되므로 Remove는 생략
            qualityDropdown.onValueChanged.AddListener(i => QualitySettings.SetQualityLevel(i));
        }
        if (applyButton)
        {
            applyButton.onClick.RemoveListener(ApplySettings);
            applyButton.onClick.AddListener(ApplySettings);
        }
        if (resetButton)
        {
            resetButton.onClick.RemoveListener(ResetSettings);
            resetButton.onClick.AddListener(ResetSettings);
        }
        if (backButton)
        {
            backButton.onClick.RemoveListener(ClosePanel);
            backButton.onClick.AddListener(ClosePanel);
        }
    }

    void OnDestroy()
    {
        // 씬 전환 시 중복 바인딩 누적 방지
        if (volumeSlider) volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
        if (applyButton) applyButton.onClick.RemoveListener(ApplySettings);
        if (resetButton) resetButton.onClick.RemoveListener(ResetSettings);
        if (backButton) backButton.onClick.RemoveListener(ClosePanel);
    }

    void OnVolumeChanged(float v)
    {
        AudioListener.volume = v;
        if (volumeText) volumeText.text = $"{Mathf.RoundToInt(v * 100)}%";
    }

    // === 적용 ===
    public void ApplySettings()
    {
        // --- 저장(그대로 유지) ---
        if (volumeSlider) PlayerPrefs.SetFloat("MasterVolume", volumeSlider.value);
        if (resolutionDropdown) PlayerPrefs.SetInt("ResolutionIndex", Mathf.Clamp(currentIdx, 0, Mathf.Max(0, resList.Count - 1)));
        if (fullscreenToggle) PlayerPrefs.SetInt("Fullscreen", fullscreenToggle.isOn ? 1 : 0);
        if (qualityDropdown) PlayerPrefs.SetInt("QualityLevel", qualityDropdown.value);
        PlayerPrefs.Save();

        // 즉시 반영되는 것들(볼륨/품질)
        if (volumeSlider) AudioListener.volume = volumeSlider.value;
        if (qualityDropdown) QualitySettings.SetQualityLevel(qualityDropdown.value);

        // --- 해상도·전체화면은 다음 프레임에 적용 ---
        bool fsOn = fullscreenToggle ? fullscreenToggle.isOn : Screen.fullScreen;

        if (applyRoutine != null) StopCoroutine(applyRoutine);
        applyRoutine = StartCoroutine(ApplyResolutionCo(fsOn));
    }

    // === 초기값 ===
    public void ResetSettings()
    {
        // 볼륨
        float defV = 0.8f;
        if (volumeSlider) { volumeSlider.SetValueWithoutNotify(defV); OnVolumeChanged(defV); }

        // 해상도: 현재 모니터 기본에 맞춤
        if (resolutionDropdown && resList.Count > 0)
        {
            int cur = resList.FindIndex(r => r.width == Screen.currentResolution.width && r.height == Screen.currentResolution.height);
            if (cur < 0) cur = resList.FindIndex(r => r.width == Screen.width && r.height == Screen.height);
            currentIdx = Mathf.Clamp(cur, 0, resList.Count - 1);
            resolutionDropdown.SetValueWithoutNotify(currentIdx);
            resolutionDropdown.RefreshShownValue();
        }

        // 전체화면 ON
        if (fullscreenToggle) fullscreenToggle.SetIsOnWithoutNotify(true);

        // 품질: 현재 값 유지
        if (qualityDropdown)
        {
            int q = QualitySettings.GetQualityLevel();
            qualityDropdown.value = q;
            qualityDropdown.RefreshShownValue();
        }
    }

    // === 패널 닫기(선택) ===
    public void ClosePanel()
    {
        if (!optionsPanel) return;

        var cg = optionsPanel.GetComponent<CanvasGroup>();
        if (cg)
        {
            cg.interactable = false;
            cg.blocksRaycasts = false;
            cg.alpha = 0f;
            optionsPanel.SetActive(true); // SetActive(false) 지양(포커스 꼬임 방지)
        }
        else
        {
            // CanvasGroup이 없다면 임시로 SetActive 사용
            optionsPanel.SetActive(false);
        }

        if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
    }

    // === 해상도/모드 적용 코루틴(프레임 분리 + 보정) ===
    System.Collections.IEnumerator ApplyResolutionCo(bool fsOn)
    {
        if (resList == null || resList.Count == 0) yield break;
        currentIdx = Mathf.Clamp(currentIdx, 0, resList.Count - 1);
        var r = resList[currentIdx];

        // 1) 모드 먼저
        var targetMode = fsOn
            ? (useExclusiveFullScreenForTesting ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.FullScreenWindow)
            : FullScreenMode.Windowed;
        Screen.fullScreenMode = targetMode;

        // 2) 한 프레임 대기
        yield return new WaitForEndOfFrame();

        // 3) 해상도 적용(새 API)
#if UNITY_6000_0_OR_NEWER || UNITY_2022_2_OR_NEWER
        var rr = r.refreshRateRatio; // RefreshRate(분수)
        Screen.SetResolution(r.width, r.height, targetMode, rr);
#else
        Screen.SetResolution(r.width, r.height, targetMode, r.refreshRate);
#endif

        // 4) 또 한 프레임 대기 후 보정
        yield return new WaitForEndOfFrame();
        Screen.fullScreen = (targetMode != FullScreenMode.Windowed);

        // 5) 드물게 씹히는 케이스 대비 0.1초 뒤 한 번 더
        yield return new WaitForSecondsRealtime(0.1f);
#if UNITY_6000_0_OR_NEWER || UNITY_2022_2_OR_NEWER
        Screen.SetResolution(r.width, r.height, targetMode, rr);
#else
        Screen.SetResolution(r.width, r.height, targetMode, r.refreshRate);
#endif

        Debug.Log($"[Options] RES={Screen.width}x{Screen.height} MODE={Screen.fullScreenMode} FS={Screen.fullScreen}");
        if (volumeText) volumeText.text = $"{Screen.width}×{Screen.height}";

        applyRoutine = null;
    }
}
