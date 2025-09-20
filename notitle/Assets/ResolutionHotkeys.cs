#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

public class ResolutionHotkeys : MonoBehaviour
{
    [Tooltip("테스트 때는 Exclusive로. 최종 빌드는 보통 FullScreenWindow 권장")]
    public bool useExclusive = true;

    [Header("Keys")]
    public KeyCode key720 = KeyCode.F9;
    public KeyCode key1080 = KeyCode.F10;
    public KeyCode keyToggleFS = KeyCode.F11;
    public KeyCode keyCycle = KeyCode.F8;   // 지원 해상도 순환
    public KeyCode keyToggleHUD = KeyCode.F1;

    bool showHud = true;
    GUIStyle style;
    int idx = -1;
    Resolution[] list;

    void Awake() => DontDestroyOnLoad(gameObject);

    void Update()
    {
        if (Input.GetKeyDown(keyToggleFS))
        {
            bool toWindow = Screen.fullScreenMode != FullScreenMode.Windowed ? true : false;
            Screen.fullScreenMode = toWindow
                ? FullScreenMode.Windowed
                : (useExclusive ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.FullScreenWindow);
            Debug.Log($"[Hotkey] MODE => {Screen.fullScreenMode}");
        }

        if (Input.GetKeyDown(key720)) SetRes(1280, 720, FullScreenMode.Windowed);
        if (Input.GetKeyDown(key1080)) SetRes(1920, 1080, useExclusive ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.FullScreenWindow);

        if (Input.GetKeyDown(keyCycle)) CycleRes();
        if (Input.GetKeyDown(keyToggleHUD)) showHud = !showHud;
    }

    void CycleRes()
    {
        if (list == null || list.Length == 0) list = Screen.resolutions;
        if (list.Length == 0) return;
        idx = (idx + 1) % list.Length;
        var r = list[idx];
#if UNITY_6000_0_OR_NEWER || UNITY_2022_2_OR_NEWER
        Screen.SetResolution(r.width, r.height, Screen.fullScreenMode, r.refreshRateRatio);
#else
        Screen.SetResolution(r.width, r.height, Screen.fullScreenMode, r.refreshRate);
#endif
        Debug.Log($"[Hotkey] CYCLE => {r.width}x{r.height}");
    }

    void SetRes(int w, int h, FullScreenMode mode)
    {
        Screen.fullScreenMode = mode;
#if UNITY_6000_0_OR_NEWER || UNITY_2022_2_OR_NEWER
        Screen.SetResolution(w, h, mode, Screen.currentResolution.refreshRateRatio);
#else
        Screen.SetResolution(w, h, mode, Screen.currentResolution.refreshRate);
#endif
        Debug.Log($"[Hotkey] RES => {w}x{h}  {mode}");
    }

    void OnGUI()
    {
        if (!showHud) return;
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.UpperRight };
        }
#if UNITY_6000_0_OR_NEWER || UNITY_2022_2_OR_NEWER
        var rr = Screen.currentResolution.refreshRateRatio;
        float hz = rr.denominator != 0 ? (float)rr.numerator / rr.denominator : 0f;
        string hzText = hz.ToString("0.#");
#else
        string hzText = Screen.currentResolution.refreshRate.ToString();
#endif
        string text =
            $"RES:  {Screen.width}×{Screen.height}\n" +
            $"MODE: {Screen.fullScreenMode}\n" +
            $"HZ:   {hzText}\n" +
            $"F9 720p Windowed | F10 1080p Fullscreen\n" +
            $"F11 Toggle Fullscreen | F8 Cycle | F1 HUD";
        GUI.Label(new Rect(Screen.width - 380, 10, 370, 100), text, style);
    }
}
#endif
