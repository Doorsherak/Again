using UnityEngine;

/// <summary>
/// 전역 RenderSettings.fog 값을 강제로 설정해 URP/빌트인 모두에서 안개를 켜줍니다.
/// Global Volume 문제가 있을 때 대체용으로 사용하세요.
/// </summary>
[ExecuteAlways]
public class FogEnabler : MonoBehaviour
{
    [ColorUsage(false, true)] public Color fogColor = new Color(0.05f, 0.05f, 0.06f, 1f);
    public FogMode fogMode = FogMode.Exponential;
    [Range(0f, 0.1f)] public float fogDensity = 0.03f;
    public float linearStart = 0f;
    public float linearEnd = 30f;

    void OnEnable() => Apply();
    void OnValidate() => Apply();

    void Apply()
    {
        RenderSettings.fog = true;
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogMode = fogMode;
        RenderSettings.fogDensity = fogDensity;
        RenderSettings.fogStartDistance = linearStart;
        RenderSettings.fogEndDistance = linearEnd;
    }
}
