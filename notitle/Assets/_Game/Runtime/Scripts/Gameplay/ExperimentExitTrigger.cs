using UnityEngine;

[DisallowMultipleComponent]
public class ExperimentExitTrigger : MonoBehaviour
{
    [HideInInspector] public ExperimentBootstrap bootstrap;

    [Header("Visuals")]
    [SerializeField] bool autoDecorate = true;
    [SerializeField] Color lockedColor = new Color(0.9f, 0.15f, 0.15f, 1f);
    [SerializeField] Color unlockedColor = new Color(0.18f, 0.95f, 0.45f, 1f);
    [SerializeField] float beaconHeight = 3.2f;
    [SerializeField] float beaconRadius = 0.22f;
    [SerializeField] float spinSpeed = 60f;
    [SerializeField] float pulseSpeed = 2.2f;
    [SerializeField] float pulseAmplitude = 0.18f;

    [Header("Glow Light")]
    [SerializeField] bool addBeaconLight = true;
    [SerializeField] float lockedLightIntensity = 1.2f;
    [SerializeField] float unlockedLightIntensity = 4.0f;
    [SerializeField] float lightRange = 9f;

    Transform _beacon;
    Material _beaconMaterial;
    Light _light;
    float _seed;
    bool _lastUnlocked;

    void Awake()
    {
        _seed = Random.Range(0f, 1000f);
        if (!autoDecorate) return;

        var beaconGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        beaconGo.name = "ExitBeacon";
        beaconGo.transform.SetParent(transform, false);
        beaconGo.transform.localPosition = Vector3.zero;
        beaconGo.transform.localRotation = Quaternion.identity;
        beaconGo.transform.localScale = new Vector3(beaconRadius * 2f, beaconHeight * 0.5f, beaconRadius * 2f);
        _beacon = beaconGo.transform;

        var col = beaconGo.GetComponent<Collider>();
        if (col) Destroy(col);

        var renderer = beaconGo.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            _beaconMaterial = renderer.material;
            _beaconMaterial.color = lockedColor;
        }

        if (addBeaconLight)
        {
            var lightGo = new GameObject("ExitLight");
            lightGo.transform.SetParent(transform, false);
            lightGo.transform.localPosition = Vector3.up * Mathf.Max(1.0f, beaconHeight);
            _light = lightGo.AddComponent<Light>();
            _light.type = LightType.Point;
            _light.shadows = LightShadows.None;
            _light.range = Mathf.Max(0.1f, lightRange);
            _light.intensity = Mathf.Max(0f, lockedLightIntensity);
            _light.color = lockedColor;
        }
    }

    void Update()
    {
        if (!Application.isPlaying) return;

        bool unlocked = bootstrap != null && bootstrap.CanExit();
        if (unlocked != _lastUnlocked)
        {
            _lastUnlocked = unlocked;
            ApplyVisualState(unlocked);
        }

        if (_beacon == null) return;

        float spin = spinSpeed * Time.unscaledDeltaTime;
        _beacon.Rotate(0f, spin, 0f, Space.World);

        float p = 1f + Mathf.Sin((Time.unscaledTime + _seed) * pulseSpeed) * pulseAmplitude;
        _beacon.localScale = new Vector3(beaconRadius * 2f * p, beaconHeight * 0.5f, beaconRadius * 2f * p);

        if (_light != null)
        {
            float baseIntensity = unlocked ? unlockedLightIntensity : lockedLightIntensity;
            float k = pulseAmplitude <= 0f ? 0.5f : Mathf.InverseLerp(1f - pulseAmplitude, 1f + pulseAmplitude, p);
            _light.intensity = Mathf.Max(0f, baseIntensity) * Mathf.Lerp(0.75f, 1.25f, k);
        }
    }

    void ApplyVisualState(bool unlocked)
    {
        var c = unlocked ? unlockedColor : lockedColor;
        if (_beaconMaterial != null) _beaconMaterial.color = c;
        if (_light != null) _light.color = c;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!bootstrap) return;
        if (!other.CompareTag("Player")) return;
        bootstrap.OnReachedExit();
    }
}
