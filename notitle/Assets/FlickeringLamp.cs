using UnityEngine;

public class FlickeringLamp : MonoBehaviour
{
    [Header("Light Components")]
    public Light lampLight;
    public AudioSource electricSound;

    [Header("Flickering Settings")]
    public float flickerChance = 0.02f;
    public float minFlickerDuration = 0.1f;
    public float maxFlickerDuration = 0.5f;
    public float intensityVariation = 0.3f;

    [Header("Horror Effects")]
    public bool canTurnOff = true;
    public float offDuration = 2f;
    public AudioClip flickerSound;
    public AudioClip burnoutSound;

    private float originalIntensity;
    private Color originalColor;
    private bool isFlickering = false;
    private bool isOff = false;

    void Start()
    {
        if (lampLight == null)
            lampLight = GetComponent<Light>();

        originalIntensity = lampLight.intensity;
        originalColor = lampLight.color;

        // 전기 윙윙거리는 소리 설정
        if (electricSound != null)
        {
            electricSound.volume = 0.3f;
            electricSound.loop = true;
            electricSound.Play();
        }
    }

    void Update()
    {
        if (isOff) return;

        // 랜덤 깜빡임
        if (Random.value < flickerChance && !isFlickering)
        {
            StartCoroutine(FlickerEffect());
        }

        // 완전히 꺼지는 효과
        if (canTurnOff && Random.value < 0.0001f)
        {
            StartCoroutine(TurnOffTemporarily());
        }
    }

    System.Collections.IEnumerator FlickerEffect()
    {
        isFlickering = true;

        float flickerDuration = Random.Range(minFlickerDuration, maxFlickerDuration);
        float endTime = Time.time + flickerDuration;

        // 깜빡임 사운드
        if (flickerSound != null && electricSound != null)
        {
            electricSound.PlayOneShot(flickerSound, 0.5f);
        }

        while (Time.time < endTime)
        {
            // 강도 변화
            float randomIntensity = originalIntensity +
                Random.Range(-intensityVariation, intensityVariation);
            lampLight.intensity = Mathf.Max(0, randomIntensity);

            // 색상 변화 (약간 붉은빛)
            lampLight.color = Color.Lerp(originalColor,
                new Color(1f, 0.7f, 0.5f), Random.value * 0.3f);

            yield return new WaitForSeconds(0.05f);
        }

        // 원래 상태로 복구
        lampLight.intensity = originalIntensity;
        lampLight.color = originalColor;
        isFlickering = false;
    }

    System.Collections.IEnumerator TurnOffTemporarily()
    {
        isOff = true;

        // 전구 터지는 소리
        if (burnoutSound != null && electricSound != null)
        {
            electricSound.PlayOneShot(burnoutSound, 0.7f);
        }

        lampLight.enabled = false;

        yield return new WaitForSeconds(offDuration);

        lampLight.enabled = true;
        isOff = false;
    }

    // 외부에서 호출 가능한 메서드
    public void TriggerFlicker()
    {
        if (!isFlickering)
            StartCoroutine(FlickerEffect());
    }

    public void TurnOff()
    {
        lampLight.enabled = false;
        isOff = true;
    }

    public void TurnOn()
    {
        lampLight.enabled = true;
        isOff = false;
    }
}