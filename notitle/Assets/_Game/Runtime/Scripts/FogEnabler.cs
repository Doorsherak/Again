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

    [Header("Subtle Drift")]
    public bool enableDrift = false;
    [Range(0f, 0.02f)] public float densityDrift = 0.004f;
    [Range(0f, 5f)] public float linearDistanceDrift = 1.5f;
    [Range(0.05f, 2f)] public float driftSpeed = 0.35f;
    [Range(0.05f, 0.5f)] public float driftInterval = 0.2f;

    float _driftTimer;
    float _driftSeed;
    bool _wasDrifting;

    void OnEnable()
    {
        _driftSeed = Random.Range(0f, 1000f);
        _driftTimer = 0f;
        _wasDrifting = enableDrift;
        Apply();
    }
    void OnValidate() => Apply();
    void Update()
    {
        if (!Application.isPlaying)
            return;

        if (!enableDrift)
        {
            if (_wasDrifting) Apply();
            _wasDrifting = false;
            return;
        }

        _wasDrifting = true;
        _driftTimer -= Time.deltaTime;
        if (_driftTimer > 0f) return;
        _driftTimer = Mathf.Max(0.01f, driftInterval);

        float noise = Mathf.PerlinNoise(_driftSeed, Time.time * driftSpeed);
        float signed = (noise - 0.5f) * 2f;

        if (fogMode == FogMode.Linear)
        {
            float start = Mathf.Max(0f, linearStart + signed * linearDistanceDrift);
            float end = Mathf.Max(start + 0.1f, linearEnd + signed * linearDistanceDrift);
            RenderSettings.fogStartDistance = start;
            RenderSettings.fogEndDistance = end;
        }
        else
        {
            float density = Mathf.Max(0f, fogDensity + signed * densityDrift);
            RenderSettings.fogDensity = density;
        }
    }

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
