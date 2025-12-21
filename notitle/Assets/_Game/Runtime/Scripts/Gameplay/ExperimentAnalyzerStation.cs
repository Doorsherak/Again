using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class ExperimentAnalyzerStation : MonoBehaviour
{
    [HideInInspector] public ExperimentBootstrap bootstrap;

    [Header("Analysis")]
    [SerializeField, Min(0.05f)] public float secondsPerSample = 1.1f;

    [Header("Visuals")]
    [SerializeField] bool autoDecorate = true;
    [SerializeField] Color idleColor = new Color(0.12f, 0.65f, 0.95f, 1f);
    [SerializeField] Color activeColor = new Color(0.95f, 0.85f, 0.18f, 1f);
    [SerializeField] float lightRange = 6f;
    [SerializeField] float idleLightIntensity = 1.2f;
    [SerializeField] float activeLightIntensity = 2.8f;
    [SerializeField] float pulseSpeed = 2.2f;

    int _inside;
    Coroutine _co;
    Material _mat;
    Light _light;

    void Awake()
    {
        if (!autoDecorate) return;

        var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "AnalyzerBody";
        body.transform.SetParent(transform, false);
        body.transform.localPosition = Vector3.up * 0.55f;
        body.transform.localRotation = Quaternion.identity;
        body.transform.localScale = new Vector3(0.9f, 1.1f, 0.55f);

        var bodyCol = body.GetComponent<Collider>();
        if (bodyCol) Destroy(bodyCol);

        var renderer = body.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            _mat = renderer.material;
            _mat.color = idleColor;
        }

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(transform, false);
        labelGo.transform.localPosition = Vector3.up * 1.35f;
        labelGo.transform.localRotation = Quaternion.identity;

        var tm = labelGo.AddComponent<TextMesh>();
        tm.text = "ANALYZE";
        tm.fontSize = 72;
        tm.characterSize = 0.05f;
        tm.color = idleColor;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;

        var lightGo = new GameObject("AnalyzerLight");
        lightGo.transform.SetParent(transform, false);
        lightGo.transform.localPosition = Vector3.up * 1.6f;
        _light = lightGo.AddComponent<Light>();
        _light.type = LightType.Point;
        _light.shadows = LightShadows.None;
        _light.range = Mathf.Max(0.1f, lightRange);
        _light.intensity = Mathf.Max(0f, idleLightIntensity);
        _light.color = idleColor;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        _inside++;
        if (_co == null) _co = StartCoroutine(CoAnalyzeLoop());
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        _inside = Mathf.Max(0, _inside - 1);
    }

    IEnumerator CoAnalyzeLoop()
    {
        while (_inside > 0)
        {
            if (bootstrap == null || bootstrap.IsEnding)
                break;

            if (!bootstrap.CanExit() && bootstrap.RequireAnalysisStep && bootstrap.RawSamples > 0)
            {
                ApplyVisualActive(true);
                float dur = Mathf.Max(0.05f, secondsPerSample);
                float t = 0f;
                while (t < dur)
                {
                    if (_inside <= 0) break;
                    if (bootstrap == null || bootstrap.IsEnding) { _inside = 0; break; }
                    t += Time.unscaledDeltaTime;
                    UpdatePulse(true);
                    yield return null;
                }

                if (_inside <= 0) break;
                bootstrap.TryProcessOneSample();
                continue;
            }

            ApplyVisualActive(false);
            UpdatePulse(false);
            yield return new WaitForSecondsRealtime(0.2f);
        }

        ApplyVisualActive(false);
        _co = null;
    }

    void ApplyVisualActive(bool active)
    {
        var c = active ? activeColor : idleColor;
        if (_mat != null) _mat.color = c;
        if (_light != null)
        {
            _light.color = c;
            _light.intensity = active ? activeLightIntensity : idleLightIntensity;
        }
    }

    void UpdatePulse(bool active)
    {
        if (_light == null) return;
        float baseIntensity = active ? activeLightIntensity : idleLightIntensity;
        float pulse = (Mathf.Sin(Time.unscaledTime * pulseSpeed) + 1f) * 0.5f;
        _light.intensity = Mathf.Max(0f, baseIntensity) * Mathf.Lerp(0.75f, 1.25f, pulse);
    }
}

