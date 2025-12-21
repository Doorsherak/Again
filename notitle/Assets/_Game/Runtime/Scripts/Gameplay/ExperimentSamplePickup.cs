using UnityEngine;

[DisallowMultipleComponent]
public class ExperimentSamplePickup : MonoBehaviour
{
    [HideInInspector] public ExperimentBootstrap bootstrap;

    [Header("Visuals")]
    [SerializeField] bool autoDecorate = true;
    [SerializeField] bool spin = true;
    [SerializeField] float spinSpeed = 120f;
    [SerializeField] bool bob = true;
    [SerializeField] float bobAmplitude = 0.08f;
    [SerializeField] float bobSpeed = 2.2f;
    [SerializeField] bool pulse = true;
    [SerializeField] float pulseAmplitude = 0.06f;
    [SerializeField] float pulseSpeed = 2.8f;

    [Header("Glow Light")]
    [SerializeField] bool addGlowLight = true;
    [SerializeField] Color glowColor = new Color(0.1f, 0.8f, 0.9f, 1f);
    [SerializeField] float glowIntensity = 1.6f;
    [SerializeField] float glowRange = 3.2f;

    Vector3 _basePos;
    Vector3 _baseScale;
    float _seed;
    Light _glow;

    void Awake()
    {
        _basePos = transform.position;
        _baseScale = transform.localScale;
        _seed = Random.Range(0f, 1000f);

        if (!autoDecorate) return;

        if (addGlowLight)
        {
            _glow = GetComponentInChildren<Light>(true);
            if (_glow == null)
            {
                var go = new GameObject("GlowLight");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = Vector3.up * 0.3f;
                _glow = go.AddComponent<Light>();
                _glow.type = LightType.Point;
                _glow.shadows = LightShadows.None;
            }

            _glow.color = glowColor;
            _glow.intensity = Mathf.Max(0f, glowIntensity);
            _glow.range = Mathf.Max(0.1f, glowRange);
        }
    }

    void Update()
    {
        if (!Application.isPlaying) return;

        if (spin)
        {
            float s = spinSpeed * Time.deltaTime;
            transform.Rotate(0f, s, 0f, Space.World);
        }

        if (bob)
        {
            float y = Mathf.Sin((Time.time + _seed) * bobSpeed) * bobAmplitude;
            transform.position = _basePos + Vector3.up * y;
        }

        if (pulse)
        {
            float p = 1f + Mathf.Sin((Time.time + _seed) * pulseSpeed) * pulseAmplitude;
            transform.localScale = _baseScale * p;
            if (_glow != null)
            {
                float k = pulseAmplitude <= 0f
                    ? 1f
                    : Mathf.InverseLerp(1f - pulseAmplitude, 1f + pulseAmplitude, p);
                _glow.intensity = glowIntensity * Mathf.Lerp(0.75f, 1.25f, k);
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!bootstrap) return;
        if (!other.CompareTag("Player")) return;
        bootstrap.OnCollectedSample(this);
        Destroy(gameObject);
    }
}
