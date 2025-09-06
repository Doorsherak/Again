using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class HorrorAtmosphere : MonoBehaviour
{
    [Header("References")]
    public Transform player;                    // 플레이어(3D 사운드 배치 기준)
    public Light[] allLights;                   // 비워두면 자동 수집
    public PostProcessVolume postProcessVolume; // PPS v2 사용 시

    [Header("Lighting: Flicker & Brownout")]
    [Range(0f, 1f)] public float horrorLevel = 0.5f; // 0~1
    public float meanBrownoutInterval = 60f;         // 평균 정전 간격(초)
    public float brownoutHold = 3.0f;                // 완전 암흑 유지 시간
    public AnimationCurve brownoutCurve = AnimationCurve.EaseInOut(0, 1, 1, 1);
    public Vector2 perlinSpeedRange = new Vector2(0.5f, 2.5f);
    public Vector2 perlinAmpRange = new Vector2(0.2f, 0.6f); // 강도 흔들림 폭

    [Header("Audio: Ambient & One-shots")]
    public AudioSource ambientLoop; // 루프용
    public AudioClip[] creepyOneShots;
    public float meanCreepyInterval = 35f;  // 평균 간격
    public float minCreepyDistance = 6f;    // 플레이어로부터 최소
    public float maxCreepyDistance = 15f;   // 최대 배치 반경
    public LayerMask occlusionMask;         // 차단 감지용 레이어

    [Header("Fog & Particles")]
    public ParticleSystem dustParticles;
    public float baseFogDensity = 0.01f;
    public float fogJitterAmp = 0.006f;
    public float fogJitterSpeed = 0.2f;

    [Header("Stress Model")]
    [Tooltip("이 값은 외부(적 조우·체력·스토리 이벤트)에서 가감 가능")]
    [Range(0f, 1f)] public float stress = 0f;
    public float stressDecayPerSec = 0.05f;

    // 내부 상태
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
    float mains = 1f; // 전력 계수(0=정전, 1=정상)

    void Awake()
    {
        // 조명 수집
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

        // 파티클 기본값
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

        // 안개 기본값
        RenderSettings.fog = true;
        RenderSettings.fogDensity = baseFogDensity;
    }

    void Update()
    {
        // 스트레스 자연 감쇠
        stress = Mathf.MoveTowards(stress, 0f, stressDecayPerSec * Time.deltaTime);

        // 브라운아웃 스케줄
        if (!isBrownout && Time.time >= nextBrownoutTime)
        {
            StartCoroutine(CoBrownout());
            ScheduleNext(ref nextBrownoutTime, Adj(meanBrownoutInterval));
        }

        // 공포 원샷 스케줄
        if (Time.time >= nextCreepyTime)
        {
            SpawnCreepyOneShot3D();
            ScheduleNext(ref nextCreepyTime, Adj(meanCreepyInterval));
        }

        // 퍼린 노이즈 기반 조명 흔들림(정전 계수와 결합)
        float t = Time.time;
        for (int i = 0; i < lights.Count; i++)
        {
            var st = lights[i];
            if (st.light == null) continue;

            float n = Mathf.PerlinNoise(st.perlinSeed, t * st.perlinSpeed); // 0~1
            float flicker = 1f - st.perlinAmp * (1f - n);                   // 1에서 아래로 흔들림
            st.light.intensity = st.baseIntensity * flicker * mains;

            // 전력 불안정 시 색온도 약간 이동(불안한 노랗게→파랗게)
            float hueShift = (1f - mains) * 0.05f * Mathf.Sin(t * 6f + st.perlinSeed);
            Color.RGBToHSV(st.baseColor, out float h, out float s, out float v);
            h = Mathf.Repeat(h + hueShift, 1f);
            st.light.color = Color.HSVToRGB(h, s, v);

            lights[i] = st;
        }

        // 안개 미세 요동(숨쉬는 환경 느낌)
        float fogN = Mathf.PerlinNoise(11.1f, t * fogJitterSpeed);
        RenderSettings.fogDensity = baseFogDensity + (fogN - 0.5f) * fogJitterAmp * (0.5f + stress);

        // 후처리(있으면) 공포/스트레스 연동
        ApplyPostProcessing();
    }

    void ScheduleNext(ref float nextTime, float meanInterval)
    {
        // 지수분포: -ln(1-u) * mean
        float u = Mathf.Max(1e-6f, 1f - Random.value);
        float delta = -Mathf.Log(u) * Mathf.Max(0.1f, meanInterval);
        nextTime = Time.time + delta;
    }

    float Adj(float x)
    {
        // 공포 레벨·스트레스로 평균 간격 단축(최대 1/3배)
        float k = 1f - 0.66f * Mathf.Clamp01(0.6f * horrorLevel + 0.4f * stress);
        return Mathf.Max(0.1f, x * k);
    }

    IEnumerator CoBrownout()
    {
        isBrownout = true;

        // 내려가기(0.08까지), 약간의 링잉 곡선
        yield return LerpMains(1f, 0.08f, 0.6f, EasingOut);
        yield return new WaitForSeconds(brownoutHold * (0.6f + 0.8f * horrorLevel));

        // 복구(오버슛 1.15 → 안정 1.0)
        yield return LerpMains(0.08f, 1.15f, 0.7f, EasingInOut);
        yield return LerpMains(1.15f, 1f, 0.25f, EasingInOut);

        // 먼지 버스트
        if (dustParticles != null)
            {
                // EmissionModule 가져오기
                var emission = dustParticles.emission;

                // Burst 타입은 'ParticleSystem.Burst'로 명시
                ParticleSystem.Burst burst = new ParticleSystem.Burst(
                    0f,                                     // 발생 시점
                    (short)Random.Range(15, 30)             // 파티클 개수
                );

                // 기존 버스트를 덮어쓰기
                emission.SetBursts(new ParticleSystem.Burst[] { burst });

                dustParticles.Play();
            }


        // 스트레스 상승
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

        // 차단 감지(벽/문 사이)
        var lp = go.AddComponent<AudioLowPassFilter>();
        lp.cutoffFrequency = 22000f;

        // 플레이어와 사이에 장애물이 있으면 컷오프 낮춤
        Vector3 head = player.position + Vector3.up * 1.6f;
        if (Physics.Linecast(go.transform.position, head, out RaycastHit hit, occlusionMask))
            lp.cutoffFrequency = Random.Range(500f, 1200f);

        src.pitch = Random.Range(0.94f, 1.05f);
        src.volume = Random.Range(0.5f, 0.9f) * (0.5f + 0.5f * (0.3f + 0.7f * horrorLevel));
        src.Play();
        Destroy(go, src.clip.length + 0.2f);

        // 들으면 살짝 스트레스
        AddStress(0.08f);
    }

    void ApplyPostProcessing()
    {
        if (postProcessVolume == null || postProcessVolume.profile == null) return;

        // PPS v2 예시: 비네트/필름그레인/크로마틱
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

    // 외부 API 호환
    public void TriggerPowerOutage()
    {
        if (!isBrownout) StartCoroutine(CoBrownout());
    }

    public void SetHorrorLevel(float level)
    {
        horrorLevel = Mathf.Clamp01(level);
        // 난이도에 따라 평균 간격 자동 보정(짧아짐)
        // 별도 파라미터 건드리지 않아도 체감 난이도 상승
    }

    public void AddStress(float delta)
    {
        stress = Mathf.Clamp01(stress + delta);
    }

    // (선택) 모든 조명 원복
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
