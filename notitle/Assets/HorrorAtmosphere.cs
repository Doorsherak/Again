using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class HorrorAtmosphere : MonoBehaviour
{
    [Header("References")]
    public Transform player;                    // �÷��̾�(3D ���� ��ġ ����)
    public Light[] allLights;                   // ����θ� �ڵ� ����
    public PostProcessVolume postProcessVolume; // PPS v2 ��� ��

    [Header("Lighting: Flicker & Brownout")]
    [Range(0f, 1f)] public float horrorLevel = 0.5f; // 0~1
    public float meanBrownoutInterval = 60f;         // ��� ���� ����(��)
    public float brownoutHold = 3.0f;                // ���� ���� ���� �ð�
    public AnimationCurve brownoutCurve = AnimationCurve.EaseInOut(0, 1, 1, 1);
    public Vector2 perlinSpeedRange = new Vector2(0.5f, 2.5f);
    public Vector2 perlinAmpRange = new Vector2(0.2f, 0.6f); // ���� ��鸲 ��

    [Header("Audio: Ambient & One-shots")]
    public AudioSource ambientLoop; // ������
    public AudioClip[] creepyOneShots;
    public float meanCreepyInterval = 35f;  // ��� ����
    public float minCreepyDistance = 6f;    // �÷��̾�κ��� �ּ�
    public float maxCreepyDistance = 15f;   // �ִ� ��ġ �ݰ�
    public LayerMask occlusionMask;         // ���� ������ ���̾�

    [Header("Fog & Particles")]
    public ParticleSystem dustParticles;
    public float baseFogDensity = 0.01f;
    public float fogJitterAmp = 0.006f;
    public float fogJitterSpeed = 0.2f;

    [Header("Stress Model")]
    [Tooltip("�� ���� �ܺ�(�� ���졤ü�¡����丮 �̺�Ʈ)���� ���� ����")]
    [Range(0f, 1f)] public float stress = 0f;
    public float stressDecayPerSec = 0.05f;

    // ���� ����
    struct LightState
    {
        public Light light;
        public float baseIntensity;
        public Color baseColor;
        public float perlinSeed;
        public float perlinSpeed;
        public float perlinAmp;
    }
    List<LightState> lights = new List<LightState>();
    float nextBrownoutTime;
    float nextCreepyTime;
    bool isBrownout = false;
    float mains = 1f; // ���� ���(0=����, 1=����)

    void Awake()
    {
        // ���� ����
        if (allLights == null || allLights.Length == 0)
            allLights = FindObjectsByType<Light>(FindObjectsSortMode.None);

        lights.Clear();
        foreach (var l in allLights)
        {
            if (l == null) continue;
            var st = new LightState
            {
                light = l,
                baseIntensity = l.intensity,
                baseColor = l.color,
                perlinSeed = Random.value * 1000f,
                perlinSpeed = Random.Range(perlinSpeedRange.x, perlinSpeedRange.y),
                perlinAmp = Random.Range(perlinAmpRange.x, perlinAmpRange.y)
            };
            lights.Add(st);
        }

        // ��ƼŬ �⺻��
        if (dustParticles != null)
        {
            var main = dustParticles.main;
            main.startSize = 0.01f;
            main.startSpeed = 0.05f;
            main.maxParticles = 80;

            var emission = dustParticles.emission;
            emission.rateOverTime = 4f;
        }

        ScheduleNext(ref nextBrownoutTime, Adj(meanBrownoutInterval));
        ScheduleNext(ref nextCreepyTime, Adj(meanCreepyInterval));

        // �Ȱ� �⺻��
        RenderSettings.fog = true;
        RenderSettings.fogDensity = baseFogDensity;
    }

    void Update()
    {
        // ��Ʈ���� �ڿ� ����
        stress = Mathf.MoveTowards(stress, 0f, stressDecayPerSec * Time.deltaTime);

        // ����ƿ� ������
        if (!isBrownout && Time.time >= nextBrownoutTime)
        {
            StartCoroutine(CoBrownout());
            ScheduleNext(ref nextBrownoutTime, Adj(meanBrownoutInterval));
        }

        // ���� ���� ������
        if (Time.time >= nextCreepyTime)
        {
            SpawnCreepyOneShot3D();
            ScheduleNext(ref nextCreepyTime, Adj(meanCreepyInterval));
        }

        // �۸� ������ ��� ���� ��鸲(���� ����� ����)
        float t = Time.time;
        for (int i = 0; i < lights.Count; i++)
        {
            var st = lights[i];
            if (st.light == null) continue;

            float n = Mathf.PerlinNoise(st.perlinSeed, t * st.perlinSpeed); // 0~1
            float flicker = 1f - st.perlinAmp * (1f - n);                   // 1���� �Ʒ��� ��鸲
            st.light.intensity = st.baseIntensity * flicker * mains;

            // ���� �Ҿ��� �� ���µ� �ణ �̵�(�Ҿ��� ����ԡ��Ķ���)
            float hueShift = (1f - mains) * 0.05f * Mathf.Sin(t * 6f + st.perlinSeed);
            Color.RGBToHSV(st.baseColor, out float h, out float s, out float v);
            h = Mathf.Repeat(h + hueShift, 1f);
            st.light.color = Color.HSVToRGB(h, s, v);

            lights[i] = st;
        }

        // �Ȱ� �̼� �䵿(������ ȯ�� ����)
        float fogN = Mathf.PerlinNoise(11.1f, t * fogJitterSpeed);
        RenderSettings.fogDensity = baseFogDensity + (fogN - 0.5f) * fogJitterAmp * (0.5f + stress);

        // ��ó��(������) ����/��Ʈ���� ����
        ApplyPostProcessing();
    }

    void ScheduleNext(ref float nextTime, float meanInterval)
    {
        // ��������: -ln(1-u) * mean
        float u = Mathf.Max(1e-6f, 1f - Random.value);
        float delta = -Mathf.Log(u) * Mathf.Max(0.1f, meanInterval);
        nextTime = Time.time + delta;
    }

    float Adj(float x)
    {
        // ���� ��������Ʈ������ ��� ���� ����(�ִ� 1/3��)
        float k = 1f - 0.66f * Mathf.Clamp01(0.6f * horrorLevel + 0.4f * stress);
        return Mathf.Max(0.1f, x * k);
    }

    IEnumerator CoBrownout()
    {
        isBrownout = true;

        // ��������(0.08����), �ణ�� ���� �
        yield return LerpMains(1f, 0.08f, 0.6f, EasingOut);
        yield return new WaitForSeconds(brownoutHold * (0.6f + 0.8f * horrorLevel));

        // ����(������ 1.15 �� ���� 1.0)
        yield return LerpMains(0.08f, 1.15f, 0.7f, EasingInOut);
        yield return LerpMains(1.15f, 1f, 0.25f, EasingInOut);

        // ���� ����Ʈ
        if (dustParticles != null)
            {
                // EmissionModule ��������
                var emission = dustParticles.emission;

                // Burst Ÿ���� 'ParticleSystem.Burst'�� ���
                ParticleSystem.Burst burst = new ParticleSystem.Burst(
                    0f,                                     // �߻� ����
                    (short)Random.Range(15, 30)             // ��ƼŬ ����
                );

                // ���� ����Ʈ�� �����
                emission.SetBursts(new ParticleSystem.Burst[] { burst });

                dustParticles.Play();
            }


        // ��Ʈ���� ���
        AddStress(0.25f);

        isBrownout = false;
    }

    IEnumerator LerpMains(float a, float b, float dur, System.Func<float, float> ease)
    {
        float t0 = Time.time;
        while (Time.time - t0 < dur)
        {
            float t = (Time.time - t0) / dur;
            mains = Mathf.LerpUnclamped(a, b, ease(t));
            yield return null;
        }
        mains = b;
    }

    float EasingOut(float x) => 1f - Mathf.Pow(1f - x, 2f);
    float EasingInOut(float x) => x < 0.5f ? 2f * x * x : 1f - Mathf.Pow(-2f * x + 2f, 2f) / 2f;

    void SpawnCreepyOneShot3D()
    {
        if (creepyOneShots == null || creepyOneShots.Length == 0 || player == null) return;

        Vector2 ang = Random.insideUnitCircle.normalized;
        float r = Random.Range(minCreepyDistance, maxCreepyDistance);
        Vector3 pos = player.position + new Vector3(ang.x, 0f, ang.y) * r + Vector3.up * Random.Range(-0.2f, 0.6f);

        GameObject go = new GameObject("CreepyOneShot");
        go.transform.position = pos;
        var src = go.AddComponent<AudioSource>();
        src.clip = creepyOneShots[Random.Range(0, creepyOneShots.Length)];
        src.spatialBlend = 1f;
        src.rolloffMode = AudioRolloffMode.Linear;
        src.minDistance = 2f;
        src.maxDistance = maxCreepyDistance + 8f;
        src.playOnAwake = false;
        src.dopplerLevel = 0f;

        // ���� ����(��/�� ����)
        var lp = go.AddComponent<AudioLowPassFilter>();
        lp.cutoffFrequency = 22000f;

        // �÷��̾�� ���̿� ��ֹ��� ������ �ƿ��� ����
        Vector3 head = player.position + Vector3.up * 1.6f;
        if (Physics.Linecast(go.transform.position, head, out RaycastHit hit, occlusionMask))
            lp.cutoffFrequency = Random.Range(500f, 1200f);

        src.pitch = Random.Range(0.94f, 1.05f);
        src.volume = Random.Range(0.5f, 0.9f) * (0.5f + 0.5f * (0.3f + 0.7f * horrorLevel));
        src.Play();
        Destroy(go, src.clip.length + 0.2f);

        // ������ ��¦ ��Ʈ����
        AddStress(0.08f);
    }

    void ApplyPostProcessing()
    {
        if (postProcessVolume == null || postProcessVolume.profile == null) return;

        // PPS v2 ����: ���Ʈ/�ʸ��׷���/ũ�θ�ƽ
        if (postProcessVolume.profile.TryGetSettings(out Vignette vig))
            vig.intensity.value = Mathf.Lerp(0.15f, 0.45f, 0.6f * horrorLevel + 0.4f * stress);

        if (postProcessVolume.profile.TryGetSettings(out Grain gr))
        {
            gr.intensity.value = Mathf.Lerp(0.1f, 0.45f, 0.5f * horrorLevel + 0.5f * stress);
            gr.colored.value = false;
        }

        if (postProcessVolume.profile.TryGetSettings(out ChromaticAberration ca))
            ca.intensity.value = Mathf.Lerp(0.02f, 0.25f, (isBrownout ? 1f : (0.6f * horrorLevel + 0.4f * stress)));
    }

    // �ܺ� API ȣȯ
    public void TriggerPowerOutage()
    {
        if (!isBrownout) StartCoroutine(CoBrownout());
    }

    public void SetHorrorLevel(float level)
    {
        horrorLevel = Mathf.Clamp01(level);
        // ���̵��� ���� ��� ���� �ڵ� ����(ª����)
        // ���� �Ķ���� �ǵ帮�� �ʾƵ� ü�� ���̵� ���
    }

    public void AddStress(float delta)
    {
        stress = Mathf.Clamp01(stress + delta);
    }

    // (����) ��� ���� ����
    [ContextMenu("Restore Lights")]
    void RestoreLights()
    {
        mains = 1f;
        for (int i = 0; i < lights.Count; i++)
        {
            var st = lights[i];
            if (st.light == null) continue;
            st.light.intensity = st.baseIntensity;
            st.light.color = st.baseColor;
            lights[i] = st;
        }
    }
}
