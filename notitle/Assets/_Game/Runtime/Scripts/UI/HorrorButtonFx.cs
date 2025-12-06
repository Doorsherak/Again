using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class HorrorButtonFx : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, ISelectHandler, IDeselectHandler, ISubmitHandler
{
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip hoverClip;
    public AudioClip clickClip;
    [Range(0.8f, 1.2f)] public float clickPitchJitter = 0.04f;

    [Header("Hover/Pulse")]
    public float hoverScale = 1.05f;
    public float hoverDuration = 0.08f;
    public float idlePulseAmplitude = 0.01f;
    public float idlePulseSpeed = 2.4f;

    Vector3 _baseScale;
    bool _hovered;
    Coroutine _scaleRoutine;

    void Awake()
    {
        _baseScale = transform.localScale;
    }

    void OnEnable()
    {
        _hovered = false;
        transform.localScale = _baseScale;
    }

    void OnDisable()
    {
        if (_scaleRoutine != null) StopCoroutine(_scaleRoutine);
        transform.localScale = _baseScale;
    }

    void Update()
    {
        // subtle breathing when idle
        if (_hovered || idlePulseAmplitude <= 0f) return;
        float offset = Mathf.Sin(Time.unscaledTime * idlePulseSpeed) * idlePulseAmplitude;
        float target = 1f + offset;
        transform.localScale = Vector3.Lerp(transform.localScale, _baseScale * target, Time.unscaledDeltaTime * 6f);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SetHover(true);
        Play(hoverClip, 1f);
    }

    public void OnSelect(BaseEventData eventData)
    {
        SetHover(true);
    }

    public void OnPointerExit(PointerEventData eventData) => SetHover(false);
    public void OnDeselect(BaseEventData eventData) => SetHover(false);

    public void OnPointerClick(PointerEventData eventData) => PlayClick();
    public void OnSubmit(BaseEventData eventData) => PlayClick();

    void SetHover(bool on)
    {
        if (_hovered == on) return;
        _hovered = on;
        float target = on ? hoverScale : 1f;
        if (_scaleRoutine != null) StopCoroutine(_scaleRoutine);
        _scaleRoutine = StartCoroutine(ScaleTo(target));
    }

    System.Collections.IEnumerator ScaleTo(float targetScale)
    {
        Vector3 start = transform.localScale;
        Vector3 dest = _baseScale * targetScale;
        float t = 0f;
        while (t < hoverDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / hoverDuration);
            transform.localScale = Vector3.Lerp(start, dest, Mathf.SmoothStep(0f, 1f, k));
            yield return null;
        }
        transform.localScale = dest;
        _scaleRoutine = null;
    }

    void PlayClick() => Play(clickClip, 1f + Random.Range(-clickPitchJitter, clickPitchJitter));

    void Play(AudioClip clip, float pitch)
    {
        if (!clip || !audioSource) return;
        audioSource.pitch = pitch;
        audioSource.PlayOneShot(clip);
    }
}
