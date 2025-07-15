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

        // ���� �����Ÿ��� �Ҹ� ����
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

        // ���� ������
        if (Random.value < flickerChance && !isFlickering)
        {
            StartCoroutine(FlickerEffect());
        }

        // ������ ������ ȿ��
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

        // ������ ����
        if (flickerSound != null && electricSound != null)
        {
            electricSound.PlayOneShot(flickerSound, 0.5f);
        }

        while (Time.time < endTime)
        {
            // ���� ��ȭ
            float randomIntensity = originalIntensity +
                Random.Range(-intensityVariation, intensityVariation);
            lampLight.intensity = Mathf.Max(0, randomIntensity);

            // ���� ��ȭ (�ణ ������)
            lampLight.color = Color.Lerp(originalColor,
                new Color(1f, 0.7f, 0.5f), Random.value * 0.3f);

            yield return new WaitForSeconds(0.05f);
        }

        // ���� ���·� ����
        lampLight.intensity = originalIntensity;
        lampLight.color = originalColor;
        isFlickering = false;
    }

    System.Collections.IEnumerator TurnOffTemporarily()
    {
        isOff = true;

        // ���� ������ �Ҹ�
        if (burnoutSound != null && electricSound != null)
        {
            electricSound.PlayOneShot(burnoutSound, 0.7f);
        }

        lampLight.enabled = false;

        yield return new WaitForSeconds(offDuration);

        lampLight.enabled = true;
        isOff = false;
    }

    // �ܺο��� ȣ�� ������ �޼���
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