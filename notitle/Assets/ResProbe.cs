// ResProbe.cs (빈 오브젝트에 부착)
using UnityEngine;
using System.Collections; // ← 추가

public class ResProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod]
    static void Kick() { new GameObject("ResProbe").AddComponent<ResProbe>(); }

    IEnumerator Start()
    {
        // 1) 모드 → 1프레임 대기 → 해상도 → 0.1s 후 재호출 (최소재현)
        var exclusive = true; // 테스트는 눈에 보이는 Exclusive 권장
        var mode = exclusive ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.FullScreenWindow;

        Screen.fullScreenMode = mode;
        yield return new WaitForEndOfFrame();

#if UNITY_6000_0_OR_NEWER || UNITY_2022_2_OR_NEWER
        Screen.SetResolution(1280, 720, mode, Screen.currentResolution.refreshRateRatio);
#else
        Screen.SetResolution(1280, 720, mode, Screen.currentResolution.refreshRate);
#endif
        yield return new WaitForSecondsRealtime(0.1f);
#if UNITY_6000_0_OR_NEWER || UNITY_2022_2_OR_NEWER
        Screen.SetResolution(1920, 1080, mode, Screen.currentResolution.refreshRateRatio);
#else
        Screen.SetResolution(1920, 1080, mode, Screen.currentResolution.refreshRate);
#endif
    }

    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 400, 60),
            $"RES: {Screen.width}×{Screen.height}\nMODE: {Screen.fullScreenMode}");
    }
}
